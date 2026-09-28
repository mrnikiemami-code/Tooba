namespace Tooba.AccessControl.Contracts;

/// <summary>
/// Scope kind of a platform/seller owner as seen by external modules. Mirrors the AccessControl
/// domain vocabulary without leaking Application/Domain types.
/// </summary>
public enum AccessOwnerScopeKind
{
    /// <summary>Platform / Admin roles.</summary>
    Platform = 1,

    /// <summary>A single seller's roles.</summary>
    Seller = 2,
}

/// <summary>Owner scope for a directory operation.</summary>
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
