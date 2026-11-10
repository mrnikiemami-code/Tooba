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
using Tooba.Order.Application.Storefront.Ports;

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
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");

        var checkoutGroups = await _committedShippingGroups.ListByCheckoutAsync(
            snapshot.CheckoutId,
            cancellationToken);
        var bound = AcceptedGroupShipmentCreateBinder.Resolve(snapshot, request, checkoutGroups);

        var methodCode = bound.ShippingMethodCode;
        var carrier = bound.CarrierDisplayName;
        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var enabled = _fulfillment.ListEnabledShippingMethods().Any(x =>
                string.Equals(x.Code, methodCode, StringComparison.OrdinalIgnoreCase));
            if (enabled)
            {
                carrier = _fulfillment.ResolveShippingMethodLabel(methodCode, carrier);
            }
        }

        // TB-252-R2: never forward client ProviderMetadataJson; pass trusted server-derived execution facts only.
        return (await _fulfillment.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            carrier,
            bound.Lines,
            methodCode,
            providerMetadataJson: bound.TrustedProviderMetadataJson,
            cancellationToken,
            bound.ShippingGroupId)).Snapshot;
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

        // TB-257: the canonical server suggestion is not a stateless guess — submitting it takes the owning
        // automatic allocation path, which is deterministic per parcel and retries bounded fresh candidates on a
        // real database collision. Anything the operator typed is a manual reference with strict duplicate rules.
        if (FulfillmentInternalTrackingReference.IsCanonicalSuggestion(request.TrackingReference, shipmentId))
        {
            return await _fulfillment.AssignAutomaticTrackingAsync(
                fulfillmentId, shipmentId, actorUserId, cancellationToken);
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

    /// <summary>TB-258: post-dispatch non-delivery is an internal audited fact, never a provider action.</summary>
    private async Task<object> RecordNonDeliveryCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        var reason = (request.Reason ?? string.Empty).Trim();
        if (!FulfillmentNonDeliveryReasons.IsKnown(reason))
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.RecordNonDeliveryAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            reason,
            request.Note,
            cancellationToken);
    }

    /// <summary>TB-258: the carrier started the physical return of the failed parcel to origin.</summary>
    private async Task<object> StartReturnCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.StartReturnAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    /// <summary>
    /// TB-258: confirms the physical origin receipt. Only the received usable quantity can fund a resend, so the
    /// submitted lines and conditions are validated here and the owning Fulfillment aggregate stays authoritative.
    /// </summary>
    private async Task<object> RecordReturnReceiptCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        var items = request.ReceiptItems;
        if (items is not { Count: > 0 })
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        var lines = new List<ReturnReceiptLineCommand>(items.Count);
        var seen = new HashSet<Guid>();
        foreach (var item in items)
        {
            if (item is null
                || item.OrderLineId == Guid.Empty
                || item.Quantity <= 0m
                || item.Condition is not (0 or 1)
                || !seen.Add(item.OrderLineId))
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            lines.Add(new ReturnReceiptLineCommand(
                item.OrderLineId,
                item.Quantity,
                item.Condition == 0 ? ReturnReceiptCondition.Usable : ReturnReceiptCondition.Damaged));
        }

        return await _fulfillment.RecordReturnReceiptAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            lines,
            request.Note,
            cancellationToken);
    }

    /// <summary>
    /// TB-258: allocates a new resend parcel for goods physically received back at origin. The request shipment is
    /// the RETURNED original parcel; its accepted group/method is reused by the owning aggregate, and the client
    /// idempotency key becomes the dispatch key so a retried request can never mint a second parcel.
    /// </summary>
    private async Task<object> ResendShipmentCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var originalShipmentId = RequireShipmentId(request);
        var selections = request.Selections;
        if (selections is not { Count: > 0 })
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        var lines = new List<ShipmentLineCommand>(selections.Count);
        var seen = new HashSet<Guid>();
        foreach (var selection in selections)
        {
            if (selection is null
                || selection.OrderLineId == Guid.Empty
                || selection.Quantity <= 0m
                || !seen.Add(selection.OrderLineId))
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            lines.Add(new ShipmentLineCommand(selection.OrderLineId, selection.Quantity));
        }

        var dispatchKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        return (await _fulfillment.CreateResendShipmentAsync(
            fulfillmentId,
            actorUserId,
            originalShipmentId,
            lines,
            cancellationToken,
            dispatchKey)).Snapshot;
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

        var acceptedMethod = await EnsureOperationalConsolidateAllowedAsync(
            checkoutId,
            shipmentIds,
            request.ShippingMethodCode,
            cancellationToken);

        return await _fulfillment.CreateConsolidatedPackageAsync(
            checkoutId,
            shipmentIds,
            acceptedMethod,
            request.TrackingReference,
            request.Reason,
            actorUserId,
            cancellationToken);
    }

    /// <summary>
    /// TB-253: consolidated package mutation must reuse the same Marketplace operational compatibility authority as the UI.
    /// </summary>
    private async Task<string> EnsureOperationalConsolidateAllowedAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        string? clientShippingMethodCode,
        CancellationToken cancellationToken)
    {
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var shipmentById = fulfillments
            .SelectMany(f => f.Shipments.Select(s => (Fulfillment: f, Shipment: s)))
            .ToDictionary(x => x.Shipment.ShipmentId);

        var shippingGroupIds = new List<Guid>();
        foreach (var shipmentId in shipmentIds.Distinct())
        {
            if (!shipmentById.TryGetValue(shipmentId, out var row)
                || row.Shipment.ShippingGroupId is not Guid groupId
                || groupId == Guid.Empty)
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.ConsolidateIncompatible);
            }

            shippingGroupIds.Add(groupId);
        }

        var facts = await _committedShippingGroups.ListByCheckoutAsync(checkoutId, cancellationToken);
        var history = await _shippingGroupHistory.GetForCheckoutAsync(checkoutId, cancellationToken);
        var historyById = history.ToDictionary(x => x.ShippingGroupId);
        var factsById = facts.ToDictionary(x => x.ShippingGroupId);

        var selected = new List<MarketplaceOperationalDeliveryGroupAuthority.GroupFact>();
        foreach (var groupId in shippingGroupIds.Distinct())
        {
            if (!factsById.TryGetValue(groupId, out var fact)
                || !historyById.TryGetValue(groupId, out var hist))
            {
                throw new AdminOrderOperationsException(
                    MarketplaceOperationalDeliveryGroupAuthority.ConsolidateIncompatible);
            }

            selected.Add(ToOperationalFact(checkoutId, fact, hist));
        }

        try
        {
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed(selected);
            return MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode(
                selected,
                clientShippingMethodCode);
        }
        catch (MarketplaceOperationalDeliveryGroupException ex)
        {
            throw new AdminOrderOperationsException(ex.Code);
        }
    }

    private static MarketplaceOperationalDeliveryGroupAuthority.GroupFact ToOperationalFact(
        Guid checkoutId,
        OrderCommittedCarrierShipmentFacts fact,
        OrderShippingGroupHistoryRecord hist) =>
        AdminOrderOperationalFacts.ToFact(checkoutId, fact, hist);

    /// <summary>
    /// TB-253-R2: one coherent Marketplace operational group workflow; the workflow service owns
    /// access, compatibility, parcel allocation identity and common package binding.
    /// </summary>
    private Task<object> PrepareOperationalGroupCoreAsync(
        Guid checkoutId,
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        _operationalGroupWorkflow.PrepareAsync(checkoutId, request, actorUserId, cancellationToken);

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
