using System.Text.Json.Serialization;

namespace WEBSOCKET.WebSockets.Contracts;

/// <summary>
/// Mensaje que envía el cliente. Los campos son opcionales para poder detectar,
/// tras una única deserialización, si el cliente omitió alguno.
/// </summary>
public sealed record PlaceBidMessage(
    [property: JsonPropertyName("bidder")] string? Bidder,
    [property: JsonPropertyName("amount")] decimal? Amount)
{
    public const string MessageType = "placeBid";

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
