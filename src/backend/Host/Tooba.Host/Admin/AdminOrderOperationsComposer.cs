using Microsoft.EntityFrameworkCore;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;
using Tooba.Returns.Application;
using Tooba.Settlement.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Host.Returns;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب عملیات lifecycle سفارش ادمین؛ منطق دامنه در Directoryها می‌ماند.
/// </summary>
public sealed class AdminOrderOperationsComposer
{
    private static readonly HashSet<string> OpsFamilyPrefixes =
    [
        "order.",
        "return.",
        "fulfillment.",
        "refund.",
        "payment.",
    ];

    internal static readonly HashSet<string> CancelledBlockedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "confirm_deposit",
        "reject_deposit",
        "restore_deposit",
        "unconfirm_deposit",
        "mark_processing",
        "mark_packed",
        "pack_selected",
        "unprocess",
        "unpack",
        "create_shipment",
        "cancel_shipment",
        "assign_tracking",
        "correct_tracking",
        "dispatch_shipment",
        "deliver_shipment",
        "create_consolidated_package",
        "cancel_consolidated_package",
        "assign_consolidated_package_tracking",
        "dispatch_consolidated_package",
        "deliver_consolidated_package",
    };

    internal const string WholeOrderCancelBlockedAfterDispatchFa =
        "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.";

    internal const string WholeOrderCancelConfirmFa =
        "با لغو کامل سفارش، مرسوله‌های پیش از ارسال ابطال می‌شوند، موجودی آزاد می‌شود و در صورت پرداخت موفق بازگشت وجه آغاز می‌شود. آیا مطمئن هستید؟";

    private readonly OrderDbContext _orders;
    private readonly ReturnsDbContext _returns;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IReturnDirectory _returnDirectory;
    private readonly IReturnEligibilityEvaluator _eligibility;
    private readonly ICheckoutDirectory _checkout;
    private readonly IAccessControlDirectory _access;
    private readonly ICurrentTenant _tenant;
    private readonly IPaymentAdminDirectory _payments;
    private readonly IOrderPaymentProjection _orderPayments;
    private readonly ISettlementDirectory _settlement;
    private readonly ShippingMethodsOptions _shippingMethods;

    /// <summary>ترکیب‌گر عملیات را به ماژول‌های موجود وصل می‌کند.</summary>
    public AdminOrderOperationsComposer(
        OrderDbContext orders,
        ReturnsDbContext returns,
        IFulfillmentDirectory fulfillment,
        IReturnDirectory returnDirectory,
        IReturnEligibilityEvaluator eligibility,
        ICheckoutDirectory checkout,
        IAccessControlDirectory access,
        ICurrentTenant tenant,
        IPaymentAdminDirectory payments,
        IOrderPaymentProjection orderPayments,
        ISettlementDirectory settlement,
        ShippingMethodsOptions? shippingMethods = null)
    {
        _orders = orders;
        _returns = returns;
        _fulfillment = fulfillment;
        _returnDirectory = returnDirectory;
        _eligibility = eligibility;
        _checkout = checkout;
        _access = access;
        _tenant = tenant;
        _payments = payments;
        _orderPayments = orderPayments;
        _settlement = settlement;
        _shippingMethods = shippingMethods ?? new ShippingMethodsOptions();
    }

    /// <summary>اقدامات مجاز و eligibility مرجوعی یک checkout را برمی‌گرداند.</summary>
    public async Task<AdminOrderOperationsPage> ListAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");

        var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var packages = await _fulfillment.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
        var allShipmentIds = fulfillments.SelectMany(f => f.Shipments.Select(s => s.ShipmentId)).Distinct().ToArray();
        var memberships = await _fulfillment.GetActiveMembershipByShipmentIdsAsync(allShipmentIds, cancellationToken);
        var membershipByShipment = memberships.ToDictionary(x => x.ShipmentId);
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        var eligibility = new List<ReturnEligibilityResult>();
        var actions = new List<AdminOrderOperationAction>();
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        var blockedBySellerPayout = await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken);
        if (!IsCheckoutCancelled(group))
        {
            ProjectPaymentActions(actions, payment, fulfillments, returns, effective, blockedBySellerPayout);
        }
        foreach (var order in group.SellerOrders)
        {
            var elig = await _eligibility.EvaluateAsync(order.SellerOrderId, cancellationToken);
            eligibility.Add(elig);
            var fulfillment = fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId);
            ProjectActions(actions, order, fulfillment, returns, elig, effective, membershipByShipment);
        }

        ProjectWholeOrderCancel(actions, group, fulfillments, effective, blockedBySellerPayout);
        ProjectRestoreCancelledOrder(actions, group, fulfillments, returns, effective, blockedBySellerPayout, payment?.Status);
        ProjectConsolidatedPackageActions(actions, group, fulfillments, packages, membershipByShipment, effective);
        var collapsed = AdminOrderWholeOrderActions.Collapse(actions);
        var lineCaps = new List<AdminOrderLineCapability>();
        var sellerCaps = new List<AdminSellerCapability>();
        foreach (var order in group.SellerOrders)
        {
            var fulfillment = fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId);
            var projected = AdminFulfillmentCapabilityProjector.Project(order, fulfillment, collapsed);
            lineCaps.AddRange(projected.Lines);
            sellerCaps.Add(projected.Seller);
        }

        return new AdminOrderOperationsPage(checkoutId, collapsed, eligibility, lineCaps, sellerCaps);
    }

    /// <summary>eligibility همهٔ سفارش‌های فروشندهٔ یک checkout.</summary>
    public async Task<IReadOnlyList<ReturnEligibilityResult>> ListReturnEligibilityAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");
        var results = new List<ReturnEligibilityResult>();
        foreach (var order in group.SellerOrders)
        {
            results.Add(await _eligibility.EvaluateAsync(order.SellerOrderId, cancellationToken));
        }

        return results;
    }

    /// <summary>یک عملیات را پس از بررسی مجوز اجرا می‌کند.</summary>
    public async Task<object> ExecuteAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new PlatformHttpException(400, "کد عملیات نامعتبر است.", "order.operation.invalid");
        }

        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");
        var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
        var code = request.Code.Trim().ToLowerInvariant();
        if (IsCheckoutCancelled(group) && CancelledBlockedCodes.Contains(code))
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa("order.cancelled.blocks_action"), "order.cancelled.blocks_action");
        }

        // cancel / restore_deposit / unconfirm_deposit / restore_cancelled_order: projection may hide; domain remains authoritative.
        if (code == "cancel")
        {
            if (!Has(effective, "order.cancel"))
            {
                throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
            }

            try
            {
                return await CancelAsync(group, request, cancellationToken);
            }
            catch (PlatformHttpException)
            {
                throw;
            }
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("order.cancel.forbidden", StringComparison.Ordinal)
                || ex.Message == "fulfillment.cancel.already_dispatched")
            {
                throw new PlatformHttpException(400, WholeOrderCancelBlockedAfterDispatchFa, "order.cancel.forbidden");
            }
            catch (InvalidOperationException ex) when (ex.Message == "settlement.cancel.payout_completed")
            {
                throw new PlatformHttpException(
                    400,
                    "لغو پس از واریز سهم فروشنده مجاز نیست.",
                    "order.cancel.payout_completed");
            }
            catch (InvalidOperationException ex)
            {
                throw new PlatformHttpException(400, ex.Message, "order.operation.failed");
            }
        }

        if (code == "restore_deposit")
        {
            if (!Has(effective, "payment.reconcile"))
            {
                throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
            }

            try
            {
                return await RestoreDepositForCheckoutAsync(checkoutId, cancellationToken);
            }
            catch (PlatformHttpException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw MapPaymentRestoreError(ex);
            }
        }

        if (code == "unconfirm_deposit")
        {
            if (!Has(effective, "payment.reconcile"))
            {
                throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
            }

            try
            {
                return await UnconfirmDepositForCheckoutAsync(group, cancellationToken);
            }
            catch (PlatformHttpException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw MapPaymentUnconfirmError(ex);
            }
        }

        if (code == "restore_cancelled_order")
        {
            if (!HasAny(effective, "order.cancel", "order.handle"))
            {
                throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
            }

            return await RestoreCancelledOrderAsync(group, actorUserId, cancellationToken);
        }

        var page = await ListAsync(checkoutId, actorUserId, cancellationToken);
        var projected = page.Actions.FirstOrDefault(a =>
            string.Equals(a.Code, request.Code, StringComparison.OrdinalIgnoreCase)
            && MatchesIds(a, request));
        var isReturnLifecycleOp = code is "request_return" or "approve_return" or "reject_return" or "retry_refund";
        if (projected is null)
        {
            if (!isReturnLifecycleOp)
            {
                throw new PlatformHttpException(400, "این عملیات در وضعیت فعلی سفارش مجاز نیست.", "order.operation.invalid");
            }

            if (!Has(effective, "return.manage"))
            {
                throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
            }
        }
        else if (!Has(effective, projected.RequiredPermission))
        {
            throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
        }

        try
        {
            return code switch
            {
                "mark_processing" => await MarkProcessingAsync(request, actorUserId, cancellationToken),
                "mark_packed" => await MarkPackedAsync(request, actorUserId, cancellationToken, requireSelections: false),
                "pack_selected" => await MarkPackedAsync(request, actorUserId, cancellationToken, requireSelections: true),
                "unprocess" => await UnprocessAsync(request, actorUserId, cancellationToken),
                "unpack" => await UnpackAsync(request, actorUserId, cancellationToken),
                "create_shipment" => await CreateShipmentAsync(request, actorUserId, cancellationToken),
                "cancel_shipment" => await CancelShipmentAsync(request, actorUserId, cancellationToken),
                "assign_tracking" => await AssignTrackingAsync(request, actorUserId, cancellationToken),
                "correct_tracking" => await CorrectTrackingAsync(request, actorUserId, cancellationToken),
                "restore_cancelled_order" => await RestoreCancelledOrderAsync(group, actorUserId, cancellationToken),
                "dispatch_shipment" => await DispatchAsync(request, actorUserId, cancellationToken),
                "deliver_shipment" => await DeliverAsync(request, actorUserId, cancellationToken),
                "create_consolidated_package" => await CreateConsolidatedPackageAsync(checkoutId, request, actorUserId, cancellationToken),
                "cancel_consolidated_package" => await CancelConsolidatedPackageAsync(request, actorUserId, cancellationToken),
                "assign_consolidated_package_tracking" => await AssignConsolidatedPackageTrackingAsync(request, actorUserId, cancellationToken),
                "dispatch_consolidated_package" => await DispatchConsolidatedPackageAsync(request, actorUserId, cancellationToken),
                "deliver_consolidated_package" => await DeliverConsolidatedPackageAsync(request, actorUserId, cancellationToken),
                "request_return" => await RequestReturnAsync(group, request, cancellationToken),
                "approve_return" => await ApproveReturnAsync(request, actorUserId, cancellationToken),
                "reject_return" => await RejectReturnAsync(request, actorUserId, cancellationToken),
                "retry_refund" => await RetryRefundAsync(request, actorUserId, cancellationToken),
                "confirm_deposit" => await ConfirmDepositForCheckoutAsync(group, cancellationToken),
                "reject_deposit" => await RejectDepositForCheckoutAsync(checkoutId, cancellationToken),
                _ => throw new PlatformHttpException(400, "کد عملیات نامعتبر است.", "order.operation.invalid"),
            };
        }
        catch (PlatformHttpException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var mapped = MapFulfillmentException(ex.Message);
            if (mapped.Code == "order.operation.failed")
            {
                mapped = ReturnErrorMapper.Map(ex.Message);
            }

            throw new PlatformHttpException(400, mapped.Fa, mapped.Code);
        }
    }

    private void ProjectActions(
        List<AdminOrderOperationAction> actions,
        SellerOrder order,
        FulfillmentSnapshot? fulfillment,
        IReadOnlyList<ReturnRequest> returns,
        ReturnEligibilityResult eligibility,
        EffectiveAccessDto effective,
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

            if (fulfillment.Status is not (FulfillmentStatus.Cancelled or FulfillmentStatus.Failed or FulfillmentStatus.Delivered)
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

            foreach (var shipment in fulfillment.Shipments.Where(s => s.Status != ShipmentStatus.Cancelled))
            {
                var lockedByPackage = membershipByShipment.TryGetValue(shipment.ShipmentId, out var membership)
                    && membership.PackageStatus is ConsolidatedPackageStatus.Created or ConsolidatedPackageStatus.Dispatched;

                if (shipment.Status == ShipmentStatus.Created
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
                    && shipment.Status == ShipmentStatus.Created
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
                    && shipment.Status == ShipmentStatus.Created
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
                    && shipment.Status is ShipmentStatus.Created
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

                if (shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit
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
            if (ret.Status == ReturnRequestStatus.Requested && Has(effective, "return.manage"))
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

            if (ret.Status == ReturnRequestStatus.RefundFailed
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
        CheckoutGroup group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ConsolidatedPackageSnapshot> packages,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> membershipByShipment,
        EffectiveAccessDto effective)
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
                .Where(s => s.Status == ShipmentStatus.Created
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

        foreach (var package in packages.Where(p => p.Status == ConsolidatedPackageStatus.Created))
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

        foreach (var package in packages.Where(p => p.Status == ConsolidatedPackageStatus.Dispatched))
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

    private async Task<object> CancelAsync(
        CheckoutGroup group,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (HasDispatchedOrDelivered(fulfillments))
        {
            throw new PlatformHttpException(400, WholeOrderCancelBlockedAfterDispatchFa, "order.cancel.forbidden");
        }

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var alreadyCancelled = IsCheckoutCancelled(group);
        var blockedBySellerPayout = await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken);
        if (!alreadyCancelled && blockedBySellerPayout)
        {
            throw new PlatformHttpException(
                400,
                "لغو پس از واریز سهم فروشنده مجاز نیست.",
                "order.cancel.payout_completed");
        }

        var access = new OrderAccess(null, group.PlacedByUserId);
        await _fulfillment.AbortForCheckoutCancelAsync(group.CheckoutId, cancellationToken);

        var cancelled = new List<Guid>();
        foreach (var order in group.SellerOrders)
        {
            if (order.Status == SellerOrderStatus.Cancelled)
            {
                continue;
            }

            await _checkout.CancelSellerOrderAsync(order.SellerOrderId, access, cancellationToken);
            cancelled.Add(order.SellerOrderId);
        }

        if (!alreadyCancelled && cancelled.Count == 0)
        {
            throw new PlatformHttpException(400, "لغو در وضعیت فعلی سفارش مجاز نیست.", "order.cancel.forbidden");
        }

        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (payment is not null && !blockedBySellerPayout)
        {
            await _settlement.NeutralizeUnpaidAccrualForCancelAsync(
                payment.PaymentId,
                sellerOrderIds,
                cancellationToken);
        }

        await _payments.CloseOrStartRefundForOrderCancelAsync(group.CheckoutId, cancellationToken);
        return new
        {
            ok = true,
            code = "cancel",
            checkoutId = group.CheckoutId,
            sellerOrderIds = alreadyCancelled ? sellerOrderIds : cancelled,
        };
    }

    private async Task<object> MarkProcessingAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        if (request.Selections is not { Count: > 0 })
        {
            return await _fulfillment.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken);
        }

        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        if (!SelectionsAreHomogeneousProcessable(snapshot, request.Selections))
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.bulk.incompatible"), "fulfillment.bulk.incompatible");
        }

        var selections = ResolveProcessSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل پردازش باقی نمانده است.", "order.operation.invalid");
        }

        return await _fulfillment.ProcessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> UnprocessAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        if (request.Selections is { Count: > 0 } && !SelectionsAreHomogeneousUnprocessable(snapshot, request.Selections))
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.bulk.incompatible"), "fulfillment.bulk.incompatible");
        }

        var selections = ResolveUnprocessSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل برگشت از پردازش باقی نمانده است.", "order.operation.invalid");
        }

        return await _fulfillment.UnprocessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> MarkPackedAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken,
        bool requireSelections)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        if (snapshot.Status == FulfillmentStatus.ReadyToFulfill)
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.pack.requires_processing"), "fulfillment.pack.requires_processing");
        }

        if (requireSelections && (request.Selections is null || request.Selections.Count == 0))
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.bulk.incompatible"), "fulfillment.bulk.incompatible");
        }

        if (requireSelections && !SelectionsAreHomogeneousPackable(snapshot, request.Selections!))
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.bulk.incompatible"), "fulfillment.bulk.incompatible");
        }

        var selections = ResolvePackSelections(snapshot, requireSelections ? request.Selections : request.Selections);
        if (selections.Count == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل بسته‌بندی باقی نمانده است.", "order.operation.invalid");
        }

        try
        {
            return await _fulfillment.PackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("fulfillment.pack.", StringComparison.Ordinal))
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa(ex.Message), ex.Message);
        }
    }

    private async Task<object> UnpackAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        if (request.Selections is { Count: > 0 } && !SelectionsAreHomogeneousUnpackable(snapshot, request.Selections))
        {
            throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.bulk.incompatible"), "fulfillment.bulk.incompatible");
        }

        var selections = ResolveUnpackSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل بازگشت از بسته‌بندی باقی نمانده است.", "order.operation.invalid");
        }

        return await _fulfillment.UnpackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> CreateShipmentAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var methodCode = request.ShippingMethodCode?.Trim();
        var carrier = request.CarrierDisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(methodCode) && string.IsNullOrWhiteSpace(carrier))
        {
            throw new PlatformHttpException(400, "روش ارسال الزامی است.", "order.operation.invalid");
        }

        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var enabled = ShippingMethodRegistry.Enabled(_shippingMethods).Any(x =>
                string.Equals(x.Code, methodCode, StringComparison.OrdinalIgnoreCase));
            if (!enabled)
            {
                throw new PlatformHttpException(400, "روش ارسال برای این فروشگاه فعال نیست.", "order.operation.invalid");
            }

            carrier = ShippingMethodRegistry.ResolveLabel(methodCode, carrier);
        }

        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        var lines = ResolveShipmentSelections(snapshot, request.Selections);
        if (lines.Length == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل ارسال باقی نمانده است.", "order.operation.invalid");
        }

        try
        {
            return await _fulfillment.CreateShipmentAsync(
                fulfillmentId,
                actorUserId,
                carrier!,
                lines,
                cancellationToken,
                methodCode,
                request.ProviderMetadataJson);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "order.operation.invalid");
        }
    }

    private async Task<object> CancelShipmentAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.CancelShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> AssignTrackingAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new PlatformHttpException(400, "کد پیگیری الزامی است.", "order.operation.invalid");
        }

        return await _fulfillment.AssignTrackingAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            request.TrackingReference.Trim(),
            cancellationToken);
    }

    private async Task<object> CorrectTrackingAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new PlatformHttpException(400, "کد پیگیری الزامی است.", "order.operation.invalid");
        }

        try
        {
            return await _fulfillment.CorrectTrackingAsync(
                fulfillmentId,
                shipmentId,
                actorUserId,
                request.TrackingReference.Trim(),
                cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message == "fulfillment.tracking.locked_after_dispatch")
        {
            throw new PlatformHttpException(400, "پس از ارسال نمی‌توان کد رهگیری را اصلاح کرد.", ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message == "fulfillment.tracking.nothing_to_correct")
        {
            throw new PlatformHttpException(400, "ابتدا کد رهگیری را ثبت کنید.", ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message == "fulfillment.tracking.invalid_state")
        {
            throw new PlatformHttpException(400, "اصلاح کد رهگیری در این وضعیت مرسوله مجاز نیست.", ex.Message);
        }
    }

    private async Task<object> DispatchAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> DeliverAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> CreateConsolidatedPackageAsync(
        Guid checkoutId,
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var shipmentIds = request.ShipmentIds;
        if (shipmentIds is null || shipmentIds.Count == 0)
        {
            throw new PlatformHttpException(
                400,
                FulfillmentOpToFa("fulfillment.package.requires_multi_seller"),
                "fulfillment.package.requires_multi_seller");
        }

        var methodCode = request.ShippingMethodCode?.Trim();

        return await _fulfillment.CreateConsolidatedPackageAsync(
            checkoutId,
            shipmentIds,
            string.IsNullOrWhiteSpace(methodCode) ? null : methodCode,
            request.TrackingReference,
            request.Reason,
            actorUserId,
            cancellationToken);
    }

    private async Task<object> CancelConsolidatedPackageAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.CancelConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

    private async Task<object> AssignConsolidatedPackageTrackingAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new PlatformHttpException(
                400,
                FulfillmentOpToFa("fulfillment.package.tracking_required"),
                "fulfillment.package.tracking_required");
        }

        return await _fulfillment.AssignConsolidatedPackageTrackingAsync(
            packageId,
            request.TrackingReference.Trim(),
            actorUserId,
            cancellationToken);
    }

    private async Task<object> DispatchConsolidatedPackageAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.DispatchConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

    private async Task<object> DeliverConsolidatedPackageAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.DeliverConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

    private async Task<object> RequestReturnAsync(
        CheckoutGroup group,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        var sellerOrderId = request.SellerOrderId
            ?? throw new PlatformHttpException(400, "شناسه سفارش فروشنده الزامی است.", "order.operation.invalid");
        var eligibility = await _eligibility.EvaluateAsync(sellerOrderId, cancellationToken);
        if (!eligibility.Eligible)
        {
            throw new PlatformHttpException(
                400,
                ReturnEligibilityReasonCodes.ToFaMessage(eligibility.ReasonCode),
                ReturnEligibilityReasonCodes.ToErrorCode(eligibility.ReasonCode));
        }

        var items = request.ReturnItems;
        if (items is null || items.Count == 0)
        {
            items = request.Selections?
                .Where(x => x.Quantity > 0)
                .Select(x => new ReturnLineCommand(x.OrderLineId, x.Quantity))
                .ToArray();
        }

        if (items is null || items.Count == 0)
        {
            items = eligibility.Lines
                .Where(x => x.RemainingReturnableQuantity > 0)
                .Select(x => new ReturnLineCommand(x.OrderLineId, x.RemainingReturnableQuantity))
                .ToArray();
        }

        if (items.Count == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل مرجوعی باقی نمانده است.", "return.quantity_exceeded");
        }

        var idempotency = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"admin-return-{sellerOrderId:N}-{Guid.NewGuid():N}"
            : request.IdempotencyKey.Trim();
        return await _returnDirectory.CreateAdminInitiatedAsync(
            new CreateReturnCommand(
                sellerOrderId,
                group.PlacedByUserId,
                idempotency,
                request.Reason,
                items),
            cancellationToken);
    }

    private async Task<object> ApproveReturnAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var returnRequestId = RequireReturnRequestId(request);
        return await _returnDirectory.ApproveAsync(
            new ApproveReturnCommand(returnRequestId, actorUserId),
            cancellationToken);
    }

    private async Task<object> RejectReturnAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var returnRequestId = RequireReturnRequestId(request);
        return await _returnDirectory.RejectAsync(
            new RejectReturnCommand(returnRequestId, actorUserId, request.Reason),
            cancellationToken);
    }

    private void ProjectPaymentActions(
        List<AdminOrderOperationAction> actions,
        PaymentOperationalSnapshot? payment,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnRequest> returns,
        EffectiveAccessDto effective,
        bool blockedBySellerPayout)
    {
        if (payment is null || !Has(effective, "payment.reconcile"))
        {
            return;
        }

        if (payment.ConfirmDepositEligible)
        {
            actions.Add(Action(
                "confirm_deposit",
                "تأیید واریز",
                "Confirm deposit",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "آیا واریز کارت‌به‌کارت این سفارش را تأیید می‌کنید؟"));
        }

        if (payment.RejectDepositEligible)
        {
            actions.Add(Action(
                "reject_deposit",
                "رد واریز",
                "Reject deposit",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "آیا از رد واریز مطمئن هستید؟"));
        }

        if (payment.RestoreDepositEligible && !HasIrreversibleFinanceBlock(fulfillments, returns))
        {
            actions.Add(Action(
                "restore_deposit",
                "برگشت از رد واریز",
                "Undo deposit rejection",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "واریز ردشده به انتظار تأیید واریز بازگردد؟"));
        }

        if (payment.UnconfirmDepositEligible
            && !HasIrreversibleFinanceBlock(fulfillments, returns)
            && !HasStartedFulfillment(fulfillments)
            && !blockedBySellerPayout)
        {
            actions.Add(Action(
                "unconfirm_deposit",
                "برگشت از واریز",
                "Undo deposit confirmation",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "تأیید واریز این سفارش به حالت انتظار برگردد؟"));
        }
    }

    private void ProjectWholeOrderCancel(
        List<AdminOrderOperationAction> actions,
        CheckoutGroup group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        EffectiveAccessDto effective,
        bool blockedBySellerPayout)
    {
        if (!Has(effective, "order.cancel")
            || IsCheckoutCancelled(group)
            || HasDispatchedOrDelivered(fulfillments)
            || blockedBySellerPayout)
        {
            return;
        }

        var any = group.SellerOrders.Any(order =>
            order.Status != SellerOrderStatus.Cancelled
            && CanCancel(order, fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId)));
        if (!any)
        {
            return;
        }

        actions.Add(Action(
            "cancel",
            "لغو سفارش",
            "Cancel order",
            null,
            null,
            null,
            null,
            "order.cancel",
            true,
            WholeOrderCancelConfirmFa));
    }

    private void ProjectRestoreCancelledOrder(
        List<AdminOrderOperationAction> actions,
        CheckoutGroup group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnRequest> returns,
        EffectiveAccessDto effective,
        bool blockedBySellerPayout,
        PaymentStatus? paymentStatus)
    {
        if (!HasAny(effective, "order.cancel", "order.handle"))
        {
            return;
        }

        if (!CanRestoreCancelledOrder(group, fulfillments, returns, blockedBySellerPayout, paymentStatus))
        {
            return;
        }

        actions.Add(Action(
            "restore_cancelled_order",
            "بازگردانی سفارش لغوشده",
            "Restore cancelled order",
            null,
            null,
            null,
            null,
            Prefer(effective, "order.cancel", "order.handle"),
            true,
            "سفارش لغوشده بازگردانی شود؟ رزرو موجودی دوباره گرفته می‌شود."));
    }

    private async Task<object> RestoreCancelledOrderAsync(
        CheckoutGroup group,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .ToListAsync(cancellationToken);
        var blockedBySellerPayout = await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken);
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (!CanRestoreCancelledOrder(group, fulfillments, returns, blockedBySellerPayout, payment?.Status))
        {
            throw new PlatformHttpException(
                400,
                RestoreForbiddenMessage(group, fulfillments, returns, blockedBySellerPayout, payment?.Status),
                RestoreForbiddenCode(group, fulfillments, returns, blockedBySellerPayout, payment?.Status));
        }

        var paidSellerOrderIds = group.SellerOrders
            .Where(x => x.CancelledFromStatus == SellerOrderStatus.Paid)
            .Select(x => x.SellerOrderId)
            .ToList();
        try
        {
            await _payments.RestoreAfterOrderCancelRestoreAsync(group.CheckoutId, cancellationToken);
            if (payment is not null)
            {
                await _settlement.ReinstateAccrualAfterCancelRestoreAsync(
                    payment.PaymentId,
                    sellerOrderIds,
                    cancellationToken);
            }

            // ابتدا رزرو فعلی روی OrderLine ساخته می‌شود؛ سپس Fulfillment به همان مرجع فعال بازمی‌بندد.
            await _checkout.RestoreCancelledCheckoutAsync(
                group.CheckoutId,
                new OrderAccess(null, group.PlacedByUserId),
                cancellationToken);
            await _fulfillment.ReactivateAfterOrderRestoreAsync(group.CheckoutId, cancellationToken);
            if (paidSellerOrderIds.Count > 0)
            {
                await _fulfillment.EnsureCreatedForPaidCheckoutAsync(
                    group.CheckoutId,
                    paidSellerOrderIds,
                    cancellationToken);
            }

            return new { ok = true, code = "restore_cancelled_order", checkoutId = group.CheckoutId };
        }
        catch (InvalidOperationException ex) when (ex.Message == "order.restore.inventory_failed"
            || ex.Message.Contains("موجودی قابل‌فروش", StringComparison.Ordinal))
        {
            throw new PlatformHttpException(
                400,
                "بازگردانی ممکن نیست؛ موجودی برای رزرو دوباره کافی نیست. سفارش لغوشده باقی ماند.",
                "order.restore.inventory_failed");
        }
        catch (InvalidOperationException ex) when (ex.Message == "fulfillment.restore.already_dispatched")
        {
            throw new PlatformHttpException(
                400,
                RestoreCodeToFa("order.restore.dispatched"),
                "order.restore.dispatched");
        }
        catch (InvalidOperationException ex) when (ex.Message is "payment.restore.refund_completed"
            or "settlement.restore.payout_completed")
        {
            var code = ex.Message == "settlement.restore.payout_completed"
                ? "order.restore.seller_payout_completed"
                : "order.restore.refund_completed";
            throw new PlatformHttpException(400, RestoreCodeToFa(code), code);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("order.restore.", StringComparison.Ordinal))
        {
            throw new PlatformHttpException(400, RestoreCodeToFa(ex.Message), ex.Message);
        }
    }

    private async Task<object> RestoreDepositForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(400, "پرداختی برای بازگردانی پیدا نشد.", "payment.missing");
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (HasIrreversibleFinanceBlock(fulfillments, returns))
        {
            throw new PlatformHttpException(
                400,
                "بازگرداندن واریز پس از ارسال، تحویل یا بازگشت وجه تکمیل‌شده مجاز نیست.",
                "payment.restore.invalid_state");
        }

        try
        {
            return await _payments.RestoreDepositAsync(payment.PaymentId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw MapPaymentRestoreError(ex);
        }
    }

    private async Task<object> UnconfirmDepositForCheckoutAsync(
        CheckoutGroup group,
        CancellationToken cancellationToken)
    {
        var checkoutId = group.CheckoutId;
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(400, "پرداختی برای برگشت تأیید پیدا نشد.", "payment.missing");
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId) || x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (HasIrreversibleFinanceBlock(fulfillments, returns))
        {
            throw new PlatformHttpException(
                400,
                "برگشت از واریز پس از ارسال، تحویل یا بازگشت وجه تکمیل‌شده مجاز نیست.",
                "payment.unconfirm.irreversible");
        }

        if (HasStartedFulfillment(fulfillments))
        {
            throw new PlatformHttpException(
                400,
                "پس از شروع پردازش نمی‌توان تأیید پرداخت را برگرداند.",
                "fulfillment.unconfirm.already_started");
        }

        if (await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken))
        {
            throw new PlatformHttpException(
                400,
                "برگشت از واریز پس از واریز سهم فروشنده مجاز نیست.",
                "payment.unconfirm.payout_completed");
        }

        try
        {
            await _fulfillment.VoidUnstartedForCheckoutAsync(checkoutId, cancellationToken);
            await _orderPayments.RevertVerifiedSuccessAsync(checkoutId, sellerOrderIds, cancellationToken);
            await _settlement.VoidUnpaidAccrualForPaymentAsync(payment.PaymentId, sellerOrderIds, cancellationToken);
            return await _payments.UnconfirmDepositAsync(payment.PaymentId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw MapPaymentUnconfirmError(ex);
        }
    }

    private async Task<object> ConfirmDepositForCheckoutAsync(
        CheckoutGroup group,
        CancellationToken cancellationToken)
    {
        var checkoutId = group.CheckoutId;
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(400, "پرداختی برای تأیید پیدا نشد.", "payment.missing");
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        try
        {
            var result = await _payments.ConfirmDepositAsync(payment.PaymentId, cancellationToken);
            await _orderPayments.ApplyVerifiedSuccessAsync(
                checkoutId,
                payment.PaymentId,
                sellerOrderIds,
                cancellationToken);
            await _fulfillment.EnsureCreatedForPaidCheckoutAsync(checkoutId, sellerOrderIds, cancellationToken);
            return result;
        }
        catch (InvalidOperationException ex) when (ex.Message is "payment.method.not_manual")
        {
            throw new PlatformHttpException(400, "این پرداخت کارت‌به‌کارت/دستی نیست.", ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message is "payment.confirm.invalid_state")
        {
            throw new PlatformHttpException(400, "تأیید واریز در این وضعیت مجاز نیست.", ex.Message);
        }
    }

    private async Task<object> RejectDepositForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(400, "پرداختی برای رد پیدا نشد.", "payment.missing");
        try
        {
            return await _payments.RejectDepositAsync(payment.PaymentId, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message is "payment.method.not_manual")
        {
            throw new PlatformHttpException(400, "این پرداخت کارت‌به‌کارت/دستی نیست.", ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message is "payment.reject.invalid_state")
        {
            throw new PlatformHttpException(400, "رد واریز در این وضعیت مجاز نیست.", ex.Message);
        }
    }

    private async Task<object> RetryRefundAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var returnRequestId = RequireReturnRequestId(request);
        return await _returnDirectory.RetryRefundAsync(
            new RetryRefundCommand(returnRequestId, actorUserId),
            cancellationToken);
    }

    private async Task<CheckoutGroup?> LoadCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    private async Task<EffectiveAccessDto> LoadEffectiveAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.Current?.TenantId.Value;
        var scope = new AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenantId);
        return await _access.GetEffectiveAccessAsync(actorUserId, scope, cancellationToken);
    }

    /// <summary>
    /// اگر کاربر مجوز granular از خانواده‌های order/return/fulfillment دارد، همان را الزام می‌کند؛
    /// در غیر این صورت admin قدیمی (فقط tenant#view) همهٔ ops را می‌بیند.
    /// </summary>
    internal static bool Has(EffectiveAccessDto effective, string permissionId)
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

    internal static bool HasAny(EffectiveAccessDto effective, params string[] permissionIds) =>
        permissionIds.Any(p => Has(effective, p));

    internal static string Prefer(EffectiveAccessDto effective, params string[] permissionIds)
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

    internal static bool CanCancel(SellerOrder order, FulfillmentSnapshot? fulfillment)
    {
        SellerOrderCancelFulfillmentSnapshot? gate = fulfillment is null
            ? null
            : new SellerOrderCancelFulfillmentSnapshot(
                fulfillment.Status.ToString(),
                fulfillment.Shipments.Count,
                HasDispatchedQuantity(fulfillment));
        return SellerOrderCancellationPolicy.CanCancel(order.Status, gate);
    }

    internal static bool IsCheckoutCancelled(CheckoutGroup group) =>
        group.SellerOrders.Count > 0
        && group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled);

    internal static bool HasDispatchedQuantity(FulfillmentSnapshot? fulfillment)
    {
        if (fulfillment is null)
        {
            return false;
        }

        if (fulfillment.Status is FulfillmentStatus.Dispatched
            or FulfillmentStatus.InTransit
            or FulfillmentStatus.Delivered)
        {
            return true;
        }

        if (fulfillment.Items.Any(item => item.QuantityShipped > 0))
        {
            return true;
        }

        return fulfillment.Shipments.Any(shipment =>
            shipment.Status != ShipmentStatus.Cancelled
            && (shipment.DispatchedAt is not null
                || shipment.DeliveredAt is not null
                || shipment.Status is ShipmentStatus.Dispatched
                    or ShipmentStatus.InTransit
                    or ShipmentStatus.Delivered));
    }

    internal static bool HasDispatchedOrDelivered(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        fulfillments.Any(HasDispatchedQuantity);

    internal static bool HasCompletedRefund(
        IReadOnlyList<ReturnRequest> returns,
        PaymentStatus? paymentStatus = null) =>
        returns.Any(x => x.Status == ReturnRequestStatus.Completed)
        || paymentStatus == PaymentStatus.Refunded;

    internal static bool HasIrreversibleFinanceBlock(
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnRequest> returns,
        PaymentStatus? paymentStatus = null) =>
        HasDispatchedOrDelivered(fulfillments) || HasCompletedRefund(returns, paymentStatus);

    internal static bool HasStartedFulfillment(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        fulfillments.Any(f =>
            f.Status != FulfillmentStatus.ReadyToFulfill
            || f.Items.Any(i => i.QuantityPacked > 0 || i.QuantityProcessing > 0)
            || f.Shipments.Any(s => s.Status != ShipmentStatus.Cancelled));

    internal static bool CanRestoreCancelledOrder(
        CheckoutGroup group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnRequest> returns,
        bool blockedBySellerPayout = false,
        PaymentStatus? paymentStatus = null)
    {
        if (group.SellerOrders.Count == 0
            || group.SellerOrders.Any(x => x.Status != SellerOrderStatus.Cancelled)
            || group.SellerOrders.Any(x => x.CancelledFromStatus is null))
        {
            return false;
        }

        return !HasIrreversibleFinanceBlock(fulfillments, returns, paymentStatus) && !blockedBySellerPayout;
    }

    internal static string RestoreForbiddenCode(
        CheckoutGroup group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnRequest> returns,
        bool blockedBySellerPayout = false,
        PaymentStatus? paymentStatus = null)
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
            f.Status == FulfillmentStatus.Delivered
            || f.Shipments.Any(s => s.DeliveredAt is not null || s.Status == ShipmentStatus.Delivered)))
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

    internal static string RestoreForbiddenMessage(
        CheckoutGroup group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnRequest> returns,
        bool blockedBySellerPayout = false,
        PaymentStatus? paymentStatus = null) =>
        RestoreCodeToFa(RestoreForbiddenCode(group, fulfillments, returns, blockedBySellerPayout, paymentStatus));

    internal static string RestoreCodeToFa(string code) => code switch
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
        var gates = await _settlement.GetRestoreSettlementGatesAsync(sellerOrderIds, cancellationToken);
        return gates.Values.Any(x => x.HasCompletedPayoutEffect);
    }

    private static PlatformHttpException MapPaymentRestoreError(InvalidOperationException ex) =>
        ex.Message switch
        {
            "payment.restore.not_manual" =>
                new PlatformHttpException(400, "فقط پرداخت کارت‌به‌کارت/دستی قابل بازگردانی است.", ex.Message),
            "payment.restore.already_succeeded" =>
                new PlatformHttpException(400, "پرداخت موفق جایگزین شده و قابل بازگردانی نیست.", ex.Message),
            "payment.restore.invalid_state" =>
                new PlatformHttpException(400, "بازگرداندن واریز در این وضعیت مجاز نیست.", ex.Message),
            _ => new PlatformHttpException(400, ex.Message, "order.operation.failed"),
        };

    private static PlatformHttpException MapPaymentUnconfirmError(InvalidOperationException ex) =>
        ex.Message switch
        {
            "payment.unconfirm.not_manual" =>
                new PlatformHttpException(400, "فقط پرداخت کارت‌به‌کارت/دستی قابل برگشت از واریز است.", ex.Message),
            "payment.unconfirm.invalid_state" =>
                new PlatformHttpException(400, "برگشت از واریز در این وضعیت مجاز نیست.", ex.Message),
            "fulfillment.unconfirm.already_started" =>
                new PlatformHttpException(400, "پس از شروع پردازش نمی‌توان واریز را برگرداند.", ex.Message),
            "order.payment.unconfirm.invalid_state" =>
                new PlatformHttpException(400, "وضعیت سفارش برای برگشت از واریز مناسب نیست.", ex.Message),
            "settlement.unconfirm.payout_completed" =>
                new PlatformHttpException(400, "برگشت از واریز پس از واریز سهم فروشنده مجاز نیست.", ex.Message),
            _ => new PlatformHttpException(400, ex.Message, "order.operation.failed"),
        };

    private static bool MatchesIds(AdminOrderOperationAction action, AdminOrderOperationRequest request) =>
        (request.SellerOrderId is null || action.SellerOrderId == request.SellerOrderId)
        && (request.FulfillmentId is null || action.FulfillmentId == request.FulfillmentId)
        && (request.ShipmentId is null || action.ShipmentId == request.ShipmentId)
        && (request.ReturnRequestId is null || action.ReturnRequestId == request.ReturnRequestId)
        && (request.ConsolidatedPackageId is null || action.ConsolidatedPackageId == request.ConsolidatedPackageId);

    private static Guid RequireFulfillmentId(AdminOrderOperationRequest request) =>
        request.FulfillmentId
        ?? throw new PlatformHttpException(400, "شناسه fulfillment الزامی است.", "order.operation.invalid");

    private static Guid RequireShipmentId(AdminOrderOperationRequest request) =>
        request.ShipmentId
        ?? throw new PlatformHttpException(400, "شناسه محموله الزامی است.", "order.operation.invalid");

    private static Guid RequireConsolidatedPackageId(AdminOrderOperationRequest request) =>
        request.ConsolidatedPackageId
        ?? throw new PlatformHttpException(400, "شناسه بسته تجمیعی الزامی است.", "order.operation.invalid");

    private static Guid RequireReturnRequestId(AdminOrderOperationRequest request) =>
        request.ReturnRequestId
        ?? throw new PlatformHttpException(400, "شناسه درخواست مرجوعی الزامی است.", "order.operation.invalid");

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
            .Where(s => s.Status == ShipmentStatus.Created)
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
                throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.selection.qty_exceeded"), "fulfillment.selection.qty_exceeded");
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
                throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.selection.qty_exceeded"), "fulfillment.selection.qty_exceeded");
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
                throw new PlatformHttpException(400, FulfillmentOpToFa("fulfillment.selection.qty_exceeded"), "fulfillment.selection.qty_exceeded");
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
                throw new PlatformHttpException(
                    400,
                    "بازگشت از بسته‌بندی برای تعداد تخصیص‌یافته یا ارسال‌شده مجاز نیست.",
                    "order.operation.invalid");
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
                throw new PlatformHttpException(400, "تعداد از باقیماندهٔ قابل تخصیص به مرسوله بیشتر است.", "order.operation.invalid");
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
                throw new PlatformHttpException(400, "تعداد باید بزرگ‌تر از صفر باشد.", "order.operation.invalid");
            }

            if (!itemsByLine.ContainsKey(selection.OrderLineId))
            {
                throw new PlatformHttpException(400, "خط انتخاب‌شده متعلق به این فروشنده نیست.", "order.operation.invalid");
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

    internal static bool SelectionsAreHomogeneousUnpackable(
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

    internal static bool SelectionsAreHomogeneousPackable(
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

    internal static bool SelectionsAreHomogeneousProcessable(
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

    internal static bool SelectionsAreHomogeneousUnprocessable(
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

    internal static string FulfillmentOpToFa(string code) => code switch
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

    internal static (string Code, string Fa) MapFulfillmentException(string message) => message switch
    {
        "dispatch از این وضعیت مجاز نیست." =>
            ("fulfillment.dispatch.invalid_state", FulfillmentOpToFa("fulfillment.dispatch.invalid_state")),
        "dispatch بدون tracking مجاز نیست." =>
            ("fulfillment.dispatch.tracking_required", FulfillmentOpToFa("fulfillment.dispatch.tracking_required")),
        "ابطال مرسوله پس از ارسال مجاز نیست." =>
            ("fulfillment.shipment.void_after_dispatch", FulfillmentOpToFa("fulfillment.shipment.void_after_dispatch")),
        "ابطال مرسوله از این وضعیت مجاز نیست." =>
            ("fulfillment.shipment.void_invalid_state", FulfillmentOpToFa("fulfillment.shipment.void_invalid_state")),
        "fulfillment.cancel.already_dispatched" =>
            ("fulfillment.dispatch.already_dispatched", FulfillmentOpToFa("fulfillment.dispatch.already_dispatched")),
        "بسته‌بندی پس از تحویل کامل مجاز نیست." =>
            ("fulfillment.pack.after_delivered", FulfillmentOpToFa("fulfillment.pack.after_delivered")),
        "پردازش پس از تحویل کامل مجاز نیست." =>
            ("fulfillment.process.after_delivered", FulfillmentOpToFa("fulfillment.process.after_delivered")),
        "بسته‌بندی پس از ارسال مجاز نیست." =>
            ("fulfillment.pack.after_delivered", FulfillmentOpToFa("fulfillment.pack.after_delivered")),
        "پردازش پس از ارسال مجاز نیست." =>
            ("fulfillment.process.after_delivered", FulfillmentOpToFa("fulfillment.process.after_delivered")),
        "تعداد محموله از باقیمانده بسته‌بندی‌شده بیشتر است." =>
            ("fulfillment.allocation.conflict", FulfillmentOpToFa("fulfillment.allocation.conflict")),
        "این کد پیگیری قبلاً ثبت شده است." =>
            ("fulfillment.tracking.duplicate", FulfillmentOpToFa("fulfillment.tracking.duplicate")),
        "inventory.reservation.not_active" =>
            ("inventory.reservation.not_active", FulfillmentOpToFa("inventory.reservation.not_active")),
        "فقط رزرو Held قابل آزادسازی یا مصرف است." =>
            ("inventory.reservation.not_active", FulfillmentOpToFa("inventory.reservation.not_active")),
        _ when message.StartsWith("fulfillment.", StringComparison.Ordinal) =>
            (message, FulfillmentOpToFa(message)),
        _ when message.StartsWith("inventory.", StringComparison.Ordinal) =>
            (message, FulfillmentOpToFa(message)),
        _ => ("order.operation.failed", message),
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

/// <summary>
/// حذف تکرار اقدام‌های کل‌سفارش؛ seller-scoped دست نخورده می‌ماند.
/// </summary>
internal static class AdminOrderWholeOrderActions
{
    private static readonly HashSet<string> WholeOrderCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "cancel",
        "confirm_deposit",
        "reject_deposit",
        "restore_deposit",
        "unconfirm_deposit",
        "restore_cancelled_order",
    };

    /// <summary>هر کد کل‌سفارش حداکثر یک‌بار؛ ترجیح با sellerOrderId خالی.</summary>
    public static List<AdminOrderOperationAction> Collapse(IReadOnlyList<AdminOrderOperationAction> actions)
    {
        var preferred = new Dictionary<string, AdminOrderOperationAction>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in actions)
        {
            if (!WholeOrderCodes.Contains(action.Code))
            {
                continue;
            }

            if (!preferred.TryGetValue(action.Code, out var existing) || action.SellerOrderId is null)
            {
                preferred[action.Code] = action;
            }
            else
            {
                _ = existing;
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<AdminOrderOperationAction>(actions.Count);
        foreach (var action in actions)
        {
            if (WholeOrderCodes.Contains(action.Code))
            {
                if (!seen.Add(action.Code))
                {
                    continue;
                }

                result.Add(preferred[action.Code]);
                continue;
            }

            result.Add(action);
        }

        return result;
    }
}
