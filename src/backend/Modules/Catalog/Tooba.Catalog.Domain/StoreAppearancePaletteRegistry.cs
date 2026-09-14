namespace Tooba.Catalog.Domain;

/// <summary>توکن‌های برند یک پالت از پیش تعریف‌شده. danger/success/warning اینجا نیستند.</summary>
public sealed record StoreAppearanceBrandTokens(
    string PrimaryRgb,
    string PrimaryStrongRgb,
    string OnPrimaryRgb,
    string FocusRgb,
    string PrimaryOnDarkRgb);

/// <summary>تعریف curated یک پالت؛ کلید پایدار و توکن‌ها کد-مالک هستند.</summary>
public sealed record StoreAppearancePaletteDefinition(
    string Key,
    string NameFa,
    string NameEn,
    StoreAppearanceBrandTokens Tokens);

/// <summary>ثبت پالت‌های curated. اجرای CSS/HTML/JS از دیتابیس ممنوع است.</summary>
public static class StoreAppearancePaletteRegistry
{
    /// <summary>پالت پیش‌فرض معادل بصری فعلی فروشگاه.</summary>
    public const string DefaultPaletteKey = "tooba-blue";

    /// <summary>#2563EB به صورت RGB فاصله‌دار.</summary>
    public static readonly StoreAppearanceBrandTokens ToobaBlue = new(
        "37 99 235",
        "29 78 216",
        "255 255 255",
        "37 99 235",
        "59 115 237");

    private static readonly StoreAppearancePaletteDefinition[] Definitions =
    [
        new("tooba-blue", "آبی توبا", "Tooba Blue", ToobaBlue),
        new("forest-green", "سبز جنگلی", "Forest Green", new("21 128 61", "22 101 52", "255 255 255", "21 128 61", "42 139 78")),
        new("wine-burgundy", "شرابی تیره", "Wine Burgundy", new("159 18 57", "136 19 55", "255 255 255", "159 18 57", "189 91 118")),
        new("slate-navy", "سرمه‌ای سنگی", "Slate Navy", new("30 58 95", "23 37 84", "255 255 255", "30 58 95", "104 123 148")),
        new("amber-gold", "کهربایی", "Amber Gold", new("180 83 9", "146 64 14", "255 255 255", "180 83 9", "187 98 31")),
        new("teal-lagoon", "سبزآبی مرداب", "Teal Lagoon", new("15 118 110", "17 94 89", "255 255 255", "15 118 110", "46 136 129")),
        new("violet-royal", "بنفش سلطنتی", "Royal Violet", new("124 58 237", "109 40 217", "255 255 255", "124 58 237", "144 88 240")),
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
