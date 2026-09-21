namespace Auction.Application.Bids.PlaceBid;

public sealed record PlaceBidResult(
    bool Accepted,
    decimal CurrentPrice,
    string? CurrentWinner,
    string? Error);
