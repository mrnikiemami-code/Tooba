using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts;

namespace Tooba.Fulfillment.Application.Ports;

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
    /// <summary>صفحه‌بندی DB-native صف کار.</summary>
    Task<BuildingBlocks.Grid.GridPageResponse<Models.AdminFulfillmentWorkQueueRow>> QueryAsync(
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
