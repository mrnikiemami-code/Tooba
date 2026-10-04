using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Assignments.Models;

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
