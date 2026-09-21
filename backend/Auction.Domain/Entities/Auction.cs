using Auction.Domain.Enums;

namespace Auction.Domain.Entities;

public sealed class Auction
{
    public const int MaxBidderLength = 100;

    public Auction(Guid id, decimal initialPrice)
    {
        Id = id;
        CurrentPrice = initialPrice;
    }

    public Guid Id { get; }

    public decimal CurrentPrice { get; private set; }

    public string? CurrentWinner => CurrentBid?.Bidder;

    public AuctionStatus Status { get; private set; } = AuctionStatus.Waiting;

    public Bid? CurrentBid { get; private set; }

    public void Start()
    {
        if (Status == AuctionStatus.Waiting)
        {
            Status = AuctionStatus.Active;
        }
    }

    public bool TryPlaceBid(Bid bid, out string? error)
    {
        ArgumentNullException.ThrowIfNull(bid);

        error = null;

        if (Status != AuctionStatus.Active)
            error = "La subasta no está activa.";
        else if (string.IsNullOrWhiteSpace(bid.Bidder))
            error = "El nombre del participante es obligatorio.";
        else if (bid.Bidder.Length > MaxBidderLength)
            error = $"El nombre del participante no puede superar {MaxBidderLength} caracteres.";
        else if (bid.Amount <= 0)
            error = "El monto debe ser mayor que cero.";
        else if (!IsHigherThanCurrentPrice(bid.Amount))
            error = "El monto debe ser estrictamente mayor al precio actual.";

        if (error is not null)
        {
            return false;
        }

        CurrentPrice = bid.Amount;
        CurrentBid = bid;
        return true;
    }

    private bool IsHigherThanCurrentPrice(decimal amount) => amount > CurrentPrice;
}
