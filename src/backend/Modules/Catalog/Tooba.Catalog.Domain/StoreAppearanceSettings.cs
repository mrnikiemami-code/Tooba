namespace Tooba.Catalog.Domain;

/// <summary>حالت تم کنترل‌شدهٔ فروشگاه. رشتهٔ دلخواه پذیرفته نمی‌شود.</summary>
public enum StoreAppearanceThemeMode
{
    /// <summary>میراث ذخیره‌شده؛ معادل LightOnly.</summary>
    Light = 0,

    /// <summary>میراث ذخیره‌شده؛ معادل DarkOnly.</summary>
    Dark = 1,

    /// <summary>فروشگاه همیشه روشن است.</summary>
    LightOnly = 2,

    /// <summary>فروشگاه همیشه تاریک است.</summary>
    DarkOnly = 3,

    /// <summary>از prefers-color-scheme پیروی می‌کند.</summary>
    System = 4,

    /// <summary>کاربر می‌تواند روشن/تاریک را روی دستگاه انتخاب کند.</summary>
    UserChoice = 5,
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

    /// <summary>پوستهٔ کنترل‌شدهٔ کارت کالا.</summary>
    public string ProductCardSkin { get; private set; } = StoreAppearanceProductCardSkinRegistry.DefaultSkinKey;

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف پیش‌فرض پالت آبی توبا و تم روشن.</summary>
    public static StoreAppearanceSettings CreateDefault(DateTimeOffset now) => new()
    {
        SettingsId = SingletonId,
        PaletteKey = StoreAppearancePaletteRegistry.DefaultPaletteKey,
        ThemeMode = StoreAppearanceThemeMode.Light,
        ProductCardSkin = StoreAppearanceProductCardSkinRegistry.DefaultSkinKey,
        UpdatedAt = now,
    };

    /// <summary>پالت و حالت را با fallback کلید ناشناخته جایگزین می‌کند.</summary>
    public void Replace(string? paletteKey, StoreAppearanceThemeMode themeMode, DateTimeOffset now)
        => Replace(paletteKey, themeMode, productCardSkin: null, now);

    /// <summary>پالت، تم و پوستهٔ کارت را با fallback کلید ناشناخته جایگزین می‌کند.</summary>
    public void Replace(string? paletteKey, StoreAppearanceThemeMode themeMode, string? productCardSkin, DateTimeOffset now)
    {
        PaletteKey = StoreAppearancePaletteRegistry.ResolveKey(paletteKey);
        ThemeMode = NormalizeThemeMode(themeMode);
        ProductCardSkin = StoreAppearanceProductCardSkinRegistry.ResolveKey(productCardSkin ?? ProductCardSkin);
        UpdatedAt = now;
    }

    /// <summary>Light/Dark میراث به LightOnly/DarkOnly نگاشت می‌شود.</summary>
    public static StoreAppearanceThemeMode NormalizeThemeMode(StoreAppearanceThemeMode mode) => mode switch
    {
        StoreAppearanceThemeMode.Dark or StoreAppearanceThemeMode.DarkOnly => StoreAppearanceThemeMode.DarkOnly,
        StoreAppearanceThemeMode.System => StoreAppearanceThemeMode.System,
        StoreAppearanceThemeMode.UserChoice => StoreAppearanceThemeMode.UserChoice,
        _ => StoreAppearanceThemeMode.LightOnly,
    };

    /// <summary>رشتهٔ ورودی را به حالت کاننیکال تبدیل می‌کند.</summary>
    public static bool TryParseThemeMode(string? raw, out StoreAppearanceThemeMode mode)
    {
        switch (raw?.Trim())
        {
            case "Light":
            case "LightOnly":
                mode = StoreAppearanceThemeMode.LightOnly;
                return true;
            case "Dark":
            case "DarkOnly":
                mode = StoreAppearanceThemeMode.DarkOnly;
                return true;
            case "System":
                mode = StoreAppearanceThemeMode.System;
                return true;
            case "UserChoice":
                mode = StoreAppearanceThemeMode.UserChoice;
                return true;
            default:
                mode = StoreAppearanceThemeMode.LightOnly;
                return false;
        }
    }
}
