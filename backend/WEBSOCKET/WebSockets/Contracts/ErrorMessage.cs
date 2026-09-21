using System.Text.Json.Serialization;

namespace WEBSOCKET.WebSockets.Contracts;

public sealed record ErrorMessage(
    [property: JsonPropertyName("message")] string Message)
{
    public const string MessageType = "error";

    [JsonPropertyName("type")]
    public string Type => MessageType;
}
