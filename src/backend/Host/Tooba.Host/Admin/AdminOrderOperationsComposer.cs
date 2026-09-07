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
using Tooba.Payment.Infrastructure;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;

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

    private readonly OrderDbContext _orders;
    private readonly ReturnsDbContext _returns;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IReturnDirectory _returnDirectory;
    private readonly IReturnEligibilityEvaluator _eligibility;
    private readonly ICheckoutDirectory _checkout;
    private readonly IAccessControlDirectory _access;
    private readonly ICurrentTenant _tenant;
    private readonly IPaymentAdminDirectory _payments;

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
        IPaymentAdminDirectory payments)
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
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        var eligibility = new List<ReturnEligibilityResult>();
        var actions = new List<AdminOrderOperationAction>();
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        ProjectPaymentActions(actions, payment, effective);
        foreach (var order in group.SellerOrders)
        {
            var elig = await _eligibility.EvaluateAsync(order.SellerOrderId, cancellationToken);
            eligibility.Add(elig);
            var fulfillment = fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId);
            ProjectActions(actions, order, fulfillment, returns, elig, effective);
        }

        return new AdminOrderOperationsPage(checkoutId, actions, eligibility);
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

        // cancel: projection may hide the action, but mutation still goes through CancelSellerOrderAsync
        // so domain/application remains authoritative (order.cancel.forbidden).
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
                ex.Message.StartsWith("order.cancel.forbidden", StringComparison.Ordinal))
            {
                throw new PlatformHttpException(400, ex.Message, "order.cancel.forbidden");
            }
            catch (InvalidOperationException ex)
            {
                throw new PlatformHttpException(400, ex.Message, "order.operation.failed");
            }
        }

        var page = await ListAsync(checkoutId, actorUserId, cancellationToken);
        var projected = page.Actions.FirstOrDefault(a =>
            string.Equals(a.Code, request.Code, StringComparison.OrdinalIgnoreCase)
            && MatchesIds(a, request));
        if (projected is null)
        {
            throw new PlatformHttpException(400, "این عملیات در وضعیت فعلی سفارش مجاز نیست.", "order.operation.invalid");
        }

        if (!Has(effective, projected.RequiredPermission))
        {
            throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
        }

        try
        {
            return code switch
            {
                "mark_processing" => await MarkProcessingAsync(request, actorUserId, cancellationToken),
                "mark_packed" => await MarkPackedAsync(request, actorUserId, cancellationToken),
                "unpack" => await UnpackAsync(request, actorUserId, cancellationToken),
                "create_shipment" => await CreateShipmentAsync(request, actorUserId, cancellationToken),
                "cancel_shipment" => await CancelShipmentAsync(request, actorUserId, cancellationToken),
                "assign_tracking" => await AssignTrackingAsync(request, actorUserId, cancellationToken),
                "dispatch_shipment" => await DispatchAsync(request, actorUserId, cancellationToken),
                "deliver_shipment" => await DeliverAsync(request, actorUserId, cancellationToken),
                "request_return" => await RequestReturnAsync(group, request, cancellationToken),
                "approve_return" => await ApproveReturnAsync(request, actorUserId, cancellationToken),
                "reject_return" => await RejectReturnAsync(request, actorUserId, cancellationToken),
                "retry_refund" => await RetryRefundAsync(request, actorUserId, cancellationToken),
                "confirm_deposit" => await ConfirmDepositForCheckoutAsync(checkoutId, cancellationToken),
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
            throw new PlatformHttpException(400, ex.Message, "order.operation.failed");
        }
    }

    private void ProjectActions(
        List<AdminOrderOperationAction> actions,
        SellerOrder order,
        FulfillmentSnapshot? fulfillment,
        IReadOnlyList<ReturnRequest> returns,
        ReturnEligibilityResult eligibility,
        EffectiveAccessDto effective)
    {
        if (CanCancel(order, fulfillment) && Has(effective, "order.cancel"))
        {
            actions.Add(Action(
                "cancel",
                "لغو سفارش",
                "Cancel order",
                order.SellerOrderId,
                null,
                null,
                null,
                "order.cancel",
                true,
                "آیا از لغو این سفارش مطمئن هستید؟"));
        }

        if (fulfillment is not null)
        {
            if (fulfillment.Status == FulfillmentStatus.ReadyToFulfill
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
            }

            if (fulfillment.Status is FulfillmentStatus.ReadyToFulfill or FulfillmentStatus.Processing or FulfillmentStatus.Packed
                && HasPackableQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "mark_packed",
                    "بسته‌بندی",
                    "Mark packed",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "بسته‌بندی این سفارش ثبت شود؟"));
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
                if (shipment.Status == ShipmentStatus.Created
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
                        "مرسوله ابطال و تخصیص آزاد شود؟"));
                }

                if (string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.Status == ShipmentStatus.Created
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
                    && shipment.Status is ShipmentStatus.Created
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

    private async Task<object> CancelAsync(
        CheckoutGroup group,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        var sellerOrderId = request.SellerOrderId
            ?? throw new PlatformHttpException(400, "شناسه سفارش فروشنده الزامی است.", "order.operation.invalid");
        await _checkout.CancelSellerOrderAsync(
            sellerOrderId,
            new OrderAccess(null, group.PlacedByUserId),
            cancellationToken);
        return new { ok = true, code = "cancel", sellerOrderId };
    }

    private async Task<object> MarkProcessingAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        return await _fulfillment.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken);
    }

    private async Task<object> MarkPackedAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        var selections = ResolvePackSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل بسته‌بندی باقی نمانده است.", "order.operation.invalid");
        }

        return await _fulfillment.PackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> UnpackAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
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
        if (string.IsNullOrWhiteSpace(request.CarrierDisplayName))
        {
            throw new PlatformHttpException(400, "نام حمل‌کننده الزامی است.", "order.operation.invalid");
        }

        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        var lines = ResolveShipmentSelections(snapshot, request.Selections);
        if (lines.Length == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل ارسال باقی نمانده است.", "order.operation.invalid");
        }

        return await _fulfillment.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            request.CarrierDisplayName.Trim(),
            lines,
            cancellationToken);
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
                "order.operation.failed");
        }

        var items = request.ReturnItems;
        if (items is null || items.Count == 0)
        {
            items = eligibility.Lines
                .Where(x => x.RemainingReturnableQuantity > 0)
                .Select(x => new ReturnLineCommand(x.OrderLineId, x.RemainingReturnableQuantity))
                .ToArray();
        }

        if (items.Count == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل مرجوعی باقی نمانده است.", "order.operation.invalid");
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
        EffectiveAccessDto effective)
    {
        if (payment is null || !payment.ConfirmDepositEligible)
        {
            return;
        }

        if (Has(effective, "payment.reconcile"))
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
        }
    }

    private async Task<object> ConfirmDepositForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(400, "پرداختی برای تأیید پیدا نشد.", "payment.missing");
        try
        {
            return await _payments.ConfirmDepositAsync(payment.PaymentId, cancellationToken);
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
                fulfillment.Shipments.Count);
        return SellerOrderCancellationPolicy.CanCancel(order.Status, gate);
    }

    private static bool MatchesIds(AdminOrderOperationAction action, AdminOrderOperationRequest request) =>
        (request.SellerOrderId is null || action.SellerOrderId == request.SellerOrderId)
        && (request.FulfillmentId is null || action.FulfillmentId == request.FulfillmentId)
        && (request.ShipmentId is null || action.ShipmentId == request.ShipmentId)
        && (request.ReturnRequestId is null || action.ReturnRequestId == request.ReturnRequestId);

    private static Guid RequireFulfillmentId(AdminOrderOperationRequest request) =>
        request.FulfillmentId
        ?? throw new PlatformHttpException(400, "شناسه fulfillment الزامی است.", "order.operation.invalid");

    private static Guid RequireShipmentId(AdminOrderOperationRequest request) =>
        request.ShipmentId
        ?? throw new PlatformHttpException(400, "شناسه محموله الزامی است.", "order.operation.invalid");

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

    private static bool HasPackableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(x => x.QuantityOrdered > x.QuantityPacked);

    private static bool HasUnpackableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(item =>
        {
            var blocking = OpenAllocated(fulfillment, item.OrderLineId) + item.QuantityShipped;
            return item.QuantityPacked > blocking;
        });

    private static int OpenAllocated(FulfillmentSnapshot fulfillment, Guid orderLineId) =>
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
                .Where(x => x.QuantityOrdered > x.QuantityPacked)
                .Select(x => new FulfillmentSelectionCommand(x.OrderLineId, x.QuantityOrdered - x.QuantityPacked))
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var packable = item.QuantityOrdered - item.QuantityPacked;
            if (qty > packable)
            {
                throw new PlatformHttpException(400, "تعداد از باقیماندهٔ قابل بسته‌بندی بیشتر است.", "order.operation.invalid");
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
        Action<FulfillmentItemSnapshot, int> validateQuantity)
    {
        var itemsByLine = snapshot.Items.ToDictionary(x => x.OrderLineId);
        var map = new Dictionary<Guid, int>();
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
        string? confirmMessageFa) =>
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
            confirmMessageFa);
}
