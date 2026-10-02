using Tooba.AccessControl.Application.Permissions;
using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Models;

/// <summary>زمینهٔ مالک برای عملیات دایرکتوری.</summary>
/// <param name="Kind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک.</param>
/// <param name="TenantId">Tenant اختیاری.</param>
public sealed record AccessOwnerScope(AccessOwnerScopeKind Kind, Guid? OwnerScopeId, string? TenantId = null);

/// <summary>فرمان ایجاد نقش.</summary>
public sealed record CreateAccessRoleCommand(string Name, string Code, string Description);

/// <summary>فرمان به‌روزرسانی نقش.</summary>
public sealed record UpdateAccessRoleCommand(string Name, string Description);

/// <summary>فرمان کلون نقش.</summary>
public sealed record CloneAccessRoleCommand(string Name, string Code, string? Description);

/// <summary>یک اعطای مجوز روی نقش.</summary>
public sealed record RolePermissionGrant(
    string PermissionId,
    AccessScopeKind ScopeKind,
    Guid? ScopeResourceId,
    bool Enabled);

/// <summary>DTO نقش با شمارش‌ها.</summary>
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
public sealed record SellerCeilingEntryDto(
    string PermissionId,
    bool Enabled,
    bool Delegable,
    string Module,
    AccessScopeKind ScopeKind = AccessScopeKind.GlobalWithinOwner,
    Guid? ScopeResourceId = null);

/// <summary>یک مجوز مؤثر با محدوده.</summary>
public sealed record EffectivePermissionDto(
    string PermissionId,
    string Module,
    AccessScopeKind ScopeKind,
    Guid? ScopeResourceId,
    IReadOnlyList<string> InheritedViaRoleCodes,
    bool DeniedByCeiling,
    string? ScopeDisplayName = null);

/// <summary>پیش‌نمایش دسترسی مؤثر.</summary>
public sealed record EffectiveAccessDto(
    Guid UserId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    IReadOnlyList<EffectivePermissionDto> Permissions,
    IReadOnlyList<string> RoleCodes);

/// <summary>کاربر قابل جستجو در محدوده.</summary>
public sealed record AccessUserHitDto(
    Guid UserId,
    IReadOnlyList<string> RoleCodes,
    string? DisplayName = null,
    string? Email = null,
    string? Mobile = null);