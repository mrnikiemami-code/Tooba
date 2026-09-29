using Tooba.BuildingBlocks.Security;
using Tooba.Order.Application.Seller.Ports;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Thin Host adapter: neutral platform effective-access snapshot for Seller Order CQRS.
/// No AccessControl Application/Domain type is referenced.
/// </summary>
public sealed class HostSellerOrderViewAccessReader(IPlatformEffectiveAccessReader access) : ISellerOrderViewAccessReader
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
