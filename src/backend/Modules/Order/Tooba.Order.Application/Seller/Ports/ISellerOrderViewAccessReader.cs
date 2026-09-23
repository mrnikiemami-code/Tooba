namespace Tooba.Order.Application.Seller.Ports;

/// <summary>
/// Snapshot دسترسی order.view برای فروشنده — بدون افشای AccessControl Application/Domain به handlers.
/// </summary>
public sealed record SellerOrderViewAccessSnapshot(
    bool Denied,
    bool GlobalWithinOwner,
    IReadOnlyCollection<Guid> AllowedCategoryIds);

/// <summary>خواندن دسترسی مؤثر order.view در محدودهٔ Seller.</summary>
public interface ISellerOrderViewAccessReader
{
    /// <summary>Snapshot دسترسی order.view برای Actor در مالکیت فروشنده.</summary>
    Task<SellerOrderViewAccessSnapshot> GetOrderViewAccessAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        CancellationToken cancellationToken);
}
