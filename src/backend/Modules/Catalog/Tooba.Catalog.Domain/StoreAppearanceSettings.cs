namespace Tooba.Catalog.Domain;

/// <summary>حالت تم ذخیره‌شده. فروشگاه فعلاً فقط Light را اعمال می‌کند.</summary>
public enum StoreAppearanceThemeMode
{
    /// <summary>روشن؛ معادل بصری فعلی فروشگاه.</summary>
    Light = 0,

    /// <summary>رزرو آینده؛ در این موج روی فروشگاه اعمال نمی‌شود.</summary>
    Dark = 1,
}

/// <summary>یک ردیف تنظیم ظاهری فروشگاه. مالک همان الگوی تنظیمات Store در Catalog است.</summary>
public sealed class StoreAppearanceSettings
{
    /// <summary>شناسه تک‌ردیفی.</summary>
    public static readonly Guid SingletonId = Guid.Parse("01900000-0000-7000-8000-00000000aa01");

    /// <summary>کلید ردیف.</summary>
    public Guid SettingsId { get; init; }

    /// <summary>کلید پالت از پیش تعریف‌شده.</summary>
    public string PaletteKey { get; private set; } = StoreAppearancePaletteRegistry.DefaultPaletteKey;

    /// <summary>حالت تم ذخیره‌شده.</summary>
    public StoreAppearanceThemeMode ThemeMode { get; private set; } = StoreAppearanceThemeMode.Light;

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف پیش‌فرض پالت آبی توبا.</summary>
    public static StoreAppearanceSettings CreateDefault(DateTimeOffset now) => new()
    {
        SettingsId = SingletonId,
        PaletteKey = StoreAppearancePaletteRegistry.DefaultPaletteKey,
        ThemeMode = StoreAppearanceThemeMode.Light,
        UpdatedAt = now,
    };

    /// <summary>پالت و حالت را با fallback کلید ناشناخته جایگزین می‌کند.</summary>
    public void Replace(string? paletteKey, StoreAppearanceThemeMode themeMode, DateTimeOffset now)
    {
        PaletteKey = StoreAppearancePaletteRegistry.ResolveKey(paletteKey);
        ThemeMode = themeMode == StoreAppearanceThemeMode.Dark
            ? StoreAppearanceThemeMode.Dark
            : StoreAppearanceThemeMode.Light;
        UpdatedAt = now;
    }
}
