using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// ردیف صف کار ارسال و تحویل Admin — fulfillment/seller-group نه Customer Order خام.
/// </summary>
public sealed record AdminFulfillmentWorkQueueRow(
    Guid FulfillmentId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    string SellerDisplayName,
    string OrderReference,
    string Status,
    string RecipientName,
    string CityName,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    int ItemCount,
    int QuantityOrdered,
    int QuantityShipped,
    int ShipmentCount,
    string? PrimaryShipmentId,
    string TrackingSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> AvailableActionCodes);

/// <summary>
/// فیلترهای سریع صف کار بر اساس وضعیت واقعی دامنه — بدون SLA ساختگی.
/// </summary>
public static class AdminFulfillmentQueueFilters
{
    /// <summary>همه.</summary>
    public const string All = "all";
    /// <summary>نیازمند اقدام.</summary>
    public const string NeedsAction = "needs_action";
    /// <summary>آماده پردازش.</summary>
    public const string ReadyToProcess = "ready_to_process";
    /// <summary>آماده بسته‌بندی.</summary>
    public const string ReadyToPack = "ready_to_pack";
    /// <summary>آماده ارسال.</summary>
    public const string ReadyToShip = "ready_to_ship";
    /// <summary>بدون کد رهگیری.</summary>
    public const string MissingTracking = "missing_tracking";
    /// <summary>در مسیر.</summary>
    public const string InTransit = "in_transit";
    /// <summary>تحویل‌شده.</summary>
    public const string Delivered = "delivered";
    /// <summary>مشکل‌دار.</summary>
    public const string Problem = "problem";

