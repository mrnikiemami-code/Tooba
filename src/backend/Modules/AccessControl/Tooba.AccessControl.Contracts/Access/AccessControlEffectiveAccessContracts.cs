using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Contracts.Access;

/// <summary>Owner scope for a directory operation (external modules).</summary>
public sealed record AccessOwnerScope(AccessOwnerScopeKind Kind, Guid? OwnerScopeId, string? TenantId = null);

/// <summary>One effective permission with its ceiling decision and scope.</summary>
public sealed record EffectivePermission(
    string PermissionId,
    bool DeniedByCeiling);

/// <summary>
/// Effective access projection for a user inside one owner scope. Cooked by AccessControl and
/// consumed by other modules for permission decisions; no Application/Domain leakage.
/// </summary>
public sealed record EffectiveAccess(
    Guid UserId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    IReadOnlyList<EffectivePermission> Permissions);

/// <summary>
/// Narrow access-control decision port. External modules resolve a user's effective access and
/// decide locally; the permission matrix and SpiceDB synchronization stay owned by AccessControl.
/// </summary>
public interface IAccessControlEffectiveAccessReader
{
    /// <summary>Effective access of a user inside the given owner scope.</summary>
    Task<EffectiveAccess> GetEffectiveAccessAsync(
        Guid userId,
        AccessOwnerScope owner,
        CancellationToken cancellationToken);
}