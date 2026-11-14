using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Shipping;
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
            // TB-266: new physical work (processing, packing, parcel creation, individual/consolidated dispatch) is only
            // offered once the order carries authoritative supply/payment authorization — `SellerOrderStatus.Paid`,
            // which is written solely by the owning payment confirmation/manual-verification path. A pending-payment
            // order keeps read-only visibility of what is already recorded, but never a new dispatch action; the
            // fulfillment aggregate enforces the same rule, so hiding the action is not the only guard.
            var supplyAuthorized = order.Status == SellerOrderStatus.Paid;

            if (supplyAuthorized
                && HasProcessableQuantity(fulfillment)
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

            if (supplyAuthorized
                && HasPackableQuantity(fulfillment)
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
                    if (FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(fulfillment, item) <= 0)
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

            if (supplyAuthorized
                && fulfillment.Status is not (FulfillmentOperationStatus.Cancelled or FulfillmentOperationStatus.Failed or FulfillmentOperationStatus.Delivered)
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

                if (supplyAuthorized
                    && !string.IsNullOrWhiteSpace(shipment.TrackingReference)
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

                    // TB-258: a dispatched parcel that was not delivered keeps its dispatch history and gets an
                    // explicit non-delivery fact instead of a fake delivery or a post-dispatch cancellation.
                    actions.Add(Action(
                        "record_non_delivery",
                        "ثبت عدم تحویل",
                        "Record non-delivery",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "عدم تحویل مرسوله ثبت شود؟ ارسال و کد رهگیری حفظ می‌شود."));
                }

                if (shipment.Status == ShipmentOperationStatus.NonDelivered
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "start_return",
                        "شروع برگشت به مبدأ",
                        "Start return",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "برگشت مرسوله به مبدأ ثبت شود؟"));
                }

                if (shipment.Status == ShipmentOperationStatus.Returning
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "record_return_receipt",
                        "تأیید دریافت در مبدأ",
                        "Confirm origin receipt",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "دریافت فیزیکی مرسوله در مبدأ تأیید شود؟"));
                }

                if (shipment.Status == ShipmentOperationStatus.Returned
                    && shipment.ReturnReceipt is not null
                    // TB-258-R1: capacity is the source parcel's own received usable quantity, so a sibling returned
                    // parcel can never make this one resendable.
                    && shipment.Items.Any(x => x.ResendableQuantity > 0)
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "resend_shipment",
                        "ارسال مجدد",
                        "Resend shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "مرسولهٔ جدید برای اقلام سالم دریافتی ایجاد شود؟"));
                }

                // TB-262: damaged units are never resend capacity, and they are not a resolved fact either. Each
                // unresolved damaged unit of THIS returned parcel needs ONE deliberate operator decision.
                // TB-262-R1: a decision that is durably recorded but still OWES its replacement parcel (no stock yet,
                // crash/cancelled attempt) stays actionable from the SERVER-OWNED facts, even after the outstanding
                // damaged quantity has been consumed by that very decision — so a reload can resume the exact
                // recorded occurrence instead of forcing a new incompatible decision.
                var pendingDamagedReplacement = shipment.DamagedResolutions?.Any(x =>
                    x.Outcome == DamagedResolutionOutcome.Replacement
                    && x.Status is DamagedResolutionStatus.Recorded or DamagedResolutionStatus.Reserved
                    && x.ReplacementShipmentId is null) == true;
                var outstandingDamaged = shipment.Items.Any(x => x.OutstandingDamagedQuantity > 0m);
                if (shipment.Status == ShipmentOperationStatus.Returned
                    && shipment.ReturnReceipt is not null
                    && (outstandingDamaged || pendingDamagedReplacement)
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "resolve_damaged_goods",
                        outstandingDamaged ? "تعیین تکلیف کالای آسیب‌دیده" : "ادامهٔ تعیین تکلیف کالای آسیب‌دیده",
                        "Resolve damaged goods",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        outstandingDamaged
                            ? "تصمیم کالای آسیب‌دیده (جایگزینی از موجودی سالم یا ارجاع) ثبت شود؟"
                            : "همان تصمیم ثبت‌شدهٔ کالای آسیب‌دیده ادامه یابد تا مرسولهٔ جایگزین ساخته شود؟"));
                }

                // TB-265: the third lawful outcome of a courier-return receipt — a Returns case linked to the existing
                // Returns review/refund/disposition workflow. It is offered only while no active courier-origin case
                // already holds this receipt, and it never replaces the healthy resend or the replacement path.
                var activeCourierCase = returns.Any(x =>
                    x.Origin == ReturnOriginOption.CourierOrigin
                    && x.SourceShipmentId == shipment.ShipmentId
                    && x.Status is ReturnRequestOperationStatus.Requested
                        or ReturnRequestOperationStatus.Approved
                        or ReturnRequestOperationStatus.RefundProcessing
                        or ReturnRequestOperationStatus.RefundFailed);
                if (shipment.Status == ShipmentOperationStatus.Returned
                    && shipment.ReturnReceipt is not null
                    && !activeCourierCase
                    && shipment.Items.Any(x => x.ResendableQuantity > 0m || x.OutstandingDamagedQuantity > 0m)
                    && Has(effective, "return.manage"))
                {
                    actions.Add(Action(
                        "create_courier_return",
                        "ایجاد پروندهٔ مرجوعی",
                        "Create return case",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        "return.manage",
                        true,
                        "برای همین مقدار دریافتی پروندهٔ مرجوعی ساخته شود؟ پرونده وارد گردش تأیید/بازگشت وجه موجود می‌شود و موجودی فعال سفارش آزاد نمی‌شود."));
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

    private async Task ProjectConsolidatedPackageActionsAsync(
        List<AdminOrderOperationAction> actions,
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ConsolidatedPackageSnapshot> packages,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> membershipByShipment,
        OrderAdminEffectiveAccess effective,
        CancellationToken cancellationToken)
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
        var eligibleShipments = fulfillments
            .SelectMany(f => f.Shipments
                .Where(s => s.Status == ShipmentOperationStatus.Created
                    && s.DispatchedAt is null
                    && !string.IsNullOrWhiteSpace(s.ShippingMethodCode)
                    && s.ShippingGroupId is Guid
                    && !membershipByShipment.ContainsKey(s.ShipmentId))
                .Select(s => (f.SellerPartyId, Shipment: s)))
            .ToList();

        var canOfferCreate = false;
        var facts = await _committedShippingGroups.ListByCheckoutAsync(group.CheckoutId, cancellationToken);
        var history = await _shippingGroupHistory.GetForCheckoutAsync(group.CheckoutId, cancellationToken);
        var historyById = history.ToDictionary(x => x.ShippingGroupId);
        var operationalFacts = new List<MarketplaceOperationalDeliveryGroupAuthority.GroupFact>();
        foreach (var row in facts)
        {
            if (historyById.TryGetValue(row.ShippingGroupId, out var hist))
            {
                operationalFacts.Add(ToOperationalFact(group.CheckoutId, row, hist));
            }
        }

        var projectedAll = MarketplaceOperationalDeliveryGroupAuthority.Project(operationalFacts);
        var eligibleGroupIds = eligibleShipments
            .Where(s => s.Shipment.ShippingGroupId is Guid)
            .Select(s => s.Shipment.ShippingGroupId!.Value)
            .ToHashSet();
        var anyPaymentLocked = facts.Any(f => f.PaymentLocked);

        if (eligibleShipments.Select(x => x.SellerPartyId).Distinct().Count() >= 2)
        {
            canOfferCreate = projectedAll.Any(op =>
            {
                if (!op.IsCrossSellerMarketplace)
                {
                    return false;
                }

                var memberIds = op.Members.Select(m => m.ShippingGroupId).ToHashSet();
                var everyReady = memberIds.All(id => eligibleGroupIds.Contains(id));
                var ready = MarketplaceOperationalDeliveryGroupAuthority.WithConsolidateReadiness(
                    op,
                    everyReady,
                    actorMayManage: true,
                    anyPaymentLocked);
                return ready.ConsolidateEligible;
            });
        }

        // TB-253-R2: one group-level workflow from reviewed remaining packed items and/or reusable parcels.
        var workflowReady = projectedAll.Any(op =>
            op.IsCrossSellerMarketplace
            && MarketplaceOperationalDeliveryGroupAuthority.WithGroupWorkflowReadiness(
                op,
                eligibleGroupIds,
                actorMayManage: true,
                anyPaymentLocked).ConsolidateEligible);
        if (workflowReady)
        {
            actions.Add(Action(
                "prepare_operational_group",
                "آماده‌سازی ارسال گروه",
                "Prepare group shipment",
                null,
                null,
                null,
                null,
                permission,
                true,
                "مرسوله‌های اعضای این گروه آماده شوند؟",
                packingModes:
                [
                    // TB-259: the server offers BOTH physical packing choices for one compatible group.
                    OperationalGroupPackingModes.Common,
                    OperationalGroupPackingModes.Separate,
                ]));
        }

        if (canOfferCreate)
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
            // TB-266: dispatching a consolidated package physically moves stock for every member seller, so it is only
            // offered when each member seller's order carries authoritative payment/supply authorization. The
            // fulfillment aggregate enforces the same rule per member before any reservation is consumed.
            var memberParties = package.Members
                .Where(m => m.ReleasedAt is null)
                .Select(m => m.SellerPartyId)
                .ToHashSet();
            var packageSupplyAuthorized = memberParties.Count > 0
                && memberParties.All(party => group.SellerOrders.Any(o =>
                    o.SellerPartyId == party
                    && o.Status == SellerOrderStatus.Paid));

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

            if (packageSupplyAuthorized)
            {
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
