using System.Text.Json.Serialization;

namespace WEBSOCKET.WebSockets.Contracts;

public sealed record AuctionUpdatedMessage(
    [property: JsonPropertyName("currentPrice")] decimal CurrentPrice,
    [property: JsonPropertyName("currentWinner")] string? CurrentWinner)
{
    public const string MessageType = "auctionUpdated";

    [JsonPropertyName("type")]
    public string Type => MessageType;
}
