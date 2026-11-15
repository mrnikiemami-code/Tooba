using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts;

namespace Tooba.Fulfillment.Application.Fulfillments.Ports;

/// <summary>Snapshot مجوز order.handle که Host از AccessControl می‌سازد (بدون وابستگی AccessControl.Application).</summary>
public sealed record SellerOrderHandlePermissionSnapshot(
    bool HasGlobalWithinOwner,
    IReadOnlyCollection<Guid> AllowedCategoryIds);

/// <summary>احراز مجوز جهش fulfillment فروشنده با semantics دقیق فعلی.</summary>
public interface ISellerFulfillmentAuthorizer
{
    /// <summary>
    /// order.handle را اعمال می‌کند: GlobalWithinOwner قبول؛ category-scoped نیازمند همه خطوط؛ missing→404؛ denied→403.
    /// </summary>
    Task<Result> EnsureCanMutateAsync(
        Guid sellerPartyId,
        Guid sellerOrderId,
        SellerOrderHandlePermissionSnapshot permission,
        CancellationToken cancellationToken);
}

/// <summary>Query port صف کار Admin fulfillment.</summary>
public interface IAdminFulfillmentWorkQueueQuery
{
    /// <summary>
    /// TB-271-R1: the ONE read the live queue endpoint uses — a single classification under ONE injected clock
    /// returning the page AND its counts together, so the rows and the badges can never come from two reads.
    /// </summary>
    Task<WorkQueue.Models.AdminFulfillmentWorkQueueSnapshot> QueryWithCountsAsync(
        BuildingBlocks.Grid.GridQueryRequest request,
        CancellationToken cancellationToken);

    /// <summary>صفحه‌بندی DB-native صف کار (same single-classification path as the combined read).</summary>
    Task<BuildingBlocks.Grid.GridPageResponse<WorkQueue.Models.AdminFulfillmentWorkQueueRow>> QueryAsync(
        BuildingBlocks.Grid.GridQueryRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-260: counts of the SAME filtered set (dispatch timing buckets) so tab counts never describe a
    /// single fetched page.
    /// </summary>
    Task<WorkQueue.Models.AdminFulfillmentWorkQueueCounts> CountAsync(
        BuildingBlocks.Grid.GridQueryRequest request,
        CancellationToken cancellationToken);
}

/// <summary>خواندن shipped quantities برای OrderSupply بدون FulfillmentDbContext در Host.</summary>
public interface IFulfillmentShippedQuantityReader
{
    /// <summary>مجموع QuantityShipped per OrderLineId برای checkoutهای داده‌شده.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken);
}
