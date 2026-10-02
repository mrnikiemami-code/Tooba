using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Domain.Aggregates;

/// <summary>سقف مجوز قابل تفویض فروشنده توسط پلتفرم.</summary>
public sealed class PlatformSellerCeiling
{
    /// <summary>شناسه.</summary>
    public Guid Id { get; set; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; set; }

    /// <summary>مجوز.</summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>گونهٔ scope سقف.</summary>
    public AccessScopeKind ScopeKind { get; set; } = AccessScopeKind.GlobalWithinOwner;

    /// <summary>منبع scope سقف (مثلاً CategoryId).</summary>
    public Guid? ScopeResourceId { get; set; }

    /// <summary>فعال در سقف.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
