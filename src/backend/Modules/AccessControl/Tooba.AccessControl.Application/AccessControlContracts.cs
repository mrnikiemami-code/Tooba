using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application;

/// <summary>زمینهٔ مالک برای عملیات دایرکتوری.</summary>
/// <param name="Kind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک.</param>
/// <param name="TenantId">Tenant اختیاری.</param>
public sealed record AccessOwnerScope(AccessOwnerScopeKind Kind, Guid? OwnerScopeId, string? TenantId = null);

/// <summary>فرمان ایجاد نقش.</summary>
/// <param name="Name">نام.</param>
/// <param name="Code">کد.</param>
/// <param name="Description">توضیح.</param>
public sealed record CreateAccessRoleCommand(string Name, string Code, string Description);

/// <summary>فرمان به‌روزرسانی نقش.</summary>
/// <param name="Name">نام.</param>
/// <param name="Description">توضیح.</param>
public sealed record UpdateAccessRoleCommand(string Name, string Description);

/// <summary>فرمان کلون نقش.</summary>
/// <param name="Name">نام.</param>
/// <param name="Code">کد.</param>
/// <param name="Description">توضیح.</param>
public sealed record CloneAccessRoleCommand(string Name, string Code, string? Description);

/// <summary>یک اعطای مجوز روی نقش.</summary>
/// <param name="PermissionId">شناسهٔ مجوز.</param>
/// <param name="ScopeKind">گونهٔ scope.</param>
/// <param name="ScopeResourceId">منبع.</param>
/// <param name="Enabled">فعال.</param>
public sealed record RolePermissionGrant(
    string PermissionId,
    AccessScopeKind ScopeKind,
    Guid? ScopeResourceId,
    bool Enabled);

/// <summary>DTO نقش با شمارش‌ها.</summary>
/// <param name="Id">شناسه.</param>
/// <param name="OwnerScopeKind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک.</param>
/// <param name="Name">نام.</param>
/// <param name="Code">کد.</param>
/// <param name="Description">توضیح.</param>
/// <param name="IsSystem">سیستمی.</param>
/// <param name="IsMutable">قابل ویرایش.</param>
/// <param name="IsArchived">بایگانی.</param>
/// <param name="PermissionCount">تعداد مجوز.</param>
/// <param name="AssignmentCount">تعداد تخصیص.</param>
/// <param name="CreatedAt">ایجاد.</param>
/// <param name="UpdatedAt">به‌روزرسانی.</param>
public sealed record AccessRoleDto(
    Guid Id,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    string Name,
    string Code,
    string Description,
    bool IsSystem,
    bool IsMutable,
    bool IsArchived,
    int PermissionCount,
    int AssignmentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>DTO تخصیص.</summary>
/// <param name="Id">شناسه.</param>
/// <param name="UserId">کاربر.</param>
/// <param name="RoleId">نقش.</param>
/// <param name="RoleName">نام نقش.</param>
/// <param name="RoleCode">کد نقش.</param>
/// <param name="OwnerScopeKind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک.</param>
/// <param name="AssignedAt">زمان.</param>
public sealed record UserRoleAssignmentDto(
    Guid Id,
    Guid UserId,
    Guid RoleId,
    string RoleName,
    string RoleCode,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    DateTimeOffset AssignedAt);

/// <summary>DTO سقف فروشنده.</summary>
/// <param name="PermissionId">مجوز.</param>
/// <param name="Enabled">فعال.</param>
/// <param name="Delegable">قابل تفویض.</param>
/// <param name="Module">ماژول.</param>
public sealed record SellerCeilingEntryDto(string PermissionId, bool Enabled, bool Delegable, string Module);

/// <summary>یک مجوز مؤثر با محدوده.</summary>
/// <param name="PermissionId">مجوز.</param>
/// <param name="Module">ماژول.</param>
/// <param name="ScopeKind">scope.</param>
/// <param name="ScopeResourceId">منبع.</param>
/// <param name="InheritedViaRoleCodes">نقش‌ها.</param>
/// <param name="DeniedByCeiling">ردشده توسط سقف.</param>
public sealed record EffectivePermissionDto(
    string PermissionId,
    string Module,
    AccessScopeKind ScopeKind,
    Guid? ScopeResourceId,
    IReadOnlyList<string> InheritedViaRoleCodes,
    bool DeniedByCeiling);

/// <summary>پیش‌نمایش دسترسی مؤثر.</summary>
/// <param name="UserId">کاربر.</param>
/// <param name="OwnerScopeKind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک.</param>
/// <param name="Permissions">مجوزها.</param>
/// <param name="RoleCodes">کد نقش‌ها.</param>
public sealed record EffectiveAccessDto(
    Guid UserId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    IReadOnlyList<EffectivePermissionDto> Permissions,
    IReadOnlyList<string> RoleCodes);

/// <summary>کاربر قابل جستجو در محدوده.</summary>
/// <param name="UserId">کاربر.</param>
/// <param name="RoleCodes">نقش‌ها.</param>
public sealed record AccessUserHitDto(Guid UserId, IReadOnlyList<string> RoleCodes);

/// <summary>خطای دامنهٔ Access Control با کد پایدار.</summary>
public sealed class AccessControlException : Exception
{
    /// <summary>استثنا را با کد پایدار می‌سازد.</summary>
    public AccessControlException(string code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>کد پایدار.</summary>
    public string Code { get; }
}

/// <summary>دایرکتوری Access Control (پیکربندی PG + همگام‌سازی SpiceDB).</summary>
public interface IAccessControlDirectory
{
    /// <summary>فهرست نقش‌ها.</summary>
    Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(AccessOwnerScope owner, bool includeArchived, CancellationToken cancellationToken);

    /// <summary>خواندن نقش.</summary>
    Task<AccessRoleDto?> GetRoleAsync(Guid roleId, AccessOwnerScope owner, CancellationToken cancellationToken);

    /// <summary>ایجاد نقش.</summary>
    Task<AccessRoleDto> CreateRoleAsync(AccessOwnerScope owner, CreateAccessRoleCommand command, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>به‌روزرسانی نقش.</summary>
    Task<AccessRoleDto> UpdateRoleAsync(Guid roleId, AccessOwnerScope owner, UpdateAccessRoleCommand command, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

    /// <summary>کلون نقش.</summary>
    Task<AccessRoleDto> CloneRoleAsync(Guid roleId, AccessOwnerScope owner, CloneAccessRoleCommand command, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

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

    /// <summary>تنظیم سقف فروشنده.</summary>
    Task SetSellerCeilingAsync(Guid sellerPartyId, IReadOnlyList<(string PermissionId, bool Enabled)> entries, Guid actorUserId, string? traceId, CancellationToken cancellationToken);

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
