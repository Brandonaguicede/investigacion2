namespace Auction.Domain.Enums;

public enum AuctionStatus
{
    Waiting,
    Active,
    // Estado final del ciclo de vida. Aún no hay un caso de uso que cierre la subasta;
    // mientras tanto, TryPlaceBid ya rechaza ofertas en cualquier estado distinto de Active.
    Closed
}
