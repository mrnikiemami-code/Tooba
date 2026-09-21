using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Order.Infrastructure.Fulfillment;

/// <summary>
/// Order-owned <see cref="IAdminOrderFulfillmentOperations"/> — Host-free; reuses IFulfillmentDirectory.
/// </summary>
public sealed class AdminOrderFulfillmentOperations : IAdminOrderFulfillmentOperations
{
    private static readonly HashSet<string> CancelledBlockedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "mark_processing",
        "mark_packed",
        "create_shipment",
        "cancel_shipment",
        "assign_tracking",
        "dispatch_shipment",
        "deliver_shipment",
    };

    private readonly IAdminOrderFulfillmentCheckoutReader _checkouts;
    private readonly IAdminOrderFulfillmentPermissionGate _permissions;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly ShippingMethodsOptions _shippingMethods;

    /// <summary>Operations را می‌سازد.</summary>
    public AdminOrderFulfillmentOperations(
        IAdminOrderFulfillmentCheckoutReader checkouts,
        IAdminOrderFulfillmentPermissionGate permissions,
        IFulfillmentDirectory fulfillment,
        ShippingMethodsOptions? shippingMethods = null)
    {
        _checkouts = checkouts;
        _permissions = permissions;
        _fulfillment = fulfillment;
        _shippingMethods = shippingMethods ?? new ShippingMethodsOptions();
    }

    /// <inheritdoc />
    public async Task<AdminOrderFulfillmentOperationOutcome> TryExecuteAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderFulfillmentOperationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(checkoutId, actorUserId, request, cancellationToken);
            return new AdminOrderFulfillmentOperationOutcome(true, null);
        }
        catch (PlatformHttpException ex)
        {
            return new AdminOrderFulfillmentOperationOutcome(false, ex.ErrorCode);
        }
        catch (InvalidOperationException)
        {
            return new AdminOrderFulfillmentOperationOutcome(false, null);
        }
    }

    private async Task ExecuteAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderFulfillmentOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new PlatformHttpException(400, "کد عملیات نامعتبر است.", "order.operation.invalid");
        }

        var code = request.Code.Trim().ToLowerInvariant();
        var checkout = await _checkouts.GetAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");

        if (checkout.IsCancelled && CancelledBlockedCodes.Contains(code))
        {
            throw new PlatformHttpException(
                400,
                "سفارش لغو شده است و این عملیات مجاز نیست.",
                "order.cancelled.blocks_action");
        }

        if (!await _permissions.CanManageFulfillmentAsync(actorUserId, cancellationToken))
        {
            throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
        }

        if (request.SellerOrderId is Guid sellerOrderId
            && sellerOrderId != Guid.Empty
            && !checkout.SellerOrderIds.Contains(sellerOrderId))
        {
            throw new PlatformHttpException(400, "این عملیات در وضعیت فعلی سفارش مجاز نیست.", "order.operation.invalid");
        }

        try
        {
            switch (code)
            {
                case "mark_processing":
                    await MarkProcessingAsync(request, actorUserId, cancellationToken);
                    return;
                case "mark_packed":
                    await MarkPackedAsync(request, actorUserId, cancellationToken);
                    return;
                case "create_shipment":
                    await CreateShipmentAsync(request, actorUserId, cancellationToken);
                    return;
                case "cancel_shipment":
                    await CancelShipmentAsync(request, actorUserId, cancellationToken);
                    return;
                case "assign_tracking":
                    await AssignTrackingAsync(request, actorUserId, cancellationToken);
                    return;
                case "dispatch_shipment":
                    await DispatchAsync(request, actorUserId, cancellationToken);
                    return;
                case "deliver_shipment":
                    await DeliverAsync(request, actorUserId, cancellationToken);
                    return;
                default:
                    throw new PlatformHttpException(400, "کد عملیات نامعتبر است.", "order.operation.invalid");
            }
        }
        catch (PlatformHttpException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var mapped = MapFulfillmentException(ex.Message);
            throw new PlatformHttpException(400, mapped.Fa, mapped.Code);
        }
    }

    private async Task MarkProcessingAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        await _fulfillment.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken);
    }

    private async Task MarkPackedAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (snapshot.Status == FulfillmentStatus.ReadyToFulfill)
        {
            throw new PlatformHttpException(
                400,
                "ابتدا پردازش را شروع کنید.",
                "fulfillment.pack.requires_processing");
        }

        var selections = snapshot.Items
            .Where(x => x.QuantityProcessing > x.QuantityPacked)
            .Select(x => new FulfillmentSelectionCommand(x.OrderLineId, x.QuantityProcessing - x.QuantityPacked))
            .ToArray();
        if (selections.Length == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل بسته‌بندی باقی نمانده است.", "order.operation.invalid");
        }

        try
        {
            await _fulfillment.PackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("fulfillment.pack.", StringComparison.Ordinal))
        {
            throw new PlatformHttpException(400, ex.Message, ex.Message);
        }
    }

    private async Task CreateShipmentAsync(
        AdminOrderFulfillmentOperationRequest request,
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

        var snapshot = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        var lines = ResolveShipmentSelections(snapshot);
        if (lines.Length == 0)
        {
            throw new PlatformHttpException(400, "قلم قابل ارسال باقی نمانده است.", "order.operation.invalid");
        }

        try
        {
            await _fulfillment.CreateShipmentAsync(
                fulfillmentId,
                actorUserId,
                carrier!,
                lines,
                cancellationToken,
                methodCode);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "order.operation.invalid");
        }
    }

    private async Task CancelShipmentAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        await _fulfillment.CancelShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task AssignTrackingAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new PlatformHttpException(400, "کد پیگیری الزامی است.", "order.operation.invalid");
        }

        await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        await _fulfillment.AssignTrackingAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            request.TrackingReference.Trim(),
            cancellationToken);
    }

    private async Task DispatchAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        await _fulfillment.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task DeliverAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        await _fulfillment.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<FulfillmentSnapshot> EnsureFulfillmentLinkedAsync(
        Guid fulfillmentId,
        Guid? sellerOrderId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new PlatformHttpException(404, "fulfillment پیدا نشد.", "order.operation.invalid");
        if (sellerOrderId is Guid expected && expected != Guid.Empty && snapshot.SellerOrderId != expected)
        {
            throw new PlatformHttpException(400, "این عملیات در وضعیت فعلی سفارش مجاز نیست.", "order.operation.invalid");
        }

        return snapshot;
    }

    private static Guid RequireFulfillmentId(AdminOrderFulfillmentOperationRequest request) =>
        request.FulfillmentId
        ?? throw new PlatformHttpException(400, "شناسه fulfillment الزامی است.", "order.operation.invalid");

    private static Guid RequireShipmentId(AdminOrderFulfillmentOperationRequest request) =>
        request.ShipmentId
        ?? throw new PlatformHttpException(400, "شناسه محموله الزامی است.", "order.operation.invalid");

    private static ShipmentLineCommand[] ResolveShipmentSelections(FulfillmentSnapshot snapshot) =>
        snapshot.Items
            .Select(item =>
            {
                var open = snapshot.Shipments
                    .Where(s => s.Status != ShipmentStatus.Cancelled)
                    .SelectMany(s => s.Items)
                    .Where(line => line.OrderLineId == item.OrderLineId)
                    .Sum(line => line.Quantity);
                var remaining = item.QuantityPacked - item.QuantityShipped - open;
                return remaining > 0
                    ? new ShipmentLineCommand(item.OrderLineId, remaining)
                    : null;
            })
            .Where(x => x is not null)
            .Cast<ShipmentLineCommand>()
            .ToArray();

    private static (string Code, string Fa) MapFulfillmentException(string message) => message switch
    {
        "dispatch از این وضعیت مجاز نیست." =>
            ("fulfillment.dispatch.invalid_state", "ارسال از این وضعیت مجاز نیست."),
        "dispatch بدون tracking مجاز نیست." =>
            ("fulfillment.dispatch.tracking_required", "ارسال بدون کد رهگیری مجاز نیست."),
        "ابطال مرسوله پس از ارسال مجاز نیست." =>
            ("fulfillment.shipment.void_after_dispatch", "ابطال مرسوله پس از ارسال مجاز نیست."),
        "ابطال مرسوله از این وضعیت مجاز نیست." =>
            ("fulfillment.shipment.void_invalid_state", "ابطال مرسوله از این وضعیت مجاز نیست."),
        "fulfillment.cancel.already_dispatched" =>
            ("fulfillment.dispatch.already_dispatched", "این مرسوله قبلاً ارسال شده است."),
        "بسته‌بندی پس از تحویل کامل مجاز نیست." =>
            ("fulfillment.pack.after_delivered", "بسته‌بندی پس از تحویل کامل مجاز نیست."),
        "پردازش پس از تحویل کامل مجاز نیست." =>
            ("fulfillment.process.after_delivered", "پردازش پس از تحویل کامل مجاز نیست."),
        "بسته‌بندی پس از ارسال مجاز نیست." =>
            ("fulfillment.pack.after_delivered", "بسته‌بندی پس از ارسال مجاز نیست."),
        "پردازش پس از ارسال مجاز نیست." =>
            ("fulfillment.process.after_delivered", "پردازش پس از ارسال مجاز نیست."),
        "تعداد محموله از باقیمانده بسته‌بندی‌شده بیشتر است." =>
            ("fulfillment.allocation.conflict", "تعداد محموله از باقیمانده بسته‌بندی‌شده بیشتر است."),
        "این کد پیگیری قبلاً ثبت شده است." =>
            ("fulfillment.tracking.duplicate", "این کد پیگیری قبلاً ثبت شده است."),
        _ when message.StartsWith("fulfillment.", StringComparison.Ordinal) => (message, message),
        _ when message.StartsWith("inventory.", StringComparison.Ordinal) => (message, message),
        _ => ("order.operation.failed", message),
    };
}
