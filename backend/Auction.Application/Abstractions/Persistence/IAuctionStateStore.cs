using AuctionEntity = Auction.Domain.Entities.Auction;

namespace Auction.Application.Abstractions.Persistence;

/// <summary>
/// Proporciona acceso controlado al estado de la unica subasta.
/// </summary>
public interface IAuctionStateStore
{
    /// <summary>
    /// Consulta o modifica la subasta y devuelve el resultado de la operacion.
    /// </summary>
    /// <remarks>
    /// La implementacion debe ejecutar el callback una sola vez, con acceso
    /// exclusivo al estado durante toda la operacion, incluidas las lecturas.
    /// Las modificaciones deben realizarse mediante los metodos del dominio.
    /// El callback debe ser sincrono, no debe volver a entrar al almacen ni
    /// conservar, capturar para uso posterior o devolver la entidad Auction.
    /// El resultado debe ser un valor o datos inmutables independientes del
    /// estado mutable. No se garantiza revertir cambios si el callback falla.
    /// </remarks>
    TResult Execute<TResult>(Func<AuctionEntity, TResult> operation);
}
