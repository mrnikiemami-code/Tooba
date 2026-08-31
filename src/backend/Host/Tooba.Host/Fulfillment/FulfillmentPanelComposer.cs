using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application;
using Tooba.Host.Grid;

namespace Tooba.Host.Fulfillment;

/// <summary>
/// خط محموله در درخواست HTTP.
/// </summary>
public sealed record FulfillmentShipmentLineRequest(Guid OrderLineId, int Quantity);

/// <summary>
/// درخواست ایجاد محموله.
/// </summary>
public sealed record FulfillmentCreateShipmentRequest(
    string CarrierDisplayName,
    IReadOnlyList<FulfillmentShipmentLineRequest> Items);

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

    /// <summary>
    /// سازندهٔ ترکیب fulfillment.
    /// </summary>
    public FulfillmentPanelComposer(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

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

    /// <summary>صفحه‌بندی server-side گرید fulfillment Admin.</summary>
    public async Task<GridPageResponse<FulfillmentSnapshot>> QueryGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var rows = await ListAllAsync(cancellationToken);
        return AdminListGridPolicies.Fulfillments.Execute(rows, request);
    }

    /// <summary>
    /// fulfillmentهای یک checkout.
    /// </summary>
    public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);

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
            cancellationToken);

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
