namespace Auction.Application.Bids.PlaceBid;

public sealed record PlaceBidRequest(string Bidder, decimal Amount);
