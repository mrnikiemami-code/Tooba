namespace Tooba.AccessControl.Domain;

/// <summary>محدودهٔ مالک نقش: پلتفرم یا فروشنده.</summary>
public enum AccessOwnerScopeKind
{
    /// <summary>نقش‌های پلتفرم / Admin.</summary>
    Platform = 1,

    /// <summary>نقش‌های یک فروشنده.</summary>
    Seller = 2,
}

/// <summary>انواع scope منبع تایپ‌شده؛ بدون expression آزاد.</summary>
public enum AccessScopeKind
{
    /// <summary>کل محدودهٔ مالک بدون محدودیت منبع.</summary>
    GlobalWithinOwner = 1,

    /// <summary>محدود به دسته.</summary>
    Category = 2,

    /// <summary>محدود به محصول.</summary>
    Product = 3,

    /// <summary>محدود به برند.</summary>
    Brand = 4,

    /// <summary>محدود به انبار.</summary>
    Warehouse = 5,

    /// <summary>محدود به فروشگاه.</summary>
    Store = 6,

    /// <summary>محدود به قطعهٔ سفارش.</summary>
    OrderSegment = 7,
}

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

/// <summary>انتساب نقش به کاربر در محدودهٔ مالک.</summary>
public sealed class UserRoleAssignment
{
    /// <summary>شناسه.</summary>
    public Guid Id { get; set; }

    /// <summary>کاربر.</summary>
    public Guid UserId { get; set; }

    /// <summary>نقش.</summary>
    public Guid RoleId { get; set; }

    /// <summary>گونهٔ مالک.</summary>
    public AccessOwnerScopeKind OwnerScopeKind { get; set; }

    /// <summary>شناسهٔ مالک.</summary>
    public Guid? OwnerScopeId { get; set; }

    /// <summary>زمان تخصیص.</summary>
    public DateTimeOffset AssignedAt { get; set; }
}

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

/// <summary>رویداد حسابرسی Access Control.</summary>
public sealed class AccessAuditEvent
{
    /// <summary>شناسه.</summary>
    public Guid Id { get; set; }

    /// <summary>بازیگر.</summary>
    public Guid ActorUserId { get; set; }

    /// <summary>عمل.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>نوع هدف.</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>شناسهٔ هدف.</summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>محدودهٔ فروشنده.</summary>
    public Guid? SellerScopeId { get; set; }

    /// <summary>خلاصهٔ قبل.</summary>
    public string BeforeSummary { get; set; } = string.Empty;

    /// <summary>خلاصهٔ بعد.</summary>
    public string AfterSummary { get; set; } = string.Empty;

    /// <summary>شناسهٔ ردیابی.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>زمان.</summary>
    public DateTimeOffset At { get; set; }
}
