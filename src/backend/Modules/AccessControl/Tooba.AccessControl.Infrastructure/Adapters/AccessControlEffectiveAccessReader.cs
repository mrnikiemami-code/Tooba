using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Contracts.Access;

namespace Tooba.AccessControl.Infrastructure.Adapters;

/// <summary>
/// AccessControl-owned adapter exposing the narrow <see cref="IAccessControlEffectiveAccessReader"/>
/// contract. Application/Domain vocabulary is mapped here so foreign modules never reference
/// AccessControl Application/Domain types.
/// </summary>
internal sealed class AccessControlEffectiveAccessReader(IAccessControlDirectory directory)
    : IAccessControlEffectiveAccessReader
{
    /// <inheritdoc />
    public async Task<EffectiveAccess> GetEffectiveAccessAsync(
        Guid userId,
        Tooba.AccessControl.Contracts.Access.AccessOwnerScope owner,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var source = new Tooba.AccessControl.Application.Models.AccessOwnerScope(
            (Tooba.AccessControl.Domain.AccessOwnerScopeKind)owner.Kind,
            owner.OwnerScopeId,
            owner.TenantId);

        var effective = await directory.GetEffectiveAccessAsync(userId, source, cancellationToken);
        return new EffectiveAccess(
            effective.UserId,
            (Tooba.AccessControl.Contracts.Access.AccessOwnerScopeKind)effective.OwnerScopeKind,
            effective.OwnerScopeId,
            effective.Permissions
                .Select(x => new EffectivePermission(x.PermissionId, x.DeniedByCeiling))
                .ToArray());
    }
}
