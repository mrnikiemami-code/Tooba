namespace Tooba.Catalog.Domain;

/// <summary>تعریف curated یک پوستهٔ کارت کالا. فقط ارائه، نه رفتار.</summary>
public sealed record StoreAppearanceProductCardSkinDefinition(
    string Key,
    string NameFa,
    string NameEn);

/// <summary>ثبت پوسته‌های کنترل‌شدهٔ کارت کالا. CSS/HTML اجرایی از دیتابیس ممنوع است.</summary>
public static class StoreAppearanceProductCardSkinRegistry
{
    /// <summary>کلید پیش‌فرض پوستهٔ کلاسیک.</summary>
    public const string DefaultSkinKey = "classic";

    private static readonly StoreAppearanceProductCardSkinDefinition[] Definitions =
    [
        new("classic", "کلاسیک", "Classic"),
        new("clean", "ساده", "Clean"),
        new("elevated", "برجسته", "Elevated"),
        new("glass", "شیشه‌ای", "Glass"),
    ];

    /// <summary>فهرست پوسته‌های مجاز.</summary>
    public static IReadOnlyList<StoreAppearanceProductCardSkinDefinition> All => Definitions;

    /// <summary>کلید ناشناخته به classic نگاشت می‌شود.</summary>
    public static string ResolveKey(string? skinKey)
    {
        var match = Find(skinKey);
        return match?.Key ?? DefaultSkinKey;
    }

    /// <summary>آیا کلید در فهرست curated است.</summary>
    public static bool IsKnown(string? skinKey) => Find(skinKey) is not null;

    private static StoreAppearanceProductCardSkinDefinition? Find(string? skinKey)
    {
        var key = skinKey?.Trim();
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
