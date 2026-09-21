namespace Tooba.Inventory.Contracts;

/// <summary>Availability for one location. Not an EF entity.</summary>
public sealed record LocationAvailability(
    Guid StockItemId,
    Guid LocationId,
    string LocationCode,
    decimal OnHand,
    decimal Reserved,
    decimal Available);

/// <summary>Aggregated Offer availability for the current Tenant.</summary>
public sealed record InventoryAvailability(
    Guid OfferId,
    Guid CatalogVariantId,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    IReadOnlyList<LocationAvailability> Locations);

/// <summary>Cross-module availability reads without EF leakage.</summary>
public interface IInventoryAvailabilityGateway
{
    /// <summary>Sums Offer stock in the current Tenant/Marketplace database.</summary>
    Task<InventoryAvailability?> GetAvailabilityAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>Batch availability so cart GET does not N+1.</summary>
    Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);
}
