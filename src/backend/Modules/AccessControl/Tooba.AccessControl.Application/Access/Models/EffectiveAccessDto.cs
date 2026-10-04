using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Access.Models;

/// <summary>پیش‌نمایش دسترسی مؤثر.</summary>
public sealed record EffectiveAccessDto(
    Guid UserId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    IReadOnlyList<EffectivePermissionDto> Permissions,
    IReadOnlyList<string> RoleCodes);
