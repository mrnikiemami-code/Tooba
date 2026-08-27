using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;

namespace Tooba.Host.Tests;

/// <summary>
/// دایرکتوری Access Control ساختگی برای تست‌های غیر-ACC.
/// </summary>
internal class FakeAccessControlDirectory : IAccessControlDirectory
{
    /// <inheritdoc />
    public IReadOnlyList<PermissionDefinition> ListCatalog() => PermissionCatalog.All;

    /// <inheritdoc />
    public Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(AccessOwnerScope owner, bool includeArchived, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AccessRoleDto>>([]);

    /// <inheritdoc />
    public Task<AccessRoleDto?> GetRoleAsync(Guid roleId, AccessOwnerScope owner, CancellationToken cancellationToken) =>
        Task.FromResult<AccessRoleDto?>(null);

    /// <inheritdoc />
    public Task<AccessRoleDto> CreateRoleAsync(AccessOwnerScope owner, CreateAccessRoleCommand command, Guid actorUserId, string? traceId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<AccessRoleDto> UpdateRoleAsync(Guid roleId, AccessOwnerScope owner, UpdateAccessRoleCommand command, Guid actorUserId, string? traceId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<AccessRoleDto> CloneRoleAsync(Guid roleId, AccessOwnerScope owner, CloneAccessRoleCommand command, Guid actorUserId, string? traceId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task ArchiveRoleAsync(Guid roleId, AccessOwnerScope owner, Guid actorUserId, string? traceId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task SetRolePermissionsAsync(Guid roleId, AccessOwnerScope owner, IReadOnlyList<RolePermissionGrant> grants, Guid actorUserId, string? traceId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IReadOnlyList<RolePermissionGrant>> GetRolePermissionsAsync(Guid roleId, AccessOwnerScope owner, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RolePermissionGrant>>([]);

    /// <inheritdoc />
    public Task<IReadOnlyList<UserRoleAssignmentDto>> ListAssignmentsAsync(AccessOwnerScope owner, Guid? userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserRoleAssignmentDto>>([]);

    /// <inheritdoc />
    public Task<UserRoleAssignmentDto> AssignRoleAsync(AccessOwnerScope owner, Guid userId, Guid roleId, Guid actorUserId, string? traceId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task RemoveAssignmentAsync(Guid assignmentId, AccessOwnerScope owner, Guid actorUserId, string? traceId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<IReadOnlyList<SellerCeilingEntryDto>> GetSellerCeilingAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SellerCeilingEntryDto>>([]);

    /// <inheritdoc />
    public Task SetSellerCeilingAsync(
        Guid sellerPartyId,
        IReadOnlyList<(string PermissionId, bool Enabled, AccessScopeKind ScopeKind, Guid? ScopeResourceId)> entries,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public virtual Task<EffectiveAccessDto> GetEffectiveAccessAsync(Guid userId, AccessOwnerScope owner, CancellationToken cancellationToken) =>
        Task.FromResult(new EffectiveAccessDto(
            userId,
            owner.Kind,
            owner.OwnerScopeId,
            PermissionCatalog.All
                .Where(p => p.Delegable)
                .Select(p => new EffectivePermissionDto(
                    p.PermissionId,
                    p.Module,
                    AccessScopeKind.GlobalWithinOwner,
                    null,
                    ["seller-owner"],
                    false))
                .ToList(),
            ["seller-owner"]));

    /// <inheritdoc />
    public Task<IReadOnlyList<AccessUserHitDto>> SearchUsersInScopeAsync(AccessOwnerScope owner, string? query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AccessUserHitDto>>([]);

    /// <inheritdoc />
    public Task EnsureBootstrapAsync(Guid? platformAdminUserId, IReadOnlyList<Guid> sellerPartyIds, string? tenantId, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task SyncUserCapabilityTuplesAsync(Guid userId, AccessOwnerScope owner, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
