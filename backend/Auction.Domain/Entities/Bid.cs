namespace Auction.Domain.Entities;

public sealed class Bid
{
    public Bid(string bidder, decimal amount)
    {
        Bidder = bidder;
        Amount = amount;
    }

    public string Bidder { get; }

    public decimal Amount { get; }
}
