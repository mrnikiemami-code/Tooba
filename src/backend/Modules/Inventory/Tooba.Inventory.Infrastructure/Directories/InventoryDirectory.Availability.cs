using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Seller;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Offer.Contracts.Dtos;

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
        var values = await GetAvailabilityBatchCoreAsync([offerId], cancellationToken);
        return values.GetValueOrDefault(offerId);
    }

    async Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> ISellerOfferInventoryGateway.GetAvailabilityAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        var values = await GetAvailabilityBatchAsync(offerIds, cancellationToken);
        var allocations = await _db.OfferAllocations.AsNoTracking()
            .Where(x => offerIds.Contains(x.OfferId))
            .ToDictionaryAsync(x => x.OfferId, cancellationToken);
        return values.ToDictionary(
            x => x.Key,
            x =>
            {
                allocations.TryGetValue(x.Key, out var allocation);
                return new OfferInventorySummary(
                    x.Key,
                    x.Value.OnHand,
                    x.Value.Reserved,
                    x.Value.Available,
                    x.Value.Locations.Select(location => new SellerInventoryPosition(
                        location.StockItemId,
                        location.LocationId,
                        location.LocationCode,
                        location.LocationName,
                        location.OnHand,
                        location.Reserved,
                        location.Available,
                        location.Revision,
                        location.RequiresAdjustmentReason)).ToList(),
                    allocation?.Quantity,
                    allocation?.Held,
                    allocation?.Revision.ToString("N"));
            });
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
        var allocations = await _db.OfferAllocations.AsNoTracking()
            .Where(x => distinct.Contains(x.OfferId))
            .ToDictionaryAsync(x => x.OfferId, cancellationToken);
        var pristineIds = rows.Where(x => IsPristinePosition(x.position)).Select(x => x.position.StockItemId).ToArray();
        var touchedIds = new HashSet<Guid>();
        if (pristineIds.Length > 0)
        {
            touchedIds.UnionWith(await _db.OfferInventoryChanges.AsNoTracking().Where(x => x.StockItemId.HasValue && pristineIds.Contains(x.StockItemId.Value)).Select(x => x.StockItemId!.Value).Distinct().ToListAsync(cancellationToken));
            touchedIds.UnionWith(await _db.Reservations.AsNoTracking().Where(x => pristineIds.Contains(x.StockItemId)).Select(x => x.StockItemId).Distinct().ToListAsync(cancellationToken));
        }
        var marketplace = _commerce?.Current?.Edition.Edition == ToobaEdition.Marketplace;
        var offers = _commerce is not null ? await _offers.FindOffersBatchAsync(distinct, cancellationToken) : null;
        return rows
            .GroupBy(x => x.position.OfferId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var offer = offers?.GetValueOrDefault(group.Key);
                    // Exclude unauthorized owner rows from the projection itself, not just from sellable quantity.
                    // Otherwise their warehouse labels, physical and reserved amounts leak into Offer detail.
                    var eligible = group.Where(row => _commerce is null
                        || (offer is not null && IsLocationEligible(row.location, offer))).ToArray();
                    var needsAllocation = eligible.Any(row => row.location.OwnerKind == InventoryLocationOwnerKind.Marketplace)
                        || (marketplace && offer?.FulfillmentResponsibility == OfferFulfillmentResponsibility.Platform);
                    allocations.TryGetValue(group.Key, out var allocation);
                    var cap = allocation is not null ? Math.Max(0, allocation.Quantity - allocation.Held)
                        : needsAllocation ? 0m : decimal.MaxValue;
                    var locations = eligible.Select(row => new LocationAvailability(
                        row.position.StockItemId,
                        row.location.LocationId,
                        row.location.Code,
                        row.location.Name,
                        row.position.OnHand,
                        row.position.Reserved,
                        row.location.Status != InventoryLocationStatus.Active
                            ? 0m : Math.Min(cap, row.position.OnHand - row.position.Reserved),
                        row.position.UpdatedAt.ToString("O"),
                        !IsPristinePosition(row.position) || touchedIds.Contains(row.position.StockItemId))).ToList();
                    return new InventoryAvailability(
                        group.Key,
                        group.First().position.CatalogVariantId,
                        locations.Sum(x => x.OnHand),
                        locations.Sum(x => x.Reserved),
                        Math.Min(cap, locations.Sum(x => x.Available)),
                        locations);
                });
    }
}
