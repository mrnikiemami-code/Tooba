using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Admin.Operations.Services;

/// <summary>
/// Admin order lifecycle orchestration — Host-free; Contracts + Order ports only.
/// </summary>
public sealed partial class AdminOrderOperationsOrchestrator
{
    private Task<AdminOrderOpsCheckoutSnapshot?> LoadCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        _checkouts.GetAsync(checkoutId, cancellationToken);

    private Task<OrderAdminEffectiveAccess> LoadEffectiveAsync(Guid actorUserId, CancellationToken cancellationToken) =>
        _access.GetAsync(actorUserId, cancellationToken);

    /// <summary>
    /// اگر کاربر مجوز granular از خانواده‌های order/return/fulfillment دارد، همان را الزام می‌کند؛
    /// در غیر این صورت admin قدیمی (فقط tenant#view) همهٔ ops را می‌بیند.
    /// </summary>
    public static bool Has(OrderAdminEffectiveAccess effective, string permissionId)
    {
        var grants = effective.Permissions.Where(p => !p.DeniedByCeiling).ToList();
        var hasOpsFamily = grants.Any(p => OpsFamilyPrefixes.Any(prefix =>
            p.PermissionId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        if (!hasOpsFamily)
        {
            return true;
        }

        return grants.Any(p => string.Equals(p.PermissionId, permissionId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasAny(OrderAdminEffectiveAccess effective, params string[] permissionIds) =>
        permissionIds.Any(p => Has(effective, p));

    public static string Prefer(OrderAdminEffectiveAccess effective, params string[] permissionIds)
    {
        foreach (var permissionId in permissionIds)
        {
            if (Has(effective, permissionId))
            {
                return permissionId;
            }
        }

        return permissionIds[0];
    }

    public static bool CanCancel(AdminOrderOpsSellerOrderSnapshot order, FulfillmentSnapshot? fulfillment)
    {
        SellerOrderCancelFulfillmentSnapshot? gate = fulfillment is null
            ? null
            : new SellerOrderCancelFulfillmentSnapshot(
                fulfillment.Status.ToString(),
                fulfillment.Shipments.Count,
                HasDispatchedQuantity(fulfillment));
        return SellerOrderCancellationPolicy.CanCancel(order.Status, gate);
    }

    public static bool IsCheckoutCancelled(AdminOrderOpsCheckoutSnapshot group) =>
        group.SellerOrders.Count > 0
        && group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled);

    public static bool HasDispatchedQuantity(FulfillmentSnapshot? fulfillment)
    {
        if (fulfillment is null)
        {
            return false;
        }

        if (fulfillment.Status is FulfillmentOperationStatus.Dispatched
            or FulfillmentOperationStatus.InTransit
            or FulfillmentOperationStatus.Delivered)
        {
            return true;
        }

        if (fulfillment.Items.Any(item => item.QuantityShipped > 0))
        {
            return true;
        }

        return fulfillment.Shipments.Any(shipment =>
            shipment.Status != ShipmentOperationStatus.Cancelled
            && (shipment.DispatchedAt is not null
                || shipment.DeliveredAt is not null
                || shipment.Status is ShipmentOperationStatus.Dispatched
                    or ShipmentOperationStatus.InTransit
                    or ShipmentOperationStatus.Delivered));
    }

    public static bool HasDispatchedOrDelivered(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        fulfillments.Any(HasDispatchedQuantity);

    public static bool HasCompletedRefund(
        IReadOnlyList<ReturnSnapshot> returns,
        string? paymentStatus = null) =>
        returns.Any(x => x.Status == ReturnRequestOperationStatus.Completed)
        || paymentStatus == "Refunded";

    public static bool HasIrreversibleFinanceBlock(
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        string? paymentStatus = null) =>
        HasDispatchedOrDelivered(fulfillments) || HasCompletedRefund(returns, paymentStatus);

    public static bool HasStartedFulfillment(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        fulfillments.Any(f =>
            f.Status != FulfillmentOperationStatus.ReadyToFulfill
            || f.Items.Any(i => i.QuantityPacked > 0 || i.QuantityProcessing > 0)
            || f.Shipments.Any(s => s.Status != ShipmentOperationStatus.Cancelled));

    public static bool CanRestoreCancelledOrder(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null)
    {
        if (group.SellerOrders.Count == 0
            || group.SellerOrders.Any(x => x.Status != SellerOrderStatus.Cancelled)
            || group.SellerOrders.Any(x => x.CancelledFromStatus is null))
        {
            return false;
        }

        return !HasIrreversibleFinanceBlock(fulfillments, returns, paymentStatus) && !blockedBySellerPayout;
    }

    public static string RestoreForbiddenCode(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null)
    {
        if (group.SellerOrders.Any(x => x.Status != SellerOrderStatus.Cancelled))
        {
            return "order.restore.not_cancelled";
        }

        if (group.SellerOrders.Any(x => x.CancelledFromStatus is null))
        {
            return "order.restore.missing_snapshot";
        }

        if (HasCompletedRefund(returns, paymentStatus))
        {
            return "order.restore.refund_completed";
        }

        if (fulfillments.Any(f =>
            f.Status == FulfillmentOperationStatus.Delivered
            || f.Shipments.Any(s => s.DeliveredAt is not null || s.Status == ShipmentOperationStatus.Delivered)))
        {
            return "order.restore.delivered";
        }

        if (HasDispatchedOrDelivered(fulfillments))
        {
            return "order.restore.dispatched";
        }

        if (blockedBySellerPayout)
        {
            return "order.restore.seller_payout_completed";
        }

        return "order.restore.invalid_state";
    }

    public static string RestoreForbiddenMessage(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null) =>
        RestoreCodeToFa(RestoreForbiddenCode(group, fulfillments, returns, blockedBySellerPayout, paymentStatus));

    public static string RestoreCodeToFa(string code) => code switch
    {
        "order.restore.not_cancelled" => "فقط سفارش لغوشده را می‌توان بازگرداند.",
        "order.restore.missing_snapshot" => "وضعیت قبل از لغو برای بازگردانی موجود نیست.",
        "order.restore.refund_completed" => "بازگردانی پس از بازگشت وجه تکمیل‌شده مجاز نیست.",
        "order.restore.delivered" => "بازگردانی پس از تحویل مجاز نیست؛ از مرجوعی استفاده کنید.",
        "order.restore.dispatched" => "بازگردانی پس از ارسال مرسوله مجاز نیست.",
        "order.restore.seller_payout_completed" => "این سفارش به‌دلیل انجام تسویه/واریز سهم فروشنده قابل بازگردانی نیست.",
        "order.restore.inventory_failed" => "بازگردانی ممکن نیست؛ موجودی برای رزرو دوباره کافی نیست. سفارش لغوشده باقی ماند.",
        "order.restore.invalid_state" => "بازگردانی سفارش در این وضعیت مجاز نیست.",
        _ => "بازگردانی سفارش در این وضعیت مجاز نیست.",
    };

    private async Task<bool> HasSellerPayoutRestoreBlockAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        var gates = await _settlement.GetRestoreGatesAsync(sellerOrderIds, cancellationToken);
        return gates.Values.Any(x => x.HasCompletedPayoutEffect);
    }

    private static AdminOrderOperationsException MapPaymentRestoreFault(ContractOperationException ex) =>
        ex.Code switch
        {
            "payment.restore.not_manual" => new AdminOrderOperationsException(ex.Code),
            "payment.restore.already_succeeded" => new AdminOrderOperationsException(ex.Code),
            "payment.restore.invalid_state" => new AdminOrderOperationsException(ex.Code),
            _ => throw ex,
        };

    private static AdminOrderOperationsException MapPaymentUnconfirmFault(ContractOperationException ex) =>
        ex.Code switch
        {
            "payment.unconfirm.not_manual" => new AdminOrderOperationsException(ex.Code),
            "payment.unconfirm.invalid_state" => new AdminOrderOperationsException(ex.Code),
            "fulfillment.unconfirm.already_started" => new AdminOrderOperationsException(ex.Code),
            "order.payment.unconfirm.invalid_state" => new AdminOrderOperationsException(ex.Code),
            "settlement.unconfirm.payout_completed" => new AdminOrderOperationsException(ex.Code),
            _ => throw ex,
        };

    private static bool MatchesIds(AdminOrderOperationAction action, AdminOrderOperationRequest request) =>
        (request.SellerOrderId is null || action.SellerOrderId == request.SellerOrderId)
        && (request.FulfillmentId is null || action.FulfillmentId == request.FulfillmentId)
        && (request.ShipmentId is null || action.ShipmentId == request.ShipmentId)
        && (request.ReturnRequestId is null || action.ReturnRequestId == request.ReturnRequestId)
        && (request.ConsolidatedPackageId is null || action.ConsolidatedPackageId == request.ConsolidatedPackageId);

    private static Guid RequireFulfillmentId(AdminOrderOperationRequest request) =>
        request.FulfillmentId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    private static Guid RequireShipmentId(AdminOrderOperationRequest request) =>
        request.ShipmentId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    private static Guid RequireConsolidatedPackageId(AdminOrderOperationRequest request) =>
        request.ConsolidatedPackageId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    private static Guid RequireReturnRequestId(AdminOrderOperationRequest request) =>
        request.ReturnRequestId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    /// <summary>
    /// آیا هنوز تعدادی برای ایجاد محمولهٔ جدید باقی مانده (با احتساب محمولهٔ Created باز و packed).
    /// </summary>
    private static bool HasUnallocatedShipmentQuantity(FulfillmentSnapshot fulfillment)
    {
        foreach (var item in fulfillment.Items)
        {
            var openAllocated = OpenAllocated(fulfillment, item.OrderLineId);
            if (item.QuantityPacked > item.QuantityShipped + openAllocated)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasProcessableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(x => x.QuantityOrdered > x.QuantityProcessing);

    private static bool HasPackableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(x => x.QuantityProcessing > x.QuantityPacked);

    private static bool HasUnpackableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(item =>
        {
            var blocking = OpenAllocated(fulfillment, item.OrderLineId) + item.QuantityShipped;
            return item.QuantityPacked > blocking;
        });

    private static decimal OpenAllocated(FulfillmentSnapshot fulfillment, Guid orderLineId) =>
        fulfillment.Shipments
            .Where(s => s.Status == ShipmentOperationStatus.Created)
            .SelectMany(s => s.Items)
            .Where(line => line.OrderLineId == orderLineId)
            .Sum(line => line.Quantity);

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolvePackSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Where(x => x.QuantityProcessing > x.QuantityPacked)
                .Select(x => new FulfillmentSelectionCommand(x.OrderLineId, x.QuantityProcessing - x.QuantityPacked))
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var packable = item.QuantityProcessing - item.QuantityPacked;
            if (qty > packable)
            {
                throw new AdminOrderOperationsException("fulfillment.selection.qty_exceeded");
            }
        });
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolveProcessSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Where(x => x.QuantityOrdered > x.QuantityProcessing)
                .Select(x => new FulfillmentSelectionCommand(x.OrderLineId, x.QuantityOrdered - x.QuantityProcessing))
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var processable = item.QuantityOrdered - item.QuantityProcessing;
            if (qty > processable)
            {
                throw new AdminOrderOperationsException("fulfillment.selection.qty_exceeded");
            }
        });
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolveUnprocessSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Select(item =>
                {
                    var unprocessable = item.QuantityProcessing - item.QuantityPacked;
                    return unprocessable > 0
                        ? new FulfillmentSelectionCommand(item.OrderLineId, unprocessable)
                        : null;
                })
                .Where(x => x is not null)
                .Cast<FulfillmentSelectionCommand>()
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var unprocessable = item.QuantityProcessing - item.QuantityPacked;
            if (qty > unprocessable)
            {
                throw new AdminOrderOperationsException("fulfillment.selection.qty_exceeded");
            }
        });
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolveUnpackSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Select(item =>
                {
                    var blocking = OpenAllocated(snapshot, item.OrderLineId) + item.QuantityShipped;
                    var unpackable = item.QuantityPacked - blocking;
                    return unpackable > 0
                        ? new FulfillmentSelectionCommand(item.OrderLineId, unpackable)
                        : null;
                })
                .Where(x => x is not null)
                .Cast<FulfillmentSelectionCommand>()
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var blocking = OpenAllocated(snapshot, item.OrderLineId) + item.QuantityShipped;
            var unpackable = item.QuantityPacked - blocking;
            if (qty > unpackable)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }
        });
    }

    private static ShipmentLineCommand[] ResolveShipmentSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Select(item =>
                {
                    var open = OpenAllocated(snapshot, item.OrderLineId);
                    var remaining = item.QuantityPacked - item.QuantityShipped - open;
                    return remaining > 0
                        ? new ShipmentLineCommand(item.OrderLineId, remaining)
                        : null;
                })
                .Where(x => x is not null)
                .Cast<ShipmentLineCommand>()
                .ToArray();
        }

        var normalized = NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var open = OpenAllocated(snapshot, item.OrderLineId);
            var remaining = item.QuantityPacked - item.QuantityShipped - open;
            if (qty > remaining)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }
        });
        return normalized.Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToArray();
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> NormalizeAndValidateSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections,
        Action<FulfillmentItemSnapshot, decimal> validateQuantity)
    {
        var itemsByLine = snapshot.Items.ToDictionary(x => x.OrderLineId);
        var map = new Dictionary<Guid, decimal>();
        foreach (var selection in selections)
        {
            if (selection.Quantity <= 0)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            if (!itemsByLine.ContainsKey(selection.OrderLineId))
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            map[selection.OrderLineId] = map.TryGetValue(selection.OrderLineId, out var existing)
                ? existing + selection.Quantity
                : selection.Quantity;
        }

        foreach (var pair in map)
        {
            validateQuantity(itemsByLine[pair.Key], pair.Value);
        }

        return map.Select(x => new FulfillmentSelectionCommand(x.Key, x.Value)).ToArray();
    }

    public static bool SelectionsAreHomogeneousUnpackable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item))
            {
                return false;
            }

            var blocking = OpenAllocated(snapshot, item.OrderLineId) + item.QuantityShipped;
            if (item.QuantityPacked <= blocking)
            {
                return false;
            }
        }

        return true;
    }

    public static bool SelectionsAreHomogeneousPackable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item)
                || item.QuantityProcessing <= item.QuantityPacked)
            {
                return false;
            }
        }

        return true;
    }

    public static bool SelectionsAreHomogeneousProcessable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item)
                || item.QuantityOrdered <= item.QuantityProcessing)
            {
                return false;
            }
        }

        return true;
    }

    public static bool SelectionsAreHomogeneousUnprocessable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item)
                || item.QuantityProcessing <= item.QuantityPacked)
            {
                return false;
            }
        }

        return true;
    }

    public static string FulfillmentOpToFa(string code) => code switch
    {
        "fulfillment.pack.requires_processing" => "ابتدا پردازش را شروع کنید.",
        "fulfillment.pack.not_processing" => "این قلم هنوز در مرحله پردازش نیست.",
        "fulfillment.ship.not_packed" => "این قلم هنوز بسته‌بندی نشده است.",
        "fulfillment.selection.qty_exceeded" => "تعداد انتخاب‌شده بیشتر از تعداد قابل عملیات است.",
        "fulfillment.bulk.incompatible" => "ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.",
        "fulfillment.bulk.cross_seller" => "عملیات گروهی روی فروشندگان متفاوت مجاز نیست.",
        "order.cancelled.blocks_action" => "سفارش لغوشده است؛ این عملیات مجاز نیست.",
        "order.cancel.forbidden" => WholeOrderCancelBlockedAfterDispatchFa,
        "fulfillment.dispatch.invalid_state" => "ارسال در وضعیت فعلی مرسوله مجاز نیست.",
        "fulfillment.dispatch.tracking_required" => "بدون کد رهگیری نمی‌توان ارسال کرد.",
        "fulfillment.dispatch.already_dispatched" => "این مرسوله قبلاً ارسال شده است.",
        "fulfillment.tracking.duplicate" => "این کد رهگیری قبلاً ثبت شده است.",
        "fulfillment.pack.after_delivered" => "پس از تحویل کامل نمی‌توان بسته‌بندی کرد.",
        "fulfillment.process.after_delivered" => "پس از تحویل کامل نمی‌توان پردازش را ادامه داد.",
        "fulfillment.shipment.void_after_dispatch" => "پس از ارسال نمی‌توان مرسوله را ابطال کرد.",
        "fulfillment.shipment.void_invalid_state" => "ابطال مرسوله در این وضعیت مجاز نیست.",
        "fulfillment.allocation.conflict" => "تعداد از باقیماندهٔ قابل تخصیص به مرسوله بیشتر است.",
        "fulfillment.work_queue.row_mismatch" => "ردیف انتخاب‌شده با دادهٔ سرور هم‌خوان نیست.",
        "inventory.reservation.not_active" => "رزرو موجودی این سفارش دیگر فعال نیست. اطلاعات سفارش را تازه‌سازی کنید یا وضعیت رزرو را بررسی کنید.",
        "inventory.manual_review.unavailable" => "موجودی این سفارش در زمان بررسی پرداخت دیگر در دسترس نیست. لطفاً وضعیت سفارش و بازگشت وجه را بررسی کنید.",
        "inventory.reservation.not_found" => "رزرو موجودی این سفارش پیدا نشد. اطلاعات سفارش را تازه‌سازی کنید.",
        "inventory.reservation.stock_mismatch" => "مصرف رزرو با موجودی هم‌خوان نبود.",
        "fulfillment.shipment.locked_by_consolidated_package" =>
            "این مرسوله عضو بسته تجمیعی است و عملیات ارسال باید از طریق همان بسته انجام شود.",
        "fulfillment.package.requires_multi_seller" => "بسته تجمیعی حداقل به دو فروشندهٔ متمایز نیاز دارد.",
        "fulfillment.package.shipment_not_eligible" => "این مرسوله برای بسته تجمیعی واجد شرایط نیست.",
        "fulfillment.package.shipment_already_member" => "این مرسوله هم‌اکنون عضو یک بسته تجمیعی فعال است.",
        "fulfillment.package.mixed_checkout" => "فقط مرسوله‌های همین سفارش را می‌توان در یک بسته تجمیعی قرار داد.",
        "fulfillment.package.duplicate_shipment" => "مرسوله تکراری در بسته تجمیعی مجاز نیست.",
        "fulfillment.package.cancel_after_dispatch" => "پس از ارسال بسته تجمیعی، ابطال مجاز نیست.",
        "fulfillment.package.dispatch_invalid_state" => "ارسال بسته تجمیعی در وضعیت فعلی مجاز نیست.",
        "fulfillment.package.deliver_before_dispatch" => "قبل از ارسال نمی‌توان بسته تجمیعی را تحویل داد.",
        "fulfillment.package.member_state_changed" => "وضعیت مرسوله‌های عضو تغییر کرده است؛ عملیات را تازه کنید.",
        "fulfillment.package.not_found" => "بسته تجمیعی پیدا نشد.",
        "fulfillment.package.shipping_method_required" => "روش ارسال مرسوله‌های عضو برای بسته تجمیعی الزامی است.",
        "fulfillment.package.shipping_method_mismatch" =>
            "برای ایجاد بسته تجمیعی، روش ارسال مرسوله‌های انتخاب‌شده باید یکسان باشد.",
        "fulfillment.package.tracking_required" => "برای ارسال بسته تجمیعی باید کد رهگیری مرکزی ثبت شود.",
        "fulfillment.package.tracking_locked" => "پس از ارسال بسته تجمیعی، تغییر کد رهگیری مجاز نیست.",
        "fulfillment.package.checkout_required" => "شناسه سفارش برای بسته تجمیعی الزامی است.",
        _ => "این عملیات در وضعیت فعلی سفارش مجاز نیست.",
    };

    private static AdminOrderOperationAction Action(
        string code,
        string labelFa,
        string labelEn,
        Guid? sellerOrderId,
        Guid? fulfillmentId,
        Guid? shipmentId,
        Guid? returnRequestId,
        string requiredPermission,
        bool requiresConfirm,
        string? confirmMessageFa,
        Guid? orderLineId = null,
        Guid? consolidatedPackageId = null) =>
        new(
            code,
            labelFa,
            labelEn,
            sellerOrderId,
            fulfillmentId,
            shipmentId,
            returnRequestId,
            requiredPermission,
            requiresConfirm,
            confirmMessageFa,
            orderLineId,
            consolidatedPackageId);
}
