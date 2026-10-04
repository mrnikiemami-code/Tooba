using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Access.Models;

/// <summary>یک مجوز مؤثر با محدوده.</summary>
public sealed record EffectivePermissionDto(
    string PermissionId,
    string Module,
    AccessScopeKind ScopeKind,
    Guid? ScopeResourceId,
    IReadOnlyList<string> InheritedViaRoleCodes,
    bool DeniedByCeiling,
    string? ScopeDisplayName = null);
