using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Admin.Detail;

/// <summary>وضعیت عملیاتی خط/واحد fulfillment برای جزئیات سفارش (Contracts-only).</summary>
public static class AdminOrderDetailFulfillmentStatus
{
    public const string PartialDispatched = "PartialDispatched";

    /// <summary>TB-261: mixed physical outcome across the parcels that carry the same order line.</summary>
    public const string MixedStatus = "Mixed";

    /// <summary>
    /// TB-261: one parcel that physically carries (part of) an order line. It is derived from the owning
    /// fulfillment facts — the browser never derives a line lifecycle of its own.
    /// </summary>
    public readonly record struct LineParcelState(
        ShipmentOperationStatus Status,
        decimal Quantity,
        bool IsResend);

    /// <summary>
    /// TB-261: the parcels that actually carry this line. A cancelled parcel moves no goods, so it can never
    /// describe the line's physical state.
    /// </summary>
    public static IReadOnlyList<LineParcelState> ParcelsOfLine(FulfillmentSnapshot fulfillment, Guid orderLineId)
    {
        ArgumentNullException.ThrowIfNull(fulfillment);
        return fulfillment.Shipments
            .Where(x => x.Status != ShipmentOperationStatus.Cancelled)
            .SelectMany(x => x.Items
                .Where(item => item.OrderLineId == orderLineId && item.Quantity > 0)
                .Select(item => new LineParcelState(x.Status, item.Quantity, x.ResendOfShipmentId is not null)))
            .ToList();
    }

    /// <summary>وضعیت نمایشی quantity-aware روی واحد fulfillment.</summary>
    public static string ComposeOperationalStatus(FulfillmentSnapshot fulfillment)
    {
        if (fulfillment.Status is FulfillmentOperationStatus.Cancelled or FulfillmentOperationStatus.Failed)
        {
            return fulfillment.Status.ToString();
        }

        var ordered = fulfillment.Items.Sum(x => x.QuantityOrdered);
        var shipped = fulfillment.Items.Sum(x => x.QuantityShipped);
        if (ordered > 0 && shipped > 0 && shipped < ordered)
        {
            return PartialDispatched;
        }

        return fulfillment.Status.ToString();
    }

    /// <summary>وضعیت عملیاتی یک خط سفارش.</summary>
    public static string? LineOperationalStatus(
        SellerOrderStatus sellerStatus,
        FulfillmentSnapshot? fulfillment,
        decimal packed,
        decimal ordered,
        decimal processing = 0,
        decimal shipped = 0,
        IReadOnlyList<LineParcelState>? lineParcels = null)
    {
        if (sellerStatus == SellerOrderStatus.Cancelled)
        {
            return "Cancelled";
        }

        if (fulfillment is null)
        {
            return sellerStatus == SellerOrderStatus.Paid ? "Paid" : "PendingPayment";
        }

        if (fulfillment.Status is FulfillmentOperationStatus.Cancelled or FulfillmentOperationStatus.Failed)
        {
            return fulfillment.Status.ToString();
        }

        // TB-261: a fully shipped line must describe ITS OWN parcels. A parcel received back at origin can
        // never keep a stale «in transit» badge, and a line whose parcels disagree is reported as mixed
        // instead of overwriting every item with one parcel's state. Shipped history itself is untouched.
        if (ordered > 0 && shipped >= ordered && lineParcels is { Count: > 0 })
        {
            var exceptions = lineParcels
                .Where(x => x.Status is ShipmentOperationStatus.NonDelivered
                    or ShipmentOperationStatus.Returning
                    or ShipmentOperationStatus.Returned)
                .ToList();
            if (exceptions.Count > 0)
            {
                var live = lineParcels
                    .Where(x => x.Status is not (ShipmentOperationStatus.NonDelivered
                        or ShipmentOperationStatus.Returning
                        or ShipmentOperationStatus.Returned))
                    .ToList();
                if (live.Count == 0)
                {
                    var distinct = exceptions.Select(x => x.Status).Distinct().ToList();
                    return distinct.Count == 1 ? distinct[0].ToString() : MixedStatus;
                }

                // The only open parcel is a replacement being prepared, so that is the honest next state.
                return live.All(x => x.Status == ShipmentOperationStatus.Created)
                    ? ShipmentOperationStatus.Created.ToString()
                    : MixedStatus;
            }
        }

        if (ordered > 0 && shipped >= ordered)
        {
            return fulfillment.Status == FulfillmentOperationStatus.Delivered
                ? "Delivered"
                : fulfillment.Status == FulfillmentOperationStatus.InTransit
                    ? "InTransit"
                    : "Dispatched";
        }

        if (shipped > 0 && shipped < ordered)
        {
            return PartialDispatched;
        }

        if (ordered > 0 && packed >= ordered)
        {
            return "Packed";
        }

        if (processing > 0 || packed > 0)
        {
            return "Processing";
        }

        return "ReadyToFulfill";
    }
}
