namespace Tooba.Catalog.Application;

/// <summary>
/// زمینهٔ بازیگر برای ثبت تاریخچهٔ Catalog در همان درخواست Host.
/// </summary>
public interface ICatalogActorContext
{
    /// <summary>شناسهٔ کاربر بازیگر؛ null یعنی سیستم.</summary>
    Guid? ActorUserId { get; set; }

    /// <summary>نام نمایشی اختیاری در زمان ثبت.</summary>
    string? ActorDisplayName { get; set; }
}

/// <summary>پیاده‌سازی scoped زمینهٔ بازیگر Catalog.</summary>
public sealed class CatalogActorContext : ICatalogActorContext
{
    /// <inheritdoc />
    public Guid? ActorUserId { get; set; }

    /// <inheritdoc />
    public string? ActorDisplayName { get; set; }
}
