using Auction.Application.Abstractions.Persistence;
using Auction.Domain.Entities;

namespace Auction.Application.Bids.PlaceBid;

public sealed class PlaceBidService
{
    private readonly IAuctionStateStore _stateStore;

    public PlaceBidService(IAuctionStateStore stateStore)
    {
        ArgumentNullException.ThrowIfNull(stateStore);
        _stateStore = stateStore;
    }

    public PlaceBidResult Execute(PlaceBidRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _stateStore.Execute(auction =>
        {
            var bid = new Bid(request.Bidder, request.Amount);
            var accepted = auction.TryPlaceBid(bid, out var error);

            return new PlaceBidResult(
                accepted, auction.CurrentPrice, auction.CurrentWinner, error);
        });
    }
}
