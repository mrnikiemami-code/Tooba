using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Ceiling.Models;

/// <summary>DTO سقف فروشنده.</summary>
public sealed record SellerCeilingEntryDto(
    string PermissionId,
    bool Enabled,
    bool Delegable,
    string Module,
    AccessScopeKind ScopeKind = AccessScopeKind.GlobalWithinOwner,
    Guid? ScopeResourceId = null);
