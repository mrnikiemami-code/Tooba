namespace Tooba.Catalog.Domain;

/// <summary>توکن‌های برند یک پالت از پیش تعریف‌شده. danger/success/warning اینجا نیستند.</summary>
public sealed record StoreAppearanceBrandTokens(
    string PrimaryRgb,
    string PrimaryStrongRgb,
    string OnPrimaryRgb,
    string FocusRgb);

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
        "37 99 235");

    /// <summary>کلید ناشناخته به پالت پیش‌فرض برمی‌گردد.</summary>
    public static string ResolveKey(string? paletteKey)
    {
        if (string.Equals(paletteKey?.Trim(), DefaultPaletteKey, StringComparison.OrdinalIgnoreCase))
        {
            return DefaultPaletteKey;
        }

        return DefaultPaletteKey;
    }

    /// <summary>توکن برند را برای کلید مؤثر برمی‌گرداند.</summary>
    public static StoreAppearanceBrandTokens ResolveTokens(string? paletteKey)
    {
        _ = ResolveKey(paletteKey);
        return ToobaBlue;
    }

    /// <summary>آیا کلید در ثبت موجود است.</summary>
    public static bool IsKnown(string? paletteKey)
        => string.Equals(paletteKey?.Trim(), DefaultPaletteKey, StringComparison.OrdinalIgnoreCase);
}
