using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>مقصد کنترل‌شدهٔ آیتم منو. HTML/JS آزاد نیست.</summary>
public enum StoreMenuLinkType
{
    /// <summary>خانهٔ فروشگاه.</summary>
    Home = 0,

    /// <summary>صفحهٔ Landing منتشرشده.</summary>
    LandingPage = 1,

    /// <summary>کالای منتشرشده.</summary>
    Product = 2,

    /// <summary>ردهٔ فروشگاه.</summary>
    Category = 3,

    /// <summary>برند عمومی.</summary>
    Brand = 4,

    /// <summary>مقالهٔ محتوا.</summary>
    Article = 5,

    /// <summary>نشانی http/https معتبر.</summary>
    External = 6,

    /// <summary>عنوان گروهی بدون پیوند.</summary>
    Group = 7,
}

/// <summary>منوی ساختاریافتهٔ فروشگاه. محتوای اجرایی ندارد.</summary>
public sealed class StoreMenu
{
    /// <summary>حداکثر طول عنوان.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>حداکثر طول کلید داخلی.</summary>
    public const int MenuKeyMaxLength = 64;

    /// <summary>حداکثر طول locale.</summary>
    public const int LocaleMaxLength = 16;

    /// <summary>کلید دانهٔ دمو.</summary>
    public const string DemoMenuKey = "store-demo-menu";

    private StoreMenu()
    {
    }

    /// <summary>شناسهٔ پایدار منو.</summary>
    public Guid MenuId { get; init; }

    /// <summary>عنوان انسانی.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>locale کاننیکال fa یا en.</summary>
    public string Locale { get; private set; } = "fa";

    /// <summary>کلید پایدار داخلی؛ برای دانه و ارجاع فنی.</summary>
    public string MenuKey { get; private set; } = string.Empty;

    /// <summary>آیا منو برای ارجاع عمومی فعال است.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>منوی جدید می‌سازد.</summary>
    public static StoreMenu Create(string? title, string? locale, string? menuKey, DateTimeOffset now)
    {
        return new StoreMenu
        {
            MenuId = UuidV7.New(),
            Title = NormalizeTitle(title),
            Locale = StoreLandingPageSlug.NormalizeLocale(locale),
            MenuKey = NormalizeMenuKey(menuKey),
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>عنوان و زبان را به‌روز می‌کند.</summary>
    public void Update(string? title, string? locale, DateTimeOffset now)
    {
        Title = NormalizeTitle(title);
        Locale = StoreLandingPageSlug.NormalizeLocale(locale ?? Locale);
        UpdatedAt = now;
    }

    /// <summary>فعال یا غیرفعال می‌کند.</summary>
    public void SetEnabled(bool enabled, DateTimeOffset now)
    {
        IsEnabled = enabled;
        UpdatedAt = now;
    }

    /// <summary>زمان ویرایش آیتم را روی منو هم می‌زند.</summary>
    public void Touch(DateTimeOffset now) => UpdatedAt = now;

    private static string NormalizeTitle(string? title)
    {
        var value = title?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            throw new PlatformHttpException(400, "عنوان منو لازم است.", "menu.title.required");
        }

        return value.Length > TitleMaxLength ? value[..TitleMaxLength] : value;
    }

    private static string NormalizeMenuKey(string? menuKey)
    {
        var value = (menuKey ?? string.Empty).Trim().ToLowerInvariant();
        if (value.Length == 0)
        {
            return $"menu-{UuidV7.New():N}"[..MenuKeyMaxLength];
        }

        return value.Length > MenuKeyMaxLength ? value[..MenuKeyMaxLength] : value;
    }
}
