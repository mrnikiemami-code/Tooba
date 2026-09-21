using Tooba.Inventory.Contracts.Availability;
using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Events;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Infrastructure.Adapters;

/// <summary>Host-facing inventory reads. Does not mutate stock.</summary>
public sealed class InventoryQueryGateway(InventoryDbContext db) : IInventoryQueryGateway
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<StockPositionSnapshot>> ListPositionsByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offerIds);
        if (offerIds.Count == 0)
        {
            return [];
        }

        var ids = offerIds.Distinct().ToArray();
        var rows = await db.Positions.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        return rows.Select(ToSnapshot).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockPositionSnapshot>> ListAllPositionsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await db.Positions.AsNoTracking().ToListAsync(cancellationToken);
        return rows.Select(ToSnapshot).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryLocationSnapshot>> ListLocationsByIdsAsync(
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(locationIds);
        if (locationIds.Count == 0)
        {
            return [];
        }

        var ids = locationIds.Distinct().ToArray();
        var rows = await db.Locations.AsNoTracking()
            .Where(x => ids.Contains(x.LocationId))
            .ToListAsync(cancellationToken);
        return rows.Select(ToLocation).ToArray();
    }

    /// <inheritdoc />
    public async Task<InventoryLocationSnapshot?> FindLocationByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim().ToUpperInvariant();
        var row = await db.Locations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == normalized, cancellationToken);
        return row is null ? null : ToLocation(row);
    }

    /// <inheritdoc />
    public async Task<Guid?> FindFirstActiveLocationIdAsync(CancellationToken cancellationToken)
    {
        return await db.Locations.AsNoTracking()
            .Where(x => x.Status == InventoryLocationStatus.Active)
            .OrderBy(x => x.Code)
            .Select(x => (Guid?)x.LocationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StockPositionSnapshot?> FindPositionAsync(
        Guid offerId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var row = await db.Positions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OfferId == offerId && x.LocationId == locationId, cancellationToken);
        return row is null ? null : ToSnapshot(row);
    }

    /// <inheritdoc />
    public async Task<StockPositionSnapshot?> FindPositionByStockItemIdAsync(
        Guid stockItemId,
        CancellationToken cancellationToken)
    {
        var row = await db.Positions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StockItemId == stockItemId, cancellationToken);
        return row is null ? null : ToSnapshot(row);
    }

    private static StockPositionSnapshot ToSnapshot(StockPosition position) =>
        new(
            position.StockItemId,
            position.OfferId,
            position.LocationId,
            position.OnHand,
            position.Reserved,
            position.Available);

    private static InventoryLocationSnapshot ToLocation(InventoryLocation location) =>
        new(location.LocationId, location.Code, location.Name, location.Status.ToString());
}
