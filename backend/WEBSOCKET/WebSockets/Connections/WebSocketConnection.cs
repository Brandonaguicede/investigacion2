using System.Net.WebSockets;

namespace WEBSOCKET.WebSockets.Connections;

/// <summary>
/// Un cliente conectado: su socket, su identificador y un candado de envío.
/// Un WebSocket no admite dos envíos simultáneos, así que cada conexión tiene su propio candado.
/// </summary>
public sealed class WebSocketConnection(WebSocket socket)
{
    public Guid Id { get; } = Guid.NewGuid();

    public WebSocket Socket { get; } = socket;

    public SemaphoreSlim SendLock { get; } = new(1, 1);
}
