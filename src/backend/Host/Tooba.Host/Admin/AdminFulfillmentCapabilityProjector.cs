using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Order.Domain;

namespace Tooba.Host.Admin;

/// <summary>TB-P09-T012: قابلیت‌های انتخاب/عملیات از وضعیت فروشنده + fulfillment + projection.</summary>
internal static class AdminFulfillmentCapabilityProjector
{
    internal const string PaymentLockedFa =
        "پرداخت این بخش از سفارش هنوز تأیید نشده است؛ پس از تأیید پرداخت، عملیات پردازش و ارسال فعال می‌شود.";

    internal static (IReadOnlyList<AdminOrderLineCapability> Lines, AdminSellerCapability Seller) Project(
        SellerOrder order,
        FulfillmentSnapshot? fulfillment,
        IReadOnlyList<AdminOrderOperationAction> projected)
    {
        var sellerActions = projected.Where(a => a.SellerOrderId == order.SellerOrderId).ToList();
        var paymentLocked = order.Status is SellerOrderStatus.PendingPayment
            or SellerOrderStatus.Submitted
            or SellerOrderStatus.ReservationRequested;
        var cancelled = order.Status == SellerOrderStatus.Cancelled;

        if (paymentLocked || cancelled || fulfillment is null)
        {
            var lines = order.Lines.Select(line => new AdminOrderLineCapability(
                line.LineId,
                order.SellerOrderId,
                false,
                0,
                [],
                [],
                0,
                cancelled ? "order.cancelled.blocks_action" : "order.payment.pending",
                cancelled ? "سفارش لغوشده است؛ این عملیات مجاز نیست." : PaymentLockedFa)).ToList();
            return (lines, new AdminSellerCapability(
                order.SellerOrderId,
                false,
                paymentLocked,
                paymentLocked ? PaymentLockedFa : null,
                [],
                false));
        }

        var lineCaps = new List<AdminOrderLineCapability>();
        foreach (var line in order.Lines)
        {
            var item = fulfillment.Items.FirstOrDefault(x => x.OrderLineId == line.LineId);
            var packed = item?.QuantityPacked ?? 0;
            var shipped = item?.QuantityShipped ?? 0;
            var openAllocated = fulfillment.Shipments
                .Where(s => s.Status == ShipmentStatus.Created)
                .SelectMany(s => s.Items)
                .Where(i => i.OrderLineId == line.LineId)
                .Sum(i => i.Quantity);
            var packable = Math.Max(0, (item?.QuantityOrdered ?? line.Quantity) - packed);
            var unpackable = Math.Max(0, packed - openAllocated - shipped);
            var shippable = Math.Max(0, packed - openAllocated);
            var row = new List<string>();
            var bulk = new List<string>();

            if (fulfillment.Status == FulfillmentStatus.ReadyToFulfill
                && sellerActions.Any(a => a.Code == "mark_processing"))
            {
                row.Add("mark_processing");
                bulk.Add("mark_processing");
            }

            if (fulfillment.Status is FulfillmentStatus.Processing or FulfillmentStatus.Packed
                && packable > 0
                && sellerActions.Any(a => a.Code == "pack_selected"))
            {
                row.Add("pack_selected");
                bulk.Add("pack_selected");
            }

            if (unpackable > 0 && sellerActions.Any(a => a.Code == "unpack"))
            {
                row.Add("unpack");
                bulk.Add("unpack");
            }

            if (shippable > 0 && sellerActions.Any(a => a.Code == "create_shipment"))
            {
                bulk.Add("create_shipment");
            }

            var selectable = row.Count > 0 || bulk.Count > 0;
            var max = 0;
            if (row.Contains("pack_selected")) max = Math.Max(max, packable);
            if (row.Contains("unpack")) max = Math.Max(max, unpackable);
            if (bulk.Contains("create_shipment")) max = Math.Max(max, shippable);
            if (row.Contains("mark_processing")) max = Math.Max(max, line.Quantity);

            lineCaps.Add(new AdminOrderLineCapability(
                line.LineId,
                order.SellerOrderId,
                selectable,
                selectable ? Math.Max(1, max) : 0,
                row,
                bulk,
                shippable));
        }

        var whole = new List<string>();
        if (sellerActions.Any(a => a.Code == "mark_processing")) whole.Add("mark_processing");
        if (sellerActions.Any(a => a.Code == "mark_packed")) whole.Add("mark_packed");
        if (sellerActions.Any(a => a.Code == "unpack")) whole.Add("unpack");
        var shipmentPossible = sellerActions.Any(a => a.Code == "create_shipment")
            && lineCaps.Any(x => x.ShipmentEligibleQuantity > 0);
        return (lineCaps, new AdminSellerCapability(
            order.SellerOrderId,
            lineCaps.Any(x => x.Selectable),
            false,
            null,
            whole,
            shipmentPossible));
    }
}
