using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Order.Application;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// درز Fulfillment برای قاعدهٔ لغو Paid پیش از محموله در Order.
/// </summary>
public sealed class FulfillmentSellerOrderCancelGate : ISellerOrderCancelFulfillmentGate
{
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>gate را به دایرکتوری fulfillment وصل می‌کند.</summary>
    public FulfillmentSellerOrderCancelGate(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    /// <inheritdoc />
    public async Task<SellerOrderCancelFulfillmentSnapshot?> GetAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetBySellerOrderAsync(sellerOrderId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        return new SellerOrderCancelFulfillmentSnapshot(
            snapshot.Status.ToString(),
            snapshot.Shipments.Count,
            HasDispatchedQuantity(snapshot));
    }

    internal static bool HasDispatchedQuantity(FulfillmentSnapshot snapshot) =>
        snapshot.Status is FulfillmentStatus.Dispatched
            or FulfillmentStatus.InTransit
            or FulfillmentStatus.Delivered
        || snapshot.Items.Any(item => item.QuantityShipped > 0)
        || snapshot.Shipments.Any(shipment =>
            shipment.Status != ShipmentStatus.Cancelled
            && (shipment.DispatchedAt is not null
                || shipment.DeliveredAt is not null
                || shipment.Status is ShipmentStatus.Dispatched
                    or ShipmentStatus.InTransit
                    or ShipmentStatus.Delivered));
}
