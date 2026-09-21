using System.Net.WebSockets;

namespace WEBSOCKET.WebSockets.Connections;

public interface IWebSocketConnectionManager
{
    WebSocketConnection Add(WebSocket socket);

    /// <summary>Elimina el registro sin cerrar ni disponer el socket.</summary>
    bool Remove(Guid connectionId);

    /// <summary>
    /// Devuelve una copia de las conexiones abiertas y elimina del registro las
    /// que ya no lo están. Su estado puede cambiar tras la consulta.
    /// </summary>
    IReadOnlyCollection<WebSocketConnection> GetAll();
}
