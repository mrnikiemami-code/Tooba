using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Roles.Models;

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
