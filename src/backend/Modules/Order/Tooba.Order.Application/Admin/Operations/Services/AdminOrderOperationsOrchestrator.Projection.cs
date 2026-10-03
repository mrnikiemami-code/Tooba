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
    private void ProjectActions(
        List<AdminOrderOperationAction> actions,
        AdminOrderOpsSellerOrderSnapshot order,
        FulfillmentSnapshot? fulfillment,
        IReadOnlyList<ReturnSnapshot> returns,
        ReturnEligibilityResult eligibility,
        OrderAdminEffectiveAccess effective,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> membershipByShipment)
    {
        if (order.Status != SellerOrderStatus.Cancelled && fulfillment is not null)
        {
            if (HasProcessableQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "mark_processing",
                    "شروع پردازش",
                    "Mark processing",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "شروع پردازش این سفارش؟"));
                foreach (var item in fulfillment.Items.Where(x => x.QuantityOrdered > x.QuantityProcessing))
                {
                    actions.Add(Action(
                        "mark_processing",
                        "شروع پردازش این قلم",
                        "Start processing this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "پردازش این قلم شروع شود؟",
                        item.OrderLineId));
                }
            }

            if (HasPackableQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "mark_packed",
                    "بسته‌بندی همه اقلام آماده",
                    "Pack all eligible",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "همه اقلام آماده این فروشنده بسته‌بندی شود؟"));
                actions.Add(Action(
                    "pack_selected",
                    "بسته‌بندی انتخاب‌شده‌ها",
                    "Pack selected",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "اقلام انتخاب‌شده بسته‌بندی شود؟"));
                foreach (var item in fulfillment.Items.Where(x => x.QuantityProcessing > x.QuantityPacked))
                {
                    actions.Add(Action(
                        "pack_selected",
                        "بسته‌بندی این قلم",
                        "Pack this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "این قلم بسته‌بندی شود؟",
                        item.OrderLineId));
                    actions.Add(Action(
                        "unprocess",
                        "برگشت از پردازش این قلم",
                        "Unprocess this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "برگشت از پردازش این قلم؟",
                        item.OrderLineId));
                }

                actions.Add(Action(
                    "unprocess",
                    "برگشت از پردازش",
                    "Unprocess",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "برگشت از پردازش برای اقلام بسته‌بندی‌نشده؟"));
            }

            if (HasUnpackableQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "unpack",
                    "بازگشت از بسته‌بندی",
                    "Unpack",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "بازگشت از بسته‌بندی برای اقلام تخصیص‌نشده؟"));
                foreach (var item in fulfillment.Items)
                {
                    var blocking = OpenAllocated(fulfillment, item.OrderLineId) + item.QuantityShipped;
                    if (item.QuantityPacked <= blocking)
                    {
                        continue;
                    }

                    actions.Add(Action(
                        "unpack",
                        "بازگشت از بسته‌بندی",
                        "Unpack this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "بازگشت از بسته‌بندی این قلم؟",
                        item.OrderLineId));
                }
            }

            if (fulfillment.Status is not (FulfillmentOperationStatus.Cancelled or FulfillmentOperationStatus.Failed or FulfillmentOperationStatus.Delivered)
                && HasUnallocatedShipmentQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "create_shipment",
                    "ایجاد مرسوله",
                    "Create shipment",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "مرسوله برای این سفارش ایجاد شود؟"));
            }

            foreach (var shipment in fulfillment.Shipments.Where(s => s.Status != ShipmentOperationStatus.Cancelled))
            {
                var lockedByPackage = membershipByShipment.TryGetValue(shipment.ShipmentId, out var membership)
                    && membership.PackageStatus is ConsolidatedPackageOperationStatus.Created or ConsolidatedPackageOperationStatus.Dispatched;

                if (shipment.Status == ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "cancel_shipment",
                        "ابطال مرسوله",
                        "Cancel shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "مرسوله ابطال شود؟ تخصیص آزاد می‌شود و کد رهگیری در تاریخچه می‌ماند."));
                }

                if (string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.Status == ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "assign_tracking",
                        "ثبت کد رهگیری",
                        "Assign tracking",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "کد رهگیری برای مرسوله ثبت شود؟"));
                }

                if (!string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.DispatchedAt is null
                    && shipment.Status == ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "correct_tracking",
                        "اصلاح کد رهگیری",
                        "Correct tracking",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "کد رهگیری مرسوله اصلاح شود؟ مقدار قبلی در تاریخچه می‌ماند."));
                }

                if (!string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.DispatchedAt is null
                    && shipment.Status is ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                    "dispatch_shipment",
                    "ارسال مرسوله",
                    "Dispatch shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "ارسال مرسوله ثبت شود؟"));
                }

                if (shipment.Status is ShipmentOperationStatus.Dispatched or ShipmentOperationStatus.InTransit
                    && shipment.DeliveredAt is null
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "deliver_shipment",
                        "ثبت تحویل",
                        "Deliver shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "تحویل مرسوله ثبت شود؟"));
                }
            }
        }

        if (eligibility.Eligible && Has(effective, "return.manage"))
        {
            actions.Add(Action(
                "request_return",
                "درخواست مرجوعی",
                "Request return",
                order.SellerOrderId,
                fulfillment?.FulfillmentId,
                null,
                null,
                "return.manage",
                true,
                "درخواست مرجوعی برای این سفارش ثبت شود؟"));
        }

        foreach (var ret in returns.Where(x => x.SellerOrderId == order.SellerOrderId))
        {
            if (ret.Status == ReturnRequestOperationStatus.Requested && Has(effective, "return.manage"))
            {
                actions.Add(Action(
                    "approve_return",
                    "تأیید مرجوعی",
                    "Approve return",
                    order.SellerOrderId,
                    null,
                    null,
                    ret.ReturnRequestId,
                    "return.manage",
                    true,
                    "مرجوعی تأیید و بازگشت وجه آغاز شود؟"));
                actions.Add(Action(
                    "reject_return",
                    "رد مرجوعی",
                    "Reject return",
                    order.SellerOrderId,
                    null,
                    null,
                    ret.ReturnRequestId,
                    "return.manage",
                    true,
                    "مرجوعی رد شود؟"));
            }

            if (ret.Status == ReturnRequestOperationStatus.RefundFailed
                && HasAny(effective, "order.refund", "return.manage"))
            {
                actions.Add(Action(
                    "retry_refund",
                    "تلاش مجدد برای بازگشت وجه",
                    "Retry refund",
                    order.SellerOrderId,
                    null,
                    null,
                    ret.ReturnRequestId,
                    Prefer(effective, "order.refund", "return.manage"),
                    true,
                    "بازگشت وجه دوباره تلاش شود؟"));
            }
        }
    }

    private void ProjectConsolidatedPackageActions(
        List<AdminOrderOperationAction> actions,
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ConsolidatedPackageSnapshot> packages,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> membershipByShipment,
        OrderAdminEffectiveAccess effective)
    {
        if (IsCheckoutCancelled(group))
        {
            return;
        }

        var sellerCount = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().Count();
        if (sellerCount < 2 || !HasAny(effective, "order.handle", "fulfillment.manage"))
        {
            return;
        }

        var permission = Prefer(effective, "order.handle", "fulfillment.manage");
        var eligibleByMethod = fulfillments
            .SelectMany(f => f.Shipments
                .Where(s => s.Status == ShipmentOperationStatus.Created
                    && s.DispatchedAt is null
                    && !string.IsNullOrWhiteSpace(s.ShippingMethodCode)
                    && !membershipByShipment.ContainsKey(s.ShipmentId))
                .Select(s => (f.SellerPartyId, Method: s.ShippingMethodCode.Trim())))
            .GroupBy(x => x.Method, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Select(x => x.SellerPartyId).Distinct().Count() >= 2);
        if (eligibleByMethod)
        {
            actions.Add(Action(
                "create_consolidated_package",
                "ایجاد بسته تجمیعی",
                "Create consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                "بسته تجمیعی برای مرسوله‌های انتخاب‌شده ایجاد شود؟"));
        }

        foreach (var package in packages.Where(p => p.Status == ConsolidatedPackageOperationStatus.Created))
        {
            actions.Add(Action(
                "cancel_consolidated_package",
                "ابطال بسته تجمیعی",
                "Cancel consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                $"بسته {package.PackageNumber} ابطال شود؟ عضویت مرسوله‌ها آزاد می‌شود.",
                consolidatedPackageId: package.ConsolidatedPackageId));
            if (string.IsNullOrWhiteSpace(package.TrackingReference))
            {
                actions.Add(Action(
                    "assign_consolidated_package_tracking",
                    "ثبت کد رهگیری بسته",
                    "Assign consolidated package tracking",
                    null,
                    null,
                    null,
                    null,
                    permission,
                    true,
                    $"کد رهگیری مرکزی برای بسته {package.PackageNumber} ثبت شود؟",
                    consolidatedPackageId: package.ConsolidatedPackageId));
            }

            actions.Add(Action(
                "dispatch_consolidated_package",
                "ارسال بسته تجمیعی",
                "Dispatch consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                $"ارسال مرکزی بسته {package.PackageNumber} ثبت شود؟",
                consolidatedPackageId: package.ConsolidatedPackageId));
        }

        foreach (var package in packages.Where(p => p.Status == ConsolidatedPackageOperationStatus.Dispatched))
        {
            actions.Add(Action(
                "deliver_consolidated_package",
                "تحویل بسته تجمیعی",
                "Deliver consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                $"تحویل مرکزی بسته {package.PackageNumber} ثبت شود؟",
                consolidatedPackageId: package.ConsolidatedPackageId));
        }
    }
}
