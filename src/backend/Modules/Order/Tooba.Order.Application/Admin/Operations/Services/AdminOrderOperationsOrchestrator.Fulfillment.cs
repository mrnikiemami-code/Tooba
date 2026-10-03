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
    private async Task<object> CancelAsync(
        AdminOrderOpsCheckoutSnapshot group,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (HasDispatchedOrDelivered(fulfillments))
        {
            throw new AdminOrderOperationsException("order.cancel.forbidden");
        }

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var alreadyCancelled = IsCheckoutCancelled(group);
        var blockedBySellerPayout = await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken);
        if (!alreadyCancelled && blockedBySellerPayout)
        {
            throw new AdminOrderOperationsException("order.cancel.payout_completed");
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
            throw new AdminOrderOperationsException("order.cancel.forbidden");
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

    private async Task<object> MarkProcessingCoreAsync(
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
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (!SelectionsAreHomogeneousProcessable(snapshot, request.Selections))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolveProcessSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.ProcessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> UnprocessCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (request.Selections is { Count: > 0 } && !SelectionsAreHomogeneousUnprocessable(snapshot, request.Selections))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolveUnprocessSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.UnprocessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> MarkPackedCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken,
        bool requireSelections)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (snapshot.Status == FulfillmentOperationStatus.ReadyToFulfill)
        {
            throw new AdminOrderOperationsException("fulfillment.pack.requires_processing");
        }

        if (requireSelections && (request.Selections is null || request.Selections.Count == 0))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        if (requireSelections && !SelectionsAreHomogeneousPackable(snapshot, request.Selections!))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolvePackSelections(snapshot, requireSelections ? request.Selections : request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.PackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> UnpackCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (request.Selections is { Count: > 0 } && !SelectionsAreHomogeneousUnpackable(snapshot, request.Selections))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolveUnpackSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.UnpackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> CreateShipmentCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var methodCode = request.ShippingMethodCode?.Trim();
        var carrier = request.CarrierDisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(methodCode) && string.IsNullOrWhiteSpace(carrier))
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var enabled = _fulfillment.ListEnabledShippingMethods().Any(x =>
                string.Equals(x.Code, methodCode, StringComparison.OrdinalIgnoreCase));
            if (!enabled)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            carrier = _fulfillment.ResolveShippingMethodLabel(methodCode, carrier);
        }

        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        var lines = ResolveShipmentSelections(snapshot, request.Selections);
        if (lines.Length == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            carrier!,
            lines,
            methodCode,
            request.ProviderMetadataJson,
            cancellationToken);
    }

    private async Task<object> CancelShipmentCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.CancelShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> AssignTrackingCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.AssignTrackingAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            request.TrackingReference.Trim(),
            cancellationToken);
    }

    private async Task<object> CorrectTrackingCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.CorrectTrackingAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            request.TrackingReference.Trim(),
            cancellationToken);
    }

    private async Task<object> DispatchCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> DeliverCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> CreateConsolidatedPackageCoreAsync(
        Guid checkoutId,
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var shipmentIds = request.ShipmentIds;
        if (shipmentIds is null || shipmentIds.Count == 0)
        {
            throw new AdminOrderOperationsException("fulfillment.package.requires_multi_seller");
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

    private async Task<object> CancelConsolidatedPackageCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.CancelConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

    private async Task<object> AssignConsolidatedPackageTrackingCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new AdminOrderOperationsException("fulfillment.package.tracking_required");
        }

        return await _fulfillment.AssignConsolidatedPackageTrackingAsync(
            packageId,
            request.TrackingReference.Trim(),
            actorUserId,
            cancellationToken);
    }

    private async Task<object> DispatchConsolidatedPackageCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.DispatchConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

    private async Task<object> DeliverConsolidatedPackageCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.DeliverConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }
}
