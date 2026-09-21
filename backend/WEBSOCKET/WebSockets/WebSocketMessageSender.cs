using System.Net.WebSockets;
using System.Text.Json;
using WEBSOCKET.WebSockets.Connections;

namespace WEBSOCKET.WebSockets;

public sealed class WebSocketMessageSender(
    IWebSocketConnectionManager connectionManager,
    ILogger<WebSocketMessageSender> logger)
{
    public Task SendAsync<T>(WebSocketConnection connection, T message,
        CancellationToken cancellationToken = default)
        => SendPayloadAsync(connection, JsonSerializer.SerializeToUtf8Bytes(message), cancellationToken);

    public async Task BroadcastAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        var connections = connectionManager.GetAll();
        logger.LogInformation("Broadcasting WebSocket update to {ClientCount} clients.", connections.Count);
        await Task.WhenAll(connections.Select(connection =>
            SendPayloadAsync(connection, payload, cancellationToken)));
    }

    private Task SendPayloadAsync(WebSocketConnection connection, byte[] payload,
        CancellationToken cancellationToken)
        => WithSendLockAsync(connection, async token =>
        {
            if (connection.Socket.State != WebSocketState.Open)
            {
                connectionManager.Remove(connection.Id);
                return;
            }

            await connection.Socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text,
                endOfMessage: true, token);
        }, cancellationToken);

    public Task CloseAsync(WebSocketConnection connection, WebSocketCloseStatus status,
        string? description, CancellationToken cancellationToken = default)
        => WithSendLockAsync(connection, async token =>
        {
            if (connection.Socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await connection.Socket.CloseOutputAsync(status, description, token);
        }, cancellationToken);

    // Toda operación de salida sobre un socket (enviar o cerrar) pasa por el candado de su conexión.
    private async Task WithSendLockAsync(WebSocketConnection connection,
        Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var acquired = false;
        try
        {
            await connection.SendLock.WaitAsync(timeout.Token);
            acquired = true;
            await operation(timeout.Token);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            connectionManager.Remove(connection.Id);
            connection.Socket.Abort();
            logger.LogDebug(exception, "Client {ConnectionId} disconnected or timed out during WebSocket send/close.", connection.Id);
        }
        finally
        {
            if (acquired)
                connection.SendLock.Release();
        }
    }
}
