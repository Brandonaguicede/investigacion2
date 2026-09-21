using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text.Json;
using Auction.Application.Auctions.GetAuctionState;
using Auction.Application.Bids.PlaceBid;
using WEBSOCKET.WebSockets.Connections;
using WEBSOCKET.WebSockets.Contracts;

namespace WEBSOCKET.WebSockets;

public sealed class AuctionWebSocketHandler(
    IWebSocketConnectionManager connectionManager,
    PlaceBidService placeBidService,
    GetAuctionStateService getAuctionStateService,
    WebSocketMessageSender messageSender,
    ILogger<AuctionWebSocketHandler> logger)
{
    private const int MaxMessageBytes = 64 * 1024;

    // Solo se procesa un mensaje a la vez: aceptar una oferta y difundir su resultado forman una unidad.
    // Así todos los clientes reciben las actualizaciones en el mismo orden en que se aceptaron
    // (sin este orden, una actualización de 130 podría llegar después de una de 140), y el estado
    // inicial de un cliente nuevo nunca llega después de una actualización más reciente.
    private readonly SemaphoreSlim _messageGate = new(1, 1);

    public async Task HandleAsync(HttpContext context)
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connection = connectionManager.Add(socket);

        try
        {
            logger.LogDebug("WebSocket client {ConnectionId} connected. Active connections: {ConnectionCount}.",
                connection.Id, connectionManager.GetAll().Count);

            await SendCurrentStateAsync(connection, context.RequestAborted);

            var buffer = new byte[4096];
            using var message = new MemoryStream();
            var oversized = false;

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), context.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await messageSender.CloseAsync(connection,
                        result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription, context.RequestAborted);
                    break;
                }

                if (message.Length + result.Count > MaxMessageBytes)
                    oversized = true;

                if (!oversized)
                    message.Write(buffer, 0, result.Count);

                if (!result.EndOfMessage)
                    continue;

                await ProcessMessageAsync(connection, message.ToArray(),
                    result.MessageType, oversized, context.RequestAborted);
                message.SetLength(0);
                oversized = false;
            }
        }
        catch (OperationCanceledException)
        {
            socket.Abort();
        }
        catch (WebSocketException exception)
        {
            logger.LogDebug(exception, "WebSocket connection {ConnectionId} was interrupted.", connection.Id);
            socket.Abort();
        }
        finally
        {
            connectionManager.Remove(connection.Id);
            logger.LogDebug("WebSocket client {ConnectionId} disconnected.", connection.Id);
        }
    }

    // Un cliente nuevo recibe el estado actual solo a él, por el mismo WebSocket.
    private async Task SendCurrentStateAsync(WebSocketConnection connection, CancellationToken cancellationToken)
    {
        await _messageGate.WaitAsync(cancellationToken);
        try
        {
            var state = getAuctionStateService.Execute();
            await messageSender.SendAsync(connection,
                new AuctionUpdatedMessage(state.CurrentPrice, state.CurrentWinner), cancellationToken);
        }
        finally
        {
            _messageGate.Release();
        }
    }

    private async Task ProcessMessageAsync(WebSocketConnection connection, byte[] payload,
        WebSocketMessageType messageType, bool oversized, CancellationToken cancellationToken)
    {
        await _messageGate.WaitAsync(cancellationToken);
        try
        {
            if (!TryReadBid(payload, messageType, oversized, out var request, out var error))
            {
                logger.LogDebug("Invalid WebSocket message from {ConnectionId}: {Reason}", connection.Id, error);
                await messageSender.SendAsync(connection, new ErrorMessage(error), cancellationToken);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            logger.LogDebug("Bid message received from {ConnectionId}.", connection.Id);
            var result = placeBidService.Execute(request);
            if (!result.Accepted)
            {
                logger.LogInformation("Bid rejected for {ConnectionId}.", connection.Id);
                await messageSender.SendAsync(connection,
                    new ErrorMessage(result.Error ?? "Oferta rechazada."), cancellationToken);
                return;
            }

            logger.LogInformation("Bid accepted for {ConnectionId}, current price {CurrentPrice}.",
                connection.Id, result.CurrentPrice);
            var update = new AuctionUpdatedMessage(result.CurrentPrice, result.CurrentWinner);
            // La cancelación del oferente no interrumpe la difusión de una oferta ya aceptada.
            await messageSender.BroadcastAsync(update);
        }
        finally
        {
            _messageGate.Release();
        }
    }

    // Valida solo el formato del mensaje (transporte). Las reglas de la subasta las decide el dominio.
    private static bool TryReadBid(byte[] payload, WebSocketMessageType messageType, bool oversized,
        [NotNullWhen(true)] out PlaceBidRequest? request, [NotNullWhen(false)] out string? error)
    {
        request = null;

        if (oversized)
        {
            error = "El mensaje supera el límite de 64 KiB.";
            return false;
        }

        if (messageType != WebSocketMessageType.Text)
        {
            error = "Solo se aceptan mensajes JSON de texto.";
            return false;
        }

        PlaceBidMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<PlaceBidMessage>(payload);
        }
        catch (JsonException)
        {
            error = "El mensaje JSON no es válido.";
            return false;
        }

        if (message?.Type != PlaceBidMessage.MessageType)
        {
            error = "Tipo de mensaje no soportado.";
            return false;
        }

        if (message.Bidder is null || message.Amount is null)
        {
            error = "El mensaje requiere bidder (texto) y amount (número).";
            return false;
        }

        request = new PlaceBidRequest(message.Bidder, message.Amount.Value);
        error = null;
        return true;
    }
}
