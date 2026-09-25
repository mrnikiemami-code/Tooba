using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks.Security;

namespace Tooba.AccessControl.Infrastructure.Adapters.Security;

/// <summary>
/// آداپتر عمومی AccessControl: مجوز مؤثر را به قرارداد خنثی پلتفرم نگاشت می‌کند.
/// هیچ سیاست ماژولی اینجا نیست.
/// </summary>
public sealed class PlatformEffectiveAccessReader(IAccessControlDirectory access) : IPlatformEffectiveAccessReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
        Guid userId,
        PlatformAccessOwnerKind ownerKind,
        Guid? ownerScopeId,
        CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(
            ownerKind == PlatformAccessOwnerKind.Seller ? AccessOwnerScopeKind.Seller : AccessOwnerScopeKind.Platform,
            ownerScopeId);
        var effective = await access.GetEffectiveAccessAsync(userId, owner, cancellationToken);
        return effective.Permissions
            .Select(p => new PlatformPermissionGrant(
                p.PermissionId,
                MapScope(p.ScopeKind),
                p.ScopeResourceId,
                p.DeniedByCeiling))
            .ToArray();
    }

    private static PlatformAccessScopeKind MapScope(AccessScopeKind kind) => kind switch
    {
        AccessScopeKind.Category => PlatformAccessScopeKind.Category,
        AccessScopeKind.Product => PlatformAccessScopeKind.Product,
        AccessScopeKind.Brand => PlatformAccessScopeKind.Brand,
        AccessScopeKind.Warehouse => PlatformAccessScopeKind.Warehouse,
        _ => PlatformAccessScopeKind.GlobalWithinOwner,
    };
}
