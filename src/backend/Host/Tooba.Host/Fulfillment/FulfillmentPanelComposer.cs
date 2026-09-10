using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Host.Fulfillment;

/// <summary>
/// خط محموله در درخواست HTTP.
/// </summary>
public sealed record FulfillmentShipmentLineRequest(Guid OrderLineId, decimal Quantity);

/// <summary>
/// درخواست ایجاد محموله.
/// </summary>
public sealed record FulfillmentCreateShipmentRequest(
    string CarrierDisplayName,
    IReadOnlyList<FulfillmentShipmentLineRequest> Items,
    string? ShippingMethodCode = null,
    string? ProviderMetadataJson = null);

/// <summary>
/// درخواست ثبت tracking.
/// </summary>
public sealed record FulfillmentAssignTrackingRequest(string TrackingReference);

/// <summary>
/// ترکیب HTTP fulfillment برای seller/admin/customer.
/// </summary>
public sealed class FulfillmentPanelComposer
{
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly AdminFulfillmentWorkQueueQueryEngine _grid;

    /// <summary>
    /// سازندهٔ ترکیب fulfillment.
    /// </summary>
    public FulfillmentPanelComposer(
        IFulfillmentDirectory fulfillment,
        FulfillmentDbContext db,
        PartyDbContext parties,
        OrderDbContext orders)
    {
        _fulfillment = fulfillment;
        _grid = new AdminFulfillmentWorkQueueQueryEngine(db, parties, orders);
    }

    /// <summary>
    /// fulfillment را می‌خواند.
    /// </summary>
    public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) =>
        _fulfillment.GetAsync(fulfillmentId, cancellationToken);

    /// <summary>
    /// فهرست fulfillment یک فروشنده.
    /// </summary>
    public Task<IReadOnlyList<FulfillmentSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
        _fulfillment.ListForSellerAsync(sellerPartyId, cancellationToken);

    /// <summary>
    /// fulfillment را برای همان فروشنده می‌خواند؛ در صورت عدم تطابق null برمی‌گرداند.
    /// </summary>
    public async Task<FulfillmentSnapshot?> GetForSellerAsync(Guid sellerPartyId, Guid fulfillmentId, CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken);
        return snapshot is null || snapshot.SellerPartyId != sellerPartyId ? null : snapshot;
    }

    /// <summary>
    /// فهرست همه fulfillmentها برای admin.
    /// </summary>
    public Task<IReadOnlyList<FulfillmentSnapshot>> ListAllAsync(CancellationToken cancellationToken) =>
        _fulfillment.ListAllAsync(cancellationToken);

    /// <summary>صفحه‌بندی server-side صف کار ارسال و تحویل Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminFulfillmentWorkQueueRow>> QueryGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Fulfillments.Normalize(request);
        return _grid.QueryAsync(q, cancellationToken);
    }

    /// <summary>
    /// fulfillmentهای یک checkout؛ رهگیری بسته تجمیعی فعال را به‌عنوان PreferredTrackingReference می‌گذارد.
    /// </summary>
    public async Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var list = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var packages = await _fulfillment.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
        var preferred = packages
            .Where(p => p.Status is Tooba.Fulfillment.Domain.ConsolidatedPackageStatus.Created
                or Tooba.Fulfillment.Domain.ConsolidatedPackageStatus.Dispatched
                or Tooba.Fulfillment.Domain.ConsolidatedPackageStatus.Delivered)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.TrackingReference)
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        if (string.IsNullOrWhiteSpace(preferred))
        {
            return list;
        }

        return list.Select(s => s with { PreferredTrackingReference = preferred }).ToList();
    }

    /// <summary>
    /// به Processing می‌رود.
    /// </summary>
    public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) =>
        _fulfillment.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken);

    /// <summary>
    /// به Packed می‌رود.
    /// </summary>
    public Task<FulfillmentSnapshot> MarkPackedAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) =>
        _fulfillment.MarkPackedAsync(fulfillmentId, actorUserId, cancellationToken);

    /// <summary>
    /// محموله می‌سازد.
    /// </summary>
    public Task<FulfillmentSnapshot> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        FulfillmentCreateShipmentRequest request,
        CancellationToken cancellationToken) =>
        _fulfillment.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            request.CarrierDisplayName,
            request.Items.Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToArray(),
            cancellationToken,
            request.ShippingMethodCode,
            request.ProviderMetadataJson);

    /// <summary>
    /// tracking idempotent ثبت می‌کند.
    /// </summary>
    public Task<FulfillmentSnapshot> AssignTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken) =>
        _fulfillment.AssignTrackingAsync(fulfillmentId, shipmentId, actorUserId, trackingReference, cancellationToken);

    /// <summary>
    /// محموله را dispatch می‌کند.
    /// </summary>
    public Task<FulfillmentSnapshot> DispatchShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        _fulfillment.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);

    /// <summary>
    /// محموله را delivered علامت می‌زند.
    /// </summary>
    public Task<FulfillmentSnapshot> DeliverShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        _fulfillment.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
}
