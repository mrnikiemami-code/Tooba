using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.Order.Application.Seller.Ports;

namespace Tooba.Host.Seller;

/// <summary>Thin Host adapter: AccessControl order.view snapshot for Seller Order CQRS.</summary>
public sealed class HostSellerOrderViewAccessReader(IAccessControlDirectory access) : ISellerOrderViewAccessReader
{
    /// <inheritdoc />
    public async Task<SellerOrderViewAccessSnapshot> GetOrderViewAccessAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var effective = await access.GetEffectiveAccessAsync(
            actorUserId,
            new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId),
            cancellationToken);
        var permissions = effective.Permissions
            .Where(x => x.PermissionId == "order.view" && !x.DeniedByCeiling)
            .ToList();
        if (permissions.Count == 0)
        {
            return new SellerOrderViewAccessSnapshot(Denied: true, GlobalWithinOwner: false, AllowedCategoryIds: []);
        }

        if (permissions.Any(x => x.ScopeKind == AccessScopeKind.GlobalWithinOwner))
        {
            return new SellerOrderViewAccessSnapshot(Denied: false, GlobalWithinOwner: true, AllowedCategoryIds: []);
        }

        var allowed = permissions
            .Where(x => x.ScopeKind == AccessScopeKind.Category && x.ScopeResourceId != null)
            .Select(x => x.ScopeResourceId!.Value)
            .ToHashSet();
        return new SellerOrderViewAccessSnapshot(
            Denied: allowed.Count == 0,
            GlobalWithinOwner: false,
            AllowedCategoryIds: allowed);
    }
}
