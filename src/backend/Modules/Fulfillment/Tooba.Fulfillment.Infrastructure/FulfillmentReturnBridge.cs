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

        var delivered = new Dictionary<Guid, int>();
        var lineDeliveredAt = new Dictionary<Guid, DateTimeOffset>();
        var slices = new List<LineDeliverySlice>();
        foreach (var shipment in shipments)
        {
            if (shipment.DeliveredAt is null)
            {
                continue;
            }

            var deliveredAt = shipment.DeliveredAt.Value;
            var shipmentItems = await _db.ShipmentItems.AsNoTracking()
                .Where(x => x.ShipmentId == shipment.ShipmentId)
                .ToListAsync(cancellationToken);
            foreach (var item in shipmentItems)
            {
                delivered[item.OrderLineId] = delivered.TryGetValue(item.OrderLineId, out var qty)
                    ? qty + item.Quantity
                    : item.Quantity;
                slices.Add(new LineDeliverySlice(item.OrderLineId, item.Quantity, deliveredAt));
                if (!lineDeliveredAt.TryGetValue(item.OrderLineId, out var existing) || deliveredAt < existing)
                {
                    // اولین تحویل خط برای سازگاری؛ ساعت هر برش از DeliveredAt همان مرسوله است.
                    lineDeliveredAt[item.OrderLineId] = deliveredAt;
                }
            }
        }

        DateTimeOffset? lastDeliveredAt = lineDeliveredAt.Count == 0
            ? null
            : lineDeliveredAt.Values.Max();

        return new FulfillmentReturnEligibilitySnapshot(
            sellerOrderId,
            delivered,
            lastDeliveredAt,
            lineDeliveredAt,
            slices);
    }
}
