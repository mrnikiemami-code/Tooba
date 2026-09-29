using Tooba.BuildingBlocks.Security;
using Tooba.Order.Application.Seller.Ports;

namespace Tooba.Order.Infrastructure.Seller;

/// <summary>
/// پیاده‌سازی مالک‌شدهٔ Order برای خواندن دسترسی مؤثر <c>order.view</c> فروشنده.
/// تنها به درز خنثی پلتفرم (<see cref="IPlatformEffectiveAccessReader"/>) تکیه دارد و هیچ نوع
/// AccessControl Application/Domain را مصرف نمی‌کند.
/// </summary>
internal sealed class SellerOrderViewAccessReader(
    IPlatformEffectiveAccessReader access) : ISellerOrderViewAccessReader
{
    /// <inheritdoc />
    public async Task<SellerOrderViewAccessSnapshot> GetOrderViewAccessAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var grants = await access.GetEffectivePermissionsAsync(
            actorUserId,
            PlatformAccessOwnerKind.Seller,
            sellerPartyId,
            cancellationToken);
        var permissions = grants
            .Where(x => x.PermissionId == "order.view" && !x.DeniedByCeiling)
            .ToList();
        if (permissions.Count == 0)
        {
            return new SellerOrderViewAccessSnapshot(Denied: true, GlobalWithinOwner: false, AllowedCategoryIds: []);
        }

        if (permissions.Any(x => x.ScopeKind == PlatformAccessScopeKind.GlobalWithinOwner))
        {
            return new SellerOrderViewAccessSnapshot(Denied: false, GlobalWithinOwner: true, AllowedCategoryIds: []);
        }

        var allowed = permissions
            .Where(x => x.ScopeKind == PlatformAccessScopeKind.Category && x.ScopeResourceId != null)
            .Select(x => x.ScopeResourceId!.Value)
            .ToHashSet();
        return new SellerOrderViewAccessSnapshot(
            Denied: allowed.Count == 0,
            GlobalWithinOwner: false,
            AllowedCategoryIds: allowed);
    }
}
