using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.Order.Infrastructure.Admin.Fulfillment;

/// <summary>Permission evaluation for fulfillment admin ops (legacy-admin compatible).</summary>
public interface IAdminOrderFulfillmentPermissionGate
{
    /// <summary>True when actor may run fulfillment manage ops.</summary>
    Task<bool> CanManageFulfillmentAsync(Guid actorUserId, CancellationToken cancellationToken);
}

/// <summary>AccessControl-backed permission gate.</summary>
public sealed class AdminOrderFulfillmentPermissionGate : IAdminOrderFulfillmentPermissionGate
{
    private static readonly string[] OpsFamilyPrefixes =
    [
        "order.",
        "return.",
        "fulfillment.",
        "refund.",
        "payment.",
    ];

    private readonly IAccessControlDirectory _access;
    private readonly ICurrentTenant _tenant;

    /// <summary>Gate را می‌سازد.</summary>
    public AdminOrderFulfillmentPermissionGate(IAccessControlDirectory access, ICurrentTenant tenant)
    {
        _access = access;
        _tenant = tenant;
    }

    /// <inheritdoc />
    public async Task<bool> CanManageFulfillmentAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.Current?.TenantId.Value;
        var scope = new AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenantId);
        var effective = await _access.GetEffectiveAccessAsync(actorUserId, scope, cancellationToken);
        return HasAny(effective, "order.handle", "fulfillment.manage");
    }

    private static bool Has(EffectiveAccessDto effective, string permissionId)
    {
        var grants = effective.Permissions.Where(p => !p.DeniedByCeiling).ToList();
        var hasOpsFamily = grants.Any(p => OpsFamilyPrefixes.Any(prefix =>
            p.PermissionId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        if (!hasOpsFamily)
        {
            return true;
        }

        return grants.Any(p => string.Equals(p.PermissionId, permissionId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAny(EffectiveAccessDto effective, params string[] permissionIds) =>
        permissionIds.Any(p => Has(effective, p));
}
