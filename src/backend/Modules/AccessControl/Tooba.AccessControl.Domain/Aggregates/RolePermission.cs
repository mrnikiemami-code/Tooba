using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Domain.Aggregates;

/// <summary>مجوز متصل به نقش با scope اختیاری.</summary>
public sealed class RolePermission
{
    /// <summary>شناسه.</summary>
    public Guid Id { get; set; }

    /// <summary>نقش.</summary>
    public Guid RoleId { get; set; }

    /// <summary>شناسهٔ کاتالوگ مجوز.</summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>گونهٔ scope.</summary>
    public AccessScopeKind ScopeKind { get; set; } = AccessScopeKind.GlobalWithinOwner;

    /// <summary>منبع scope.</summary>
    public Guid? ScopeResourceId { get; set; }

    /// <summary>فعال.</summary>
    public bool Enabled { get; set; } = true;
}
