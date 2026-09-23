using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Order.Infrastructure.Admin.Fulfillment;

/// <summary>
/// Order-owned <see cref="IAdminOrderFulfillmentOperations"/> — Host-free; reuses IFulfillmentDirectory.
/// Expected business failures return stable outcome codes (no HTTP exception types / localized prose).
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

    /// <summary>Creates the operations adapter.</summary>
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
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Fail("order.operation.invalid");
        }

        var code = request.Code.Trim().ToLowerInvariant();
        var checkout = await _checkouts.GetAsync(checkoutId, cancellationToken);
        if (checkout is null)
        {
            return Fail("order.operation.invalid");
        }

        if (checkout.IsCancelled && CancelledBlockedCodes.Contains(code))
        {
            return Fail("order.cancelled.blocks_action");
        }

        if (!await _permissions.CanManageFulfillmentAsync(actorUserId, cancellationToken))
        {
            return Fail("order.operation.denied");
        }

        if (request.SellerOrderId is Guid sellerOrderId
            && sellerOrderId != Guid.Empty
            && !checkout.SellerOrderIds.Contains(sellerOrderId))
        {
            return Fail("order.operation.invalid");
        }

        try
        {
            return code switch
            {
                "mark_processing" => await MarkProcessingAsync(request, actorUserId, cancellationToken),
                "mark_packed" => await MarkPackedAsync(request, actorUserId, cancellationToken),
                "create_shipment" => await CreateShipmentAsync(request, actorUserId, cancellationToken),
                "cancel_shipment" => await CancelShipmentAsync(request, actorUserId, cancellationToken),
                "assign_tracking" => await AssignTrackingAsync(request, actorUserId, cancellationToken),
                "dispatch_shipment" => await DispatchAsync(request, actorUserId, cancellationToken),
                "deliver_shipment" => await DeliverAsync(request, actorUserId, cancellationToken),
                _ => Fail("order.operation.invalid"),
            };
        }
        catch (InvalidOperationException ex) when (TryMapStableMachineCode(ex.Message, out var mapped))
        {
            return Fail(mapped);
        }
    }

    private async Task<AdminOrderFulfillmentOperationOutcome> MarkProcessingAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.FulfillmentId is not Guid fulfillmentId)
        {
            return Fail("order.operation.invalid");
        }

        var linked = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (linked.ErrorCode is not null)
        {
            return Fail(linked.ErrorCode);
        }

        await _fulfillment.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken);
        return Ok();
    }

    private async Task<AdminOrderFulfillmentOperationOutcome> MarkPackedAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.FulfillmentId is not Guid fulfillmentId)
        {
            return Fail("order.operation.invalid");
        }

        var linked = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (linked.ErrorCode is not null)
        {
            return Fail(linked.ErrorCode);
        }

        var snapshot = linked.Snapshot!;
        if (snapshot.Status == FulfillmentStatus.ReadyToFulfill)
        {
            return Fail("fulfillment.pack.requires_processing");
        }

        var selections = snapshot.Items
            .Where(x => x.QuantityProcessing > x.QuantityPacked)
            .Select(x => new FulfillmentSelectionCommand(x.OrderLineId, x.QuantityProcessing - x.QuantityPacked))
            .ToArray();
        if (selections.Length == 0)
        {
            return Fail("order.operation.invalid");
        }

        await _fulfillment.PackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
        return Ok();
    }

    private async Task<AdminOrderFulfillmentOperationOutcome> CreateShipmentAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.FulfillmentId is not Guid fulfillmentId)
        {
            return Fail("order.operation.invalid");
        }

        var methodCode = request.ShippingMethodCode?.Trim();
        var carrier = request.CarrierDisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(methodCode) && string.IsNullOrWhiteSpace(carrier))
        {
            return Fail("order.operation.invalid");
        }

        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var enabled = ShippingMethodRegistry.Enabled(_shippingMethods).Any(x =>
                string.Equals(x.Code, methodCode, StringComparison.OrdinalIgnoreCase));
            if (!enabled)
            {
                return Fail("order.operation.invalid");
            }

            carrier = ShippingMethodRegistry.ResolveLabel(methodCode, carrier);
        }

        var linked = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (linked.ErrorCode is not null)
        {
            return Fail(linked.ErrorCode);
        }

        var lines = ResolveShipmentSelections(linked.Snapshot!);
        if (lines.Length == 0)
        {
            return Fail("order.operation.invalid");
        }

        await _fulfillment.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            carrier!,
            lines,
            cancellationToken,
            methodCode);
        return Ok();
    }

    private async Task<AdminOrderFulfillmentOperationOutcome> CancelShipmentAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.FulfillmentId is not Guid fulfillmentId || request.ShipmentId is not Guid shipmentId)
        {
            return Fail("order.operation.invalid");
        }

        var linked = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (linked.ErrorCode is not null)
        {
            return Fail(linked.ErrorCode);
        }

        await _fulfillment.CancelShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
        return Ok();
    }

    private async Task<AdminOrderFulfillmentOperationOutcome> AssignTrackingAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.FulfillmentId is not Guid fulfillmentId || request.ShipmentId is not Guid shipmentId)
        {
            return Fail("order.operation.invalid");
        }

        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            return Fail("order.operation.invalid");
        }

        var linked = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (linked.ErrorCode is not null)
        {
            return Fail(linked.ErrorCode);
        }

        await _fulfillment.AssignTrackingAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            request.TrackingReference.Trim(),
            cancellationToken);
        return Ok();
    }

    private async Task<AdminOrderFulfillmentOperationOutcome> DispatchAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.FulfillmentId is not Guid fulfillmentId || request.ShipmentId is not Guid shipmentId)
        {
            return Fail("order.operation.invalid");
        }

        var linked = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (linked.ErrorCode is not null)
        {
            return Fail(linked.ErrorCode);
        }

        await _fulfillment.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
        return Ok();
    }

    private async Task<AdminOrderFulfillmentOperationOutcome> DeliverAsync(
        AdminOrderFulfillmentOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.FulfillmentId is not Guid fulfillmentId || request.ShipmentId is not Guid shipmentId)
        {
            return Fail("order.operation.invalid");
        }

        var linked = await EnsureFulfillmentLinkedAsync(fulfillmentId, request.SellerOrderId, cancellationToken);
        if (linked.ErrorCode is not null)
        {
            return Fail(linked.ErrorCode);
        }

        await _fulfillment.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
        return Ok();
    }

    private async Task<(FulfillmentSnapshot? Snapshot, string? ErrorCode)> EnsureFulfillmentLinkedAsync(
        Guid fulfillmentId,
        Guid? sellerOrderId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken);
        if (snapshot is null)
        {
            return (null, "order.operation.invalid");
        }

        if (sellerOrderId is Guid expected && expected != Guid.Empty && snapshot.SellerOrderId != expected)
        {
            return (null, "order.operation.invalid");
        }

        return (snapshot, null);
    }

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

    /// <summary>
    /// Maps only known stable machine codes. Persian/English prose is never mapped.
    /// </summary>
    private static bool TryMapStableMachineCode(string? message, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        // Transition remap preserved for Fulfillment bulk consumers.
        if (string.Equals(message, "fulfillment.cancel.already_dispatched", StringComparison.Ordinal))
        {
            code = "fulfillment.dispatch.already_dispatched";
            return true;
        }

        if (message.StartsWith("fulfillment.", StringComparison.Ordinal)
            || message.StartsWith("inventory.", StringComparison.Ordinal)
            || message.StartsWith("shipping_service.", StringComparison.Ordinal)
            || message.StartsWith("order.", StringComparison.Ordinal))
        {
            code = message;
            return true;
        }

        return false;
    }

    private static AdminOrderFulfillmentOperationOutcome Ok() => new(true, null);

    private static AdminOrderFulfillmentOperationOutcome Fail(string errorCode) => new(false, errorCode);
}
