namespace Tooba.Catalog.Contracts.Reservation;

/// <summary>
/// Catalog-owned store override for Cart persistence hours. Catalog owns the persistence of the
/// store settings row; Cart owns the Cart persistence policy and its platform fallback.
/// </summary>
public interface IStoreCartPersistenceHoursReader
{
    /// <summary>Configured store override in hours, or null when the store inherits platform.</summary>
    Task<int?> GetStoreCartPersistenceHoursAsync(CancellationToken cancellationToken);
}
