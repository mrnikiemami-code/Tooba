using Microsoft.EntityFrameworkCore;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
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
    ];

    private readonly OrderDbContext _orders;
    private readonly ReturnsDbContext _returns;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IReturnDirectory _returnDirectory;
    private readonly IReturnEligibilityEvaluator _eligibility;
    private readonly ICheckoutDirectory _checkout;
    private readonly IAccessControlDirectory _access;
    private readonly ICurrentTenant _tenant;

    /// <summary>ترکیب‌گر عملیات را به ماژول‌های موجود وصل می‌کند.</summary>
    public AdminOrderOperationsComposer(
        OrderDbContext orders,
        ReturnsDbContext returns,
        IFulfillmentDirectory fulfillment,
        IReturnDirectory returnDirectory,
        IReturnEligibilityEvaluator eligibility,
        ICheckoutDirectory checkout,
        IAccessControlDirectory access,
        ICurrentTenant tenant)
    {
        _orders = orders;
        _returns = returns;
        _fulfillment = fulfillment;
        _returnDirectory = returnDirectory;
        _eligibility = eligibility;
        _checkout = checkout;
        _access = access;
        _tenant = tenant;
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
            return request.Code.Trim().ToLowerInvariant() switch
            {
                "cancel" => await CancelAsync(group, request, cancellationToken),
                "mark_processing" => await MarkProcessingAsync(request, actorUserId, cancellationToken),
                "mark_packed" => await MarkPackedAsync(request, actorUserId, cancellationToken),
                "create_shipment" => await CreateShipmentAsync(request, actorUserId, cancellationToken),
                "assign_tracking" => await AssignTrackingAsync(request, actorUserId, cancellationToken),
                "dispatch_shipment" => await DispatchAsync(request, actorUserId, cancellationToken),
                "deliver_shipment" => await DeliverAsync(request, actorUserId, cancellationToken),
                "request_return" => await RequestReturnAsync(group, request, cancellationToken),
                "approve_return" => await ApproveReturnAsync(request, actorUserId, cancellationToken),
                "reject_return" => await RejectReturnAsync(request, actorUserId, cancellationToken),
                "retry_refund" => await RetryRefundAsync(request, actorUserId, cancellationToken),
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
                    false,
                    null));
            }

            if (fulfillment.Status is FulfillmentStatus.ReadyToFulfill or FulfillmentStatus.Processing
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "mark_packed",
                    "علامت بسته‌بندی",
                    "Mark packed",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    false,
                    null));
            }

            if (fulfillment.Status is FulfillmentStatus.Packed or FulfillmentStatus.Processing or FulfillmentStatus.ReadyToFulfill
                && fulfillment.Items.Any(i => i.QuantityOrdered > i.QuantityShipped)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "create_shipment",
                    "ایجاد محموله",
                    "Create shipment",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    false,
                    null));
            }

            foreach (var shipment in fulfillment.Shipments)
            {
                if (string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "assign_tracking",
                        "ثبت کد پیگیری",
                        "Assign tracking",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        false,
                        null));
                }

                if (!string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.DispatchedAt is null
                    && shipment.Status is ShipmentStatus.Created
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "dispatch_shipment",
                        "ارسال محموله",
                        "Dispatch shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        false,
                        null));
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
                        false,
                        null));
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
                    "مرجوعی تأیید و refund آغاز شود؟"));
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
                    "تلاش مجدد refund",
                    "Retry refund",
                    order.SellerOrderId,
                    null,
                    null,
                    ret.ReturnRequestId,
                    Prefer(effective, "order.refund", "return.manage"),
                    true,
                    "refund دوباره تلاش شود؟"));
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
        return await _fulfillment.MarkPackedAsync(fulfillmentId, actorUserId, cancellationToken);
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
        var lines = snapshot.Items
            .Where(x => x.QuantityOrdered > x.QuantityShipped)
            .Select(x => new ShipmentLineCommand(x.OrderLineId, x.QuantityOrdered - x.QuantityShipped))
            .ToArray();
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
        if (order.Status is SellerOrderStatus.PendingPayment
            or SellerOrderStatus.Submitted
            or SellerOrderStatus.ReservationRequested)
        {
            return true;
        }

        if (order.Status == SellerOrderStatus.Paid
            && fulfillment is not null
            && fulfillment.Status is FulfillmentStatus.ReadyToFulfill or FulfillmentStatus.Processing
            && fulfillment.Shipments.Count == 0)
        {
            return true;
        }

        return false;
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
