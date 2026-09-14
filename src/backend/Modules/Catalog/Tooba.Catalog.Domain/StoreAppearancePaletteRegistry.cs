namespace Tooba.Catalog.Domain;

/// <summary>توکن‌های برند یک پالت از پیش تعریف‌شده. danger/success/warning اینجا نیستند.</summary>
public sealed record StoreAppearanceBrandTokens(
    string PrimaryRgb,
    string PrimaryStrongRgb,
    string OnPrimaryRgb,
    string FocusRgb,
    string PrimaryOnDarkRgb);

/// <summary>توکن‌های tint curated برای چهار نقش سطح؛ از primary-alpha ساخته نمی‌شوند.</summary>
public sealed record StoreAppearanceTintTokens(
    string PageBackgroundRgb,
    string SectionBackgroundRgb,
    string PageBackgroundDarkRgb,
    string SectionBackgroundDarkRgb,
    string SectionAlternateRgb,
    string SectionAccentRgb,
    string SectionAlternateDarkRgb,
    string SectionAccentDarkRgb)
{
    /// <summary>نام T015؛ همان SectionSurface.</summary>
    public string SectionSurfaceRgb => SectionBackgroundRgb;

    /// <summary>نام T015؛ همان SectionSurface تاریک.</summary>
    public string SectionSurfaceDarkRgb => SectionBackgroundDarkRgb;
}

/// <summary>تعریف curated یک پالت؛ کلید پایدار و توکن‌ها کد-مالک هستند.</summary>
public sealed record StoreAppearancePaletteDefinition(
    string Key,
    string NameFa,
    string NameEn,
    StoreAppearanceBrandTokens Tokens,
    StoreAppearanceTintTokens Tint);

/// <summary>ثبت پالت‌های curated. اجرای CSS/HTML/JS از دیتابیس ممنوع است.</summary>
public static class StoreAppearancePaletteRegistry
{
    /// <summary>پالت پیش‌فرض معادل بصری فعلی فروشگاه.</summary>
    public const string DefaultPaletteKey = "tooba-blue";

    /// <summary>پس‌زمینهٔ فعلی پوستهٔ فروشگاه (#f3f5f8) برای Neutral.</summary>
    public const string NeutralPageBackgroundRgb = "243 245 248";

    /// <summary>#2563EB به صورت RGB فاصله‌دار.</summary>
    public static readonly StoreAppearanceBrandTokens ToobaBlue = new(
        "37 99 235",
        "29 78 216",
        "255 255 255",
        "37 99 235",
        "59 115 237");

    /// <summary>tint آبی توبا؛ کاغذ خنک نه wash اشباع.</summary>
    public static readonly StoreAppearanceTintTokens ToobaBlueTint = new(
        "236 241 250",
        "241 245 252",
        "14 17 26",
        "18 22 32",
        "226 234 247",
        "209 223 244",
        "16 19 28",
        "24 30 44");

    private static readonly StoreAppearancePaletteDefinition[] Definitions =
    [
        new("tooba-blue", "آبی توبا", "Tooba Blue", ToobaBlue, ToobaBlueTint),
        new(
            "forest-green",
            "سبز جنگلی",
            "Forest Green",
            new("21 128 61", "22 101 52", "255 255 255", "21 128 61", "42 139 78"),
            new("236 244 238", "241 247 242", "13 18 15", "16 22 18", "226 238 230", "210 232 218", "14 20 16", "20 30 22")),
        new(
            "wine-burgundy",
            "شرابی تیره",
            "Wine Burgundy",
            new("159 18 57", "136 19 55", "255 255 255", "159 18 57", "189 91 118"),
            new("248 241 243", "250 245 246", "20 13 16", "24 16 19", "242 228 232", "232 210 218", "22 14 17", "32 18 24")),
        new(
            "slate-navy",
            "سرمه‌ای سنگی",
            "Slate Navy",
            new("30 58 95", "23 37 84", "255 255 255", "30 58 95", "104 123 148"),
            new("236 240 247", "241 244 250", "14 16 22", "18 21 28", "226 232 242", "210 220 236", "16 18 24", "24 28 38")),
        new(
            "amber-gold",
            "کهربایی",
            "Amber Gold",
            new("180 83 9", "146 64 14", "255 255 255", "180 83 9", "187 98 31"),
            new("249 243 235", "251 247 241", "20 15 11", "24 18 14", "244 234 220", "236 220 196", "22 16 12", "34 24 16")),
        new(
            "teal-lagoon",
            "سبزآبی مرداب",
            "Teal Lagoon",
            new("15 118 110", "17 94 89", "255 255 255", "15 118 110", "46 136 129"),
            new("235 244 243", "240 247 246", "12 18 18", "15 22 22", "224 238 236", "204 228 224", "13 20 20", "18 30 30")),
        new(
            "violet-royal",
            "بنفش سلطنتی",
            "Royal Violet",
            new("124 58 237", "109 40 217", "255 255 255", "124 58 237", "144 88 240"),
            new("242 237 250", "246 242 252", "17 13 24", "21 16 30", "232 224 246", "220 208 240", "19 14 26", "28 20 40")),
    ];

    /// <summary>فهرست پایدار پالت‌های مجاز.</summary>
    public static IReadOnlyList<StoreAppearancePaletteDefinition> All => Definitions;

    /// <summary>کلید ناشناخته به پالت پیش‌فرض برمی‌گردد.</summary>
    public static string ResolveKey(string? paletteKey)
    {
        var match = Find(paletteKey);
        return match?.Key ?? DefaultPaletteKey;
    }

    /// <summary>توکن برند را برای کلید مؤثر برمی‌گرداند.</summary>
    public static StoreAppearanceBrandTokens ResolveTokens(string? paletteKey)
        => Find(ResolveKey(paletteKey))?.Tokens ?? ToobaBlue;

    /// <summary>توکن tint curated را برای کلید مؤثر برمی‌گرداند.</summary>
    public static StoreAppearanceTintTokens ResolveTint(string? paletteKey)
        => Find(ResolveKey(paletteKey))?.Tint ?? ToobaBlueTint;

    /// <summary>آیا کلید در ثبت موجود است.</summary>
    public static bool IsKnown(string? paletteKey)
        => Find(paletteKey) is not null;

    private static StoreAppearancePaletteDefinition? Find(string? paletteKey)
    {
        var key = paletteKey?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        foreach (var item in Definitions)
        {
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }
}
