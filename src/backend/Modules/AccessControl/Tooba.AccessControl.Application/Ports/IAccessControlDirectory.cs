using Tooba.AccessControl.Application.Access.Models;
using Tooba.AccessControl.Application.Assignments.Models;
using Tooba.AccessControl.Application.Ceiling.Models;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions.Models;
using Tooba.AccessControl.Application.Roles.Models;
using Tooba.AccessControl.Application.Permissions;
using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Ports;

/// <summary>دایرکتوری Access Control (پیکربندی PG + همگام‌سازی SpiceDB).</summary>
public interface IAccessControlDirectory
{
    /// <summary>فهرست نقش‌ها.</summary>
    Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(AccessOwnerScope owner, bool includeArchived, CancellationToken cancellationToken);

    /// <summary>خواندن نقش.</summary>
    Task<AccessRoleDto?> GetRoleAsync(Guid roleId, AccessOwnerScope owner, CancellationToken cancellationToken);

    /// <summary>ایجاد نقش.</summary>
    Task<AccessRoleDto> CreateRoleAsync(AccessOwnerScope owner, CreateRoleRequest command, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>به‌روزرسانی نقش.</summary>
    Task<AccessRoleDto> UpdateRoleAsync(Guid roleId, AccessOwnerScope owner, UpdateRoleRequest command, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>کلون نقش.</summary>
    Task<AccessRoleDto> CloneRoleAsync(Guid roleId, AccessOwnerScope owner, CloneRoleRequest command, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>بایگانی نقش.</summary>
    Task ArchiveRoleAsync(Guid roleId, AccessOwnerScope owner, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>تنظیم مجوزهای نقش.</summary>
    Task SetRolePermissionsAsync(Guid roleId, AccessOwnerScope owner, IReadOnlyList<RolePermissionGrant> grants, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>خواندن مجوزهای نقش.</summary>
    Task<IReadOnlyList<RolePermissionGrant>> GetRolePermissionsAsync(Guid roleId, AccessOwnerScope owner, CancellationToken cancellationToken);

    /// <summary>فهرست تخصیص‌ها.</summary>
    Task<IReadOnlyList<UserRoleAssignmentDto>> ListAssignmentsAsync(AccessOwnerScope owner, Guid? userId, CancellationToken cancellationToken);

    /// <summary>تخصیص نقش.</summary>
    Task<UserRoleAssignmentDto> AssignRoleAsync(AccessOwnerScope owner, Guid userId, Guid roleId, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>حذف تخصیص.</summary>
    Task RemoveAssignmentAsync(Guid assignmentId, AccessOwnerScope owner, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>خواندن سقف فروشنده.</summary>
    Task<IReadOnlyList<SellerCeilingEntryDto>> GetSellerCeilingAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>تنظیم سقف فروشنده با scope اختیاری.</summary>
    Task SetSellerCeilingAsync(
        Guid sellerPartyId,
        IReadOnlyList<(string PermissionId, bool Enabled, AccessScopeKind ScopeKind, Guid? ScopeResourceId)> entries,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken);

    /// <summary>دسترسی مؤثر.</summary>
    Task<EffectiveAccessDto> GetEffectiveAccessAsync(Guid userId, AccessOwnerScope owner, CancellationToken cancellationToken);

    /// <summary>کاتالوگ.</summary>
    IReadOnlyList<PermissionDefinition> ListCatalog();

    /// <summary>جستجوی کاربران محدوده.</summary>
    Task<IReadOnlyList<AccessUserHitDto>> SearchUsersInScopeAsync(AccessOwnerScope owner, string? query, CancellationToken cancellationToken);

    /// <summary>seed نقش‌های سیستمی.</summary>
    Task EnsureBootstrapAsync(Guid? platformAdminUserId, IReadOnlyList<Guid> sellerPartyIds, string? tenantId, CancellationToken cancellationToken);

    /// <summary>همگام‌سازی tupleهای SpiceDB برای کاربر.</summary>
    Task SyncUserCapabilityTuplesAsync(Guid userId, AccessOwnerScope owner, CancellationToken cancellationToken);
}