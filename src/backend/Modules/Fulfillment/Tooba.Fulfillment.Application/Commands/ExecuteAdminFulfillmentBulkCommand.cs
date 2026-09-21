using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Fulfillment.Application.Commands;

/// <summary>اجرای گروهی صف کار Fulfillment.</summary>
public sealed record ExecuteAdminFulfillmentBulkCommand(
    Guid ActorUserId,
    AdminFulfillmentWorkQueueBulkRequest Request)
    : IRequest<Result<AdminFulfillmentWorkQueueBulkResult>>;

/// <summary>Handler اجرای گروهی — مالکیت Application برای validation/orchestration.</summary>
public sealed class ExecuteAdminFulfillmentBulkHandler
    : IRequestHandler<ExecuteAdminFulfillmentBulkCommand, Result<AdminFulfillmentWorkQueueBulkResult>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IAdminOrderFulfillmentOperations _operations;

    /// <summary>Handler را می‌سازد.</summary>
    public ExecuteAdminFulfillmentBulkHandler(
        IFulfillmentDirectory fulfillment,
        IAdminOrderFulfillmentOperations operations)
    {
        _fulfillment = fulfillment;
        _operations = operations;
    }

    /// <inheritdoc />
    public async Task<Result<AdminFulfillmentWorkQueueBulkResult>> Handle(
        ExecuteAdminFulfillmentBulkCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        var code = (body.ActionCode ?? string.Empty).Trim();
            if (!AdminFulfillmentQueueFilters.SafeBulkActionCodes.Contains(code))
            {
                return Ok(0, 0, FulfillmentErrorCodes.WorkQueueBulkUnsupported);
            }

            var items = body.Items ?? [];
            if (items.Count == 0)
            {
                return Ok(0, 0, FulfillmentErrorCodes.WorkQueueBulkEmpty);
            }

            var snapshots = new List<(AdminFulfillmentWorkQueueBulkItem Item, FulfillmentSnapshot Snapshot)>();
            Guid? sellerPartyId = null;
            foreach (var item in items)
            {
                var snapshot = await _fulfillment.GetAsync(item.FulfillmentId, cancellationToken);
                if (snapshot is null)
                {
                    return Ok(0, 0, FulfillmentErrorCodes.Missing);
                }

                if (sellerPartyId is null)
                {
                    sellerPartyId = snapshot.SellerPartyId;
                }
                else if (sellerPartyId != snapshot.SellerPartyId)
                {
                    return Ok(0, 0, FulfillmentErrorCodes.WorkQueueCrossSeller);
                }

                if (snapshot.SellerOrderId != item.SellerOrderId || snapshot.CheckoutId != item.CheckoutId)
                {
                    return Ok(0, 0, FulfillmentErrorCodes.WorkQueueRowMismatch);
                }

                var codes = AdminFulfillmentQueueFilters.ProjectActionCodes(snapshot);
                if (!codes.Contains(code, StringComparer.OrdinalIgnoreCase))
                {
                    return Ok(0, 0, FulfillmentErrorCodes.WorkQueueIncompatible);
                }

                if (code is "dispatch_shipment" or "deliver_shipment")
                {
                    var shipmentId = ResolveShipmentId(item, snapshot, code);
                    if (shipmentId is null)
                    {
                        return Ok(0, 0, FulfillmentErrorCodes.WorkQueueShipmentMissing);
                    }
                }

                snapshots.Add((item, snapshot));
            }

            var succeeded = 0;
            foreach (var (item, snapshot) in snapshots)
            {
                var shipmentId = ResolveShipmentId(item, snapshot, code);
                var outcome = await _operations.TryExecuteAsync(
                    item.CheckoutId,
                    request.ActorUserId,
                    new AdminOrderFulfillmentOperationRequest(
                        code,
                        item.SellerOrderId,
                        item.FulfillmentId,
                        shipmentId,
                        body.CarrierDisplayName,
                        body.TrackingReference,
                        body.ShippingMethodCode),
                    cancellationToken);
                if (!outcome.Succeeded)
                {
                    return Ok(
                        snapshots.Count,
                        succeeded,
                        outcome.ErrorCode ?? FulfillmentErrorCodes.WorkQueueBulkFailed);
                }

                succeeded++;
            }

            return Ok(snapshots.Count, succeeded, null);
        }

        private static Result<AdminFulfillmentWorkQueueBulkResult> Ok(int attempted, int succeeded, string? errorCode) =>
            Result.Success(new AdminFulfillmentWorkQueueBulkResult(attempted, succeeded, errorCode, null));

    private static Guid? ResolveShipmentId(
        AdminFulfillmentWorkQueueBulkItem item,
        FulfillmentSnapshot snapshot,
        string code)
    {
        if (item.ShipmentId is Guid explicitId && explicitId != Guid.Empty)
        {
            return explicitId;
        }

        if (code is not ("dispatch_shipment" or "deliver_shipment" or "assign_tracking" or "cancel_shipment"))
        {
            return null;
        }

        var active = snapshot.Shipments.Where(s => s.Status != ShipmentStatus.Cancelled).ToList();
        if (code == "dispatch_shipment")
        {
            return active.FirstOrDefault(s =>
                    s.Status == ShipmentStatus.Created
                    && !string.IsNullOrWhiteSpace(s.TrackingReference)
                    && s.DispatchedAt is null)?.ShipmentId;
        }

        if (code == "deliver_shipment")
        {
            return active.FirstOrDefault(s =>
                    s.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit
                    && s.DeliveredAt is null)?.ShipmentId;
        }

        return active.FirstOrDefault()?.ShipmentId;
    }
}
