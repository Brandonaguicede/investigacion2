using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WEBSOCKET.WebSockets.Connections;

public sealed class WebSocketConnectionManager : IWebSocketConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocketConnection> _connections = new();

    public WebSocketConnection Add(WebSocket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        if (socket.State != WebSocketState.Open)
        {
            throw new ArgumentException("La conexión debe estar abierta.", nameof(socket));
        }

        var connection = new WebSocketConnection(socket);

        // Un Guid nuevo no se repite en la práctica; si ocurriera, se falla de forma explícita.
        if (!_connections.TryAdd(connection.Id, connection))
        {
            throw new InvalidOperationException("No se pudo registrar la conexión.");
        }

        return connection;
    }

    public bool Remove(Guid connectionId) => _connections.TryRemove(connectionId, out _);

    public IReadOnlyCollection<WebSocketConnection> GetAll()
    {
        var openConnections = new List<WebSocketConnection>();

        foreach (var connection in _connections.Values)
        {
            if (connection.Socket.State == WebSocketState.Open)
            {
                openConnections.Add(connection);
            }
            else
            {
                _connections.TryRemove(connection.Id, out _);
            }
        }

        return openConnections;
    }
}
