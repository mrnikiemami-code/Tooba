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
        "228 236 248",
        "247 250 253",
        "12 15 22",
        "22 26 36",
        "216 228 244",
        "188 210 236",
        "15 18 27",
        "32 40 56");

    private static readonly StoreAppearancePaletteDefinition[] Definitions =
    [
        new("tooba-blue", "آبی توبا", "Tooba Blue", ToobaBlue, ToobaBlueTint),
        new(
            "forest-green",
            "سبز جنگلی",
            "Forest Green",
            new("21 128 61", "22 101 52", "255 255 255", "21 128 61", "42 139 78"),
            new("228 242 232", "247 251 247", "12 16 13", "20 26 22", "214 234 220", "188 224 200", "14 20 16", "26 38 28")),
        new(
            "wine-burgundy",
            "شرابی تیره",
            "Wine Burgundy",
            new("159 18 57", "136 19 55", "255 255 255", "159 18 57", "189 91 118"),
            new("244 232 236", "252 248 249", "18 12 15", "26 16 20", "236 218 224", "222 192 204", "20 13 16", "36 18 26")),
        new(
            "slate-navy",
            "سرمه‌ای سنگی",
            "Slate Navy",
            new("30 58 95", "23 37 84", "255 255 255", "30 58 95", "104 123 148"),
            new("228 234 246", "246 248 252", "12 14 20", "22 25 34", "216 224 238", "190 206 228", "15 17 24", "30 36 48")),
        new(
            "amber-gold",
            "کهربایی",
            "Amber Gold",
            new("180 83 9", "146 64 14", "255 255 255", "180 83 9", "187 98 31"),
            new("246 238 224", "252 249 244", "18 14 10", "26 20 14", "240 226 206", "228 208 176", "20 15 11", "38 26 16")),
        new(
            "teal-lagoon",
            "سبزآبی مرداب",
            "Teal Lagoon",
            new("15 118 110", "17 94 89", "255 255 255", "15 118 110", "46 136 129"),
            new("226 240 238", "244 250 249", "11 17 17", "18 26 26", "210 232 228", "182 220 214", "13 20 20", "22 36 34")),
        new(
            "violet-royal",
            "بنفش سلطنتی",
            "Royal Violet",
            new("124 58 237", "109 40 217", "255 255 255", "124 58 237", "144 88 240"),
            new("236 228 248", "248 245 252", "15 12 22", "24 18 34", "224 212 242", "206 190 234", "17 13 24", "34 24 46")),
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
