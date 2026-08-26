using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// snapshot تحویل برای Returns بدون cross-DbContext.
/// </summary>
public sealed class FulfillmentReturnBridge : IFulfillmentReturnReader
{
    private readonly FulfillmentDbContext _db;

    /// <summary>پل return را به schema fulfillment وصل می‌کند.</summary>
    public FulfillmentReturnBridge(FulfillmentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var unit = await _db.Fulfillments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken);
        if (unit is null)
        {
            return null;
        }

        var shipments = await _db.Shipments.AsNoTracking()
            .Where(x => x.FulfillmentId == unit.FulfillmentId && x.Status == ShipmentStatus.Delivered)
            .ToListAsync(cancellationToken);
        if (shipments.Count == 0)
        {
            return new FulfillmentReturnEligibilitySnapshot(sellerOrderId, new Dictionary<Guid, int>(), null);
        }

        var shipmentIds = shipments.Select(x => x.ShipmentId).ToArray();
        var shipmentItems = await _db.ShipmentItems.AsNoTracking()
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .ToListAsync(cancellationToken);
        var delivered = shipmentItems
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var lastDeliveredAt = shipments
            .Where(x => x.DeliveredAt is not null)
            .Max(x => x.DeliveredAt);
        return new FulfillmentReturnEligibilitySnapshot(sellerOrderId, delivered, lastDeliveredAt);
    }
}
