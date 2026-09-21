using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Host.Grid;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب صف کار ارسال و تحویل — همان فرمان‌های Admin Order Operations، بدون lifecycle دوم.
/// خطاهای مورد انتظار به ErrorCode معنایی برمی‌گردند (بدون PlatformHttpException).
/// </summary>
public sealed class AdminFulfillmentWorkQueueComposer
{
    private readonly IAdminFulfillmentWorkQueueQuery _grid;
    private readonly AdminOrderOperationsComposer _operations;
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>صف کار را به موتور گرید و عملیات سفارش وصل می‌کند.</summary>
    public AdminFulfillmentWorkQueueComposer(
        IAdminFulfillmentWorkQueueQuery grid,
        AdminOrderOperationsComposer operations,
        IFulfillmentDirectory fulfillment)
    {
        _grid = grid;
        _operations = operations;
        _fulfillment = fulfillment;
    }

    /// <summary>صفحه‌بندی server-side صف کار.</summary>
    public Task<GridPageResponse<AdminFulfillmentWorkQueueRow>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken) =>
        _grid.QueryAsync(AdminListGridPolicies.Fulfillments.Normalize(request), cancellationToken);

    /// <summary>
    /// اجرای گروهی — همه باید سازگار باشند؛ در صورت خطا پس از شروع، تعداد موفق گزارش می‌شود (بدون موفقیت خاموش).
    /// </summary>
    public async Task<AdminFulfillmentWorkQueueBulkResult> ExecuteBulkAsync(
        Guid actorUserId,
        AdminFulfillmentWorkQueueBulkRequest request,
        CancellationToken cancellationToken)
    {
        var code = (request.ActionCode ?? string.Empty).Trim();
        if (!AdminFulfillmentQueueFilters.SafeBulkActionCodes.Contains(code))
        {
            return new AdminFulfillmentWorkQueueBulkResult(
                0, 0, FulfillmentErrorCodes.WorkQueueBulkUnsupported, null);
        }

        var items = request.Items ?? [];
        if (items.Count == 0)
        {
            return new AdminFulfillmentWorkQueueBulkResult(
                0, 0, FulfillmentErrorCodes.WorkQueueBulkEmpty, null);
        }

        var snapshots = new List<(AdminFulfillmentWorkQueueBulkItem Item, FulfillmentSnapshot Snapshot)>();
        Guid? sellerPartyId = null;
        foreach (var item in items)
        {
            var snapshot = await _fulfillment.GetAsync(item.FulfillmentId, cancellationToken);
            if (snapshot is null)
            {
                return new AdminFulfillmentWorkQueueBulkResult(
                    0, 0, FulfillmentErrorCodes.Missing, null);
            }

            if (sellerPartyId is null)
            {
                sellerPartyId = snapshot.SellerPartyId;
            }
            else if (sellerPartyId != snapshot.SellerPartyId)
            {
                return new AdminFulfillmentWorkQueueBulkResult(
                    0, 0, FulfillmentErrorCodes.WorkQueueCrossSeller, null);
            }

            if (snapshot.SellerOrderId != item.SellerOrderId || snapshot.CheckoutId != item.CheckoutId)
            {
                return new AdminFulfillmentWorkQueueBulkResult(
                    0, 0, FulfillmentErrorCodes.WorkQueueRowMismatch, null);
            }

            var codes = AdminFulfillmentQueueFilters.ProjectActionCodes(snapshot);
            if (!codes.Contains(code, StringComparer.OrdinalIgnoreCase))
            {
                return new AdminFulfillmentWorkQueueBulkResult(
                    0, 0, FulfillmentErrorCodes.WorkQueueIncompatible, null);
            }

            if (code is "dispatch_shipment" or "deliver_shipment")
            {
                var shipmentId = ResolveShipmentId(item, snapshot, code);
                if (shipmentId is null)
                {
                    return new AdminFulfillmentWorkQueueBulkResult(
                        0, 0, FulfillmentErrorCodes.WorkQueueShipmentMissing, null);
                }
            }

            snapshots.Add((item, snapshot));
        }

        var succeeded = 0;
        foreach (var (item, snapshot) in snapshots)
        {
            try
            {
                var shipmentId = ResolveShipmentId(item, snapshot, code);
                await _operations.ExecuteAsync(
                    item.CheckoutId,
                    actorUserId,
                    new AdminOrderOperationRequest(
                        code,
                        item.SellerOrderId,
                        item.FulfillmentId,
                        shipmentId,
                        null,
                        request.CarrierDisplayName,
                        request.TrackingReference,
                        null,
                        null,
                        null,
                        null,
                        request.ShippingMethodCode,
                        null),
                    cancellationToken);
                succeeded++;
            }
            catch (PlatformHttpException ex)
            {
                return new AdminFulfillmentWorkQueueBulkResult(
                    snapshots.Count,
                    succeeded,
                    ex.ErrorCode ?? FulfillmentErrorCodes.WorkQueueBulkFailed,
                    null);
            }
            catch (InvalidOperationException)
            {
                return new AdminFulfillmentWorkQueueBulkResult(
                    snapshots.Count,
                    succeeded,
                    FulfillmentErrorCodes.WorkQueueBulkFailed,
                    null);
            }
        }

        return new AdminFulfillmentWorkQueueBulkResult(snapshots.Count, succeeded, null, null);
    }

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
