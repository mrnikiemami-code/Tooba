namespace Tooba.Inventory.Contracts.Availability;

/// <summary>Stock position row for Host reads. Not an EF entity.</summary>
public sealed record StockPositionSnapshot(
    Guid StockItemId,
    Guid OfferId,
    Guid LocationId,
    decimal OnHand,
    decimal Reserved,
    decimal Available);

/// <summary>Location row for Host labels and seeds.</summary>
public sealed record InventoryLocationSnapshot(
    Guid LocationId,
    string Code,
    string Name,
    string Status);

/// <summary>Inventory-owned read port so Host never opens InventoryDbContext.</summary>
public interface IInventoryQueryGateway
{
    /// <summary>Positions for the given offers.</summary>
    Task<IReadOnlyList<StockPositionSnapshot>> ListPositionsByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);

    /// <summary>All positions (admin grid metrics).</summary>
    Task<IReadOnlyList<StockPositionSnapshot>> ListAllPositionsAsync(CancellationToken cancellationToken);

    /// <summary>Locations for the given ids.</summary>
    Task<IReadOnlyList<InventoryLocationSnapshot>> ListLocationsByIdsAsync(
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken);

    /// <summary>First location matching the code, or null.</summary>
    Task<InventoryLocationSnapshot?> FindLocationByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>First active location id ordered by code, or null.</summary>
    Task<Guid?> FindFirstActiveLocationIdAsync(CancellationToken cancellationToken);

    /// <summary>Position for offer+location, or null.</summary>
    Task<StockPositionSnapshot?> FindPositionAsync(
        Guid offerId,
        Guid locationId,
        CancellationToken cancellationToken);

    /// <summary>Position by stock item id, or null.</summary>
    Task<StockPositionSnapshot?> FindPositionByStockItemIdAsync(
        Guid stockItemId,
        CancellationToken cancellationToken);
}
