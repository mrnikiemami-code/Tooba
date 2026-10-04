using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Permissions.Models;

/// <summary>یک اعطای مجوز روی نقش.</summary>
public sealed record RolePermissionGrant(
    string PermissionId,
    AccessScopeKind ScopeKind,
    Guid? ScopeResourceId,
    bool Enabled);
