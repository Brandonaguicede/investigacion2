using Auction.Application.Abstractions.Persistence;
using AuctionEntity = Auction.Domain.Entities.Auction;

namespace Auction.Infrastructure.Persistence;

/// <summary>
/// Mantiene una subasta en memoria. Los consumidores deben compartir esta instancia.
/// </summary>
public sealed class InMemoryAuctionStateStore : IAuctionStateStore
{
    private readonly object _syncRoot = new();
    private readonly AuctionEntity _auction;

    public InMemoryAuctionStateStore()
    {
        _auction = new AuctionEntity(Guid.NewGuid(), 100m);
        _auction.Start();
    }

    /// <inheritdoc />
    
    public TResult Execute<TResult>(Func<AuctionEntity, TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_syncRoot)
        {
            return operation(_auction);
        }
    }
}
