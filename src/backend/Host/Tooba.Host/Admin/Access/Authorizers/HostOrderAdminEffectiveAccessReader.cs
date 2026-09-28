using Tooba.BuildingBlocks.Security;
using Tooba.Order.Contracts.Admin.Operations;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>
/// Thin Host adapter: maps the neutral platform effective-access seam
/// (<see cref="IPlatformEffectiveAccessReader"/>, AccessControl-owned implementation) onto the
/// Order admin effective-access contract. No AccessControl Application/Domain types are referenced.
/// </summary>
internal sealed class HostOrderAdminEffectiveAccessReader(
    IPlatformEffectiveAccessReader access) : IOrderAdminEffectiveAccessReader
{
    public async Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var grants = await access.GetEffectivePermissionsAsync(
            actorUserId,
            PlatformAccessOwnerKind.Platform,
            null,
            cancellationToken);
        return new OrderAdminEffectiveAccess(
            grants
                .Select(p => new OrderAdminPermissionGrant(p.PermissionId, p.DeniedByCeiling))
                .ToList());
    }
}
