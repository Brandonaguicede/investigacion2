using Auction.Application.Abstractions.Persistence;

namespace Auction.Application.Auctions.GetAuctionState;

/// <summary>
/// Consulta el precio y el ganador actuales, por ejemplo para un cliente que acaba de conectarse.
/// </summary>
public sealed class GetAuctionStateService
{
    private readonly IAuctionStateStore _stateStore;

    public GetAuctionStateService(IAuctionStateStore stateStore)
    {
        ArgumentNullException.ThrowIfNull(stateStore);
        _stateStore = stateStore;
    }

    public AuctionStateResult Execute()
        => _stateStore.Execute(auction =>
            new AuctionStateResult(auction.CurrentPrice, auction.CurrentWinner));
}
