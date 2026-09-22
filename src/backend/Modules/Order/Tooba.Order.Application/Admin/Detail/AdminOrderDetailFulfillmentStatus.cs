using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Admin.Detail;

/// <summary>وضعیت عملیاتی خط/واحد fulfillment برای جزئیات سفارش (Contracts-only).</summary>
public static class AdminOrderDetailFulfillmentStatus
{
    public const string PartialDispatched = "PartialDispatched";

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
        decimal shipped = 0)
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
