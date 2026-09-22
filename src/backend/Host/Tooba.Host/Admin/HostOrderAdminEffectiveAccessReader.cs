using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Order.Application.Admin.Operations.Ports;

namespace Tooba.Host.Admin;

/// <summary>Thin Host adapter: AccessControl effective grants for Order admin operations.</summary>
internal sealed class HostOrderAdminEffectiveAccessReader(
    IAccessControlDirectory access,
    ICurrentTenant tenant) : IOrderAdminEffectiveAccessReader
{
    public async Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var scope = new AccessOwnerScope(
            AccessOwnerScopeKind.Platform,
            null,
            tenant.Current?.TenantId.Value);
        var effective = await access.GetEffectiveAccessAsync(actorUserId, scope, cancellationToken);
        return new OrderAdminEffectiveAccess(
            effective.Permissions
                .Select(p => new OrderAdminPermissionGrant(p.PermissionId, p.DeniedByCeiling))
                .ToList());
    }
}