    /// <summary>کدهای شناخته‌شدهٔ فیلتر سریع.</summary>
    public static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        All,
        NeedsAction,
        ReadyToProcess,
        ReadyToPack,
        ReadyToShip,
        MissingTracking,
        InTransit,
        Delivered,
        Problem,
    };

    /// <summary>
    /// آیا واحد با فیلتر سریع هم‌خوان است (پس از بارگذاری shipments برای missing_tracking/needs_action).
    /// برای فیلترهای فقط‌وضعیت می‌توان در SQL اعمال کرد.
    /// </summary>
    public static bool MatchesStatusOnly(FulfillmentStatus status, string queueFilter)
    {
        return queueFilter.Trim().ToLowerInvariant() switch
        {
            ReadyToProcess => status == FulfillmentStatus.ReadyToFulfill,
            ReadyToPack => status == FulfillmentStatus.Processing,
            ReadyToShip => status == FulfillmentStatus.Packed,
            InTransit => status is FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit,
            Delivered => status == FulfillmentStatus.Delivered,
            Problem => status == FulfillmentStatus.Failed,
            _ => true,
        };
    }

    /// <summary>آیا مرسولهٔ Created بدون کد رهگیری دارد.</summary>
    public static bool HasMissingTracking(IEnumerable<ShipmentSnapshot> shipments) =>
        shipments.Any(s =>
            s.Status == ShipmentStatus.Created
            && string.IsNullOrWhiteSpace(s.TrackingReference));

    /// <summary>
    /// نیازمند اقدام: آماده پردازش، در حال پردازش با ظرفیت بسته‌بندی، بدون رهگیری، Packed با ظرفیت ارسال، یا Failed.
    /// </summary>
    public static bool MatchesNeedsAction(
        FulfillmentStatus status,
        IReadOnlyList<ShipmentSnapshot> shipments,
        IReadOnlyList<FulfillmentItemSnapshot> items)
    {
        if (status == FulfillmentStatus.Failed || status == FulfillmentStatus.ReadyToFulfill)
        {
            return true;
        }

        if (status == FulfillmentStatus.Processing && HasPackableQuantity(items))
        {
            return true;
        }

        if (HasMissingTracking(shipments))
        {
            return true;
        }

        if (status == FulfillmentStatus.Packed && HasUnallocatedShipmentQuantity(items, shipments))
        {
            return true;
        }

        return false;
    }

    /// <summary>کدهای عملیات قابل‌اجرای گروهی بدون ورودی اضافه.</summary>
    public static readonly HashSet<string> SafeBulkActionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "mark_processing",
        "mark_packed",
        "dispatch_shipment",
        "deliver_shipment",
    };

    /// <summary>کدهای عملیاتی دامنه برای ردیف — بدون بررسی مجوز (اجرا در عملیات سفارش تأیید می‌شود).</summary>
    public static IReadOnlyList<string> ProjectActionCodes(FulfillmentSnapshot fulfillment)
    {
        var codes = new List<string>();
        if (fulfillment.Status == FulfillmentStatus.ReadyToFulfill)
        {
            codes.Add("mark_processing");
        }

        if (fulfillment.Status is FulfillmentStatus.ReadyToFulfill or FulfillmentStatus.Processing or FulfillmentStatus.Packed
            && HasPackableQuantity(fulfillment.Items))
        {
            codes.Add("mark_packed");
        }

        if (HasUnpackableQuantity(fulfillment))
        {
            codes.Add("unpack");
        }

        if (fulfillment.Status is not (FulfillmentStatus.Cancelled or FulfillmentStatus.Failed or FulfillmentStatus.Delivered)
            && HasUnallocatedShipmentQuantity(fulfillment.Items, fulfillment.Shipments))
        {
            codes.Add("create_shipment");
        }

        foreach (var shipment in fulfillment.Shipments.Where(s => s.Status != ShipmentStatus.Cancelled))
        {
            if (shipment.Status == ShipmentStatus.Created)
            {
                codes.Add("cancel_shipment");
                if (string.IsNullOrWhiteSpace(shipment.TrackingReference))
                {
                    codes.Add("assign_tracking");
                }
                else if (shipment.DispatchedAt is null)
                {
                    codes.Add("dispatch_shipment");
                }
            }

            if (shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit
                && shipment.DeliveredAt is null)
            {
                codes.Add("deliver_shipment");
            }
        }

        return codes.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>سازگاری انتخاب چندتایی برای یک کد عملیات — همان فروشنده + همه دارای کد.</summary>
    public static bool AreBulkCompatible(
        IReadOnlyList<AdminFulfillmentWorkQueueRow> rows,
        string actionCode)
    {
        if (rows.Count == 0 || string.IsNullOrWhiteSpace(actionCode))
        {
            return false;
        }

        var seller = rows[0].SellerPartyId;
        if (rows.Any(r => r.SellerPartyId != seller))
        {
            return false;
        }

        return rows.All(r => r.AvailableActionCodes.Contains(actionCode, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasPackableQuantity(IReadOnlyList<FulfillmentItemSnapshot> items) =>
        items.Any(x => x.QuantityOrdered > x.QuantityPacked);

    private static bool HasUnallocatedShipmentQuantity(
        IReadOnlyList<FulfillmentItemSnapshot> items,
        IReadOnlyList<ShipmentSnapshot> shipments)
    {
        foreach (var item in items)
        {
            var openAllocated = shipments
                .Where(s => s.Status == ShipmentStatus.Created)
                .SelectMany(s => s.Items)
                .Where(line => line.OrderLineId == item.OrderLineId)
                .Sum(line => line.Quantity);
            if (item.QuantityPacked > item.QuantityShipped + openAllocated)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnpackableQuantity(FulfillmentSnapshot fulfillment)
    {
        foreach (var item in fulfillment.Items)
        {
            var openAllocated = fulfillment.Shipments
                .Where(s => s.Status == ShipmentStatus.Created)
                .SelectMany(s => s.Items)
                .Where(line => line.OrderLineId == item.OrderLineId)
                .Sum(line => line.Quantity);
            if (item.QuantityPacked > openAllocated + item.QuantityShipped)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>درخواست اجرای گروهی روی ردیف‌های صف کار.</summary>
public sealed record AdminFulfillmentWorkQueueBulkRequest(
    string ActionCode,
    IReadOnlyList<AdminFulfillmentWorkQueueBulkItem> Items,
    string? TrackingReference = null,
    string? CarrierDisplayName = null,
    string? ShippingMethodCode = null);

/// <summary>یک هدف گروهی در صف کار.</summary>
public sealed record AdminFulfillmentWorkQueueBulkItem(
    Guid CheckoutId,
    Guid FulfillmentId,
    Guid SellerOrderId,
    Guid? ShipmentId);

/// <summary>نتیجهٔ اجرای گروهی — بدون موفقیت جزئی خاموش.</summary>
public sealed record AdminFulfillmentWorkQueueBulkResult(
    int Attempted,
    int Succeeded,
    string? ErrorCode,
    string? ErrorMessage);
