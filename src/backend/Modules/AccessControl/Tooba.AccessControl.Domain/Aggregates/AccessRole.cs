using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Domain.Aggregates;

/// <summary>نقش پویای Access Control.</summary>
public sealed class AccessRole
{
    /// <summary>شناسه.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant اختیاری.</summary>
    public string? TenantId { get; set; }

    /// <summary>گونهٔ مالک.</summary>
    public AccessOwnerScopeKind OwnerScopeKind { get; set; }

    /// <summary>شناسهٔ مالک (مثلاً SellerPartyId).</summary>
    public Guid? OwnerScopeId { get; set; }

    /// <summary>نام نمایشی.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>کد پایدار.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>توضیح.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>نقش سیستمی.</summary>
    public bool IsSystem { get; set; }

    /// <summary>قابل ویرایش.</summary>
    public bool IsMutable { get; set; } = true;

    /// <summary>بایگانی‌شده.</summary>
    public bool IsArchived { get; set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
