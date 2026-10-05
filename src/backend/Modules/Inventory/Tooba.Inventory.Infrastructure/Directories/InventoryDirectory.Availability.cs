using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Seller;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Availability read projections for <see cref="InventoryDirectory"/>: the single-offer and batched
/// availability queries (same-schema <c>Positions ⋈ Locations</c> join only, no foreign table is
/// touched) plus the Offer-facing summary projection. Extracted into a cohesive partial of the same
/// class so the read-model shape has one reason to change and behavior is unchanged.
/// </summary>
public sealed partial class InventoryDirectory
{
    private async Task<InventoryAvailability?> GetAvailabilityCoreAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var rows = await (
            from position in _db.Positions.AsNoTracking()
            join location in _db.Locations.AsNoTracking() on position.LocationId equals location.LocationId
            where position.OfferId == offerId
            select new { position, location }).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var locations = rows.Select(row => new LocationAvailability(
            row.position.StockItemId,
            row.location.LocationId,
            row.location.Code,
            row.position.OnHand,
            row.position.Reserved,
            row.position.OnHand - row.position.Reserved)).ToList();
        return new InventoryAvailability(
            offerId,
            rows[0].position.CatalogVariantId,
            locations.Sum(x => x.OnHand),
            locations.Sum(x => x.Reserved),
            locations.Sum(x => x.Available),
            locations);
    }

    async Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> ISellerOfferInventoryGateway.GetAvailabilityAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        var values = await GetAvailabilityBatchAsync(offerIds, cancellationToken);
        return values.ToDictionary(
            x => x.Key,
            x => new OfferInventorySummary(x.Key, x.Value.OnHand, x.Value.Reserved, Math.Max(0, x.Value.Available)));
    }

    private async Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityBatchCoreAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        if (offerIds is null || offerIds.Count == 0)
        {
            return new Dictionary<Guid, InventoryAvailability>();
        }

        var distinct = offerIds.Distinct().ToArray();
        var rows = await (
            from position in _db.Positions.AsNoTracking()
            join location in _db.Locations.AsNoTracking() on position.LocationId equals location.LocationId
            where distinct.Contains(position.OfferId)
            select new { position, location }).ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.position.OfferId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var locations = group.Select(row => new LocationAvailability(
                        row.position.StockItemId,
                        row.location.LocationId,
                        row.location.Code,
                        row.position.OnHand,
                        row.position.Reserved,
                        row.position.OnHand - row.position.Reserved)).ToList();
                    return new InventoryAvailability(
                        group.Key,
                        group.First().position.CatalogVariantId,
                        locations.Sum(x => x.OnHand),
                        locations.Sum(x => x.Reserved),
                        locations.Sum(x => x.Available),
                        locations);
                });
    }
}
