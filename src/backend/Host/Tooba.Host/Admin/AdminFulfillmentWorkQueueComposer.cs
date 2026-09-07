using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Grid;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب صف کار ارسال و تحویل — همان فرمان‌های Admin Order Operations، بدون lifecycle دوم.
/// </summary>
public sealed class AdminFulfillmentWorkQueueComposer
{
    private readonly AdminFulfillmentWorkQueueQueryEngine _grid;
    private readonly AdminOrderOperationsComposer _operations;
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>صف کار را به موتور گرید و عملیات سفارش وصل می‌کند.</summary>
    public AdminFulfillmentWorkQueueComposer(
        AdminFulfillmentWorkQueueQueryEngine grid,
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
            throw new PlatformHttpException(
                400,
                "این عملیات گروهی پشتیبانی نمی‌شود.",
                "fulfillment.work_queue.bulk_unsupported");
        }

        var items = request.Items ?? [];
        if (items.Count == 0)
        {
            throw new PlatformHttpException(
                400,
                "هیچ ردیفی برای عملیات گروهی انتخاب نشده است.",
                "fulfillment.work_queue.bulk_empty");
        }

        var snapshots = new List<(AdminFulfillmentWorkQueueBulkItem Item, FulfillmentSnapshot Snapshot)>();
        Guid? sellerPartyId = null;
        foreach (var item in items)
        {
            var snapshot = await _fulfillment.GetAsync(item.FulfillmentId, cancellationToken)
                ?? throw new PlatformHttpException(404, "ارسال یافت نشد.", "fulfillment.missing");
            if (sellerPartyId is null)
            {
                sellerPartyId = snapshot.SellerPartyId;
            }
            else if (sellerPartyId != snapshot.SellerPartyId)
            {
                throw new PlatformHttpException(
                    400,
                    "انتخاب چندفروشنده برای عملیات گروهی مجاز نیست.",
                    "fulfillment.work_queue.cross_seller");
            }

            if (snapshot.SellerOrderId != item.SellerOrderId || snapshot.CheckoutId != item.CheckoutId)
            {
                throw new PlatformHttpException(
                    400,
                    "ردیف انتخاب‌شده با دادهٔ سرور هم‌خوان نیست.",
                    "fulfillment.work_queue.row_mismatch");
            }

            var codes = AdminFulfillmentQueueFilters.ProjectActionCodes(snapshot);
            if (!codes.Contains(code, StringComparer.OrdinalIgnoreCase))
            {
                throw new PlatformHttpException(
                    400,
                    "انتخاب ناسازگار است؛ همهٔ ردیف‌ها باید همان عملیات مجاز را داشته باشند.",
                    "fulfillment.work_queue.incompatible");
            }

            if (code is "dispatch_shipment" or "deliver_shipment")
            {
                var shipmentId = ResolveShipmentId(item, snapshot, code);
                if (shipmentId is null)
                {
                    throw new PlatformHttpException(
                        400,
                        "مرسولهٔ معتبر برای این عملیات یافت نشد.",
                        "fulfillment.work_queue.shipment_missing");
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
                    ex.ErrorCode ?? "order.operation.failed",
                    ex.Title);
            }
            catch (InvalidOperationException ex)
            {
                return new AdminFulfillmentWorkQueueBulkResult(
                    snapshots.Count,
                    succeeded,
                    "order.operation.failed",
                    ex.Message);
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

