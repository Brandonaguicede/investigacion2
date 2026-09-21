namespace Auction.Application.Auctions.GetAuctionState;

public sealed record AuctionStateResult(decimal CurrentPrice, string? CurrentWinner);
