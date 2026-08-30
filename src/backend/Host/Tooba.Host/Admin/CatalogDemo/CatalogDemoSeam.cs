namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>
/// نشانگرهای پایدار مالکیت دادهٔ Catalog Demo برای reset امن و idempotent seed.
/// </summary>
public static class CatalogDemoSeam
{
    /// <summary>پیشوند slug رده‌های دانه.</summary>
    public const string CategorySlugPrefix = "demo-cat-";

    /// <summary>پیشوند slug برندهای دانه.</summary>
    public const string BrandSlugPrefix = "demo-brand-";

    /// <summary>پیشوند کد/slug برچسب‌های دانه.</summary>
    public const string TagCodePrefix = "demo-tag-";

    /// <summary>پیشوند کد تعریف ویژگی دانه.</summary>
    public const string AttributeCodePrefix = "demo_attr_";

    /// <summary>پیشوند OriginalFileName رسانهٔ دانه.</summary>
    public const string MediaFilePrefix = "demo-media-";

    /// <summary>پیشوند اختیاری محصول smoke (T034 جایگزین می‌کند).</summary>
    public const string SmokeProductSlugPrefix = "demo-smoke-";

    /// <summary>slugهای شناخته‌شدهٔ محصول junk از bootstrapهای قدیمی.</summary>
    public static readonly IReadOnlyList<string> LegacyJunkProductSlugs =
    [
        "workspace-live-shirt",
        "admin-r3-draft-scarf",
        "admin-r3-archived-hat",
        "schema-mobile-demo-phone",
        "acc-demo-mobile-phone",
        "acc-demo-books-novel",
        "demo-mobile-1",
    ];

    /// <summary>کدهای ویژگی junk قدیمی بدون پیشوند جدید.</summary>
    public static readonly IReadOnlyList<string> LegacyJunkAttributeCodes =
    [
        "demo_pack",
        "demo_origin",
        "acc-demo-color",
    ];

    /// <summary>slug برندهای bootstrap قدیمی فروشگاه.</summary>
    public static readonly IReadOnlyList<string> LegacyJunkBrandSlugs =
    [
        "xiaomi",
        "samsung",
        "apple",
        "lenovo",
        "asus",
        "bosch",
        "philips",
        "jbl",
        "tooba-live",
    ];

    /// <summary>آیا slug محصول متعلق به demo/junk است؟</summary>
    public static bool IsDemoOrJunkProductSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        if (slug.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || slug.StartsWith(SmokeProductSlugPrefix, StringComparison.OrdinalIgnoreCase)
            || slug.StartsWith("admin-r3-", StringComparison.OrdinalIgnoreCase)
            || slug.StartsWith("acc-demo-", StringComparison.OrdinalIgnoreCase)
            || slug.StartsWith("schema-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return LegacyJunkProductSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>آیا کد ویژگی متعلق به demo/junk است؟</summary>
    public static bool IsDemoOrJunkAttributeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (code.StartsWith(AttributeCodePrefix, StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("acc-demo-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return LegacyJunkAttributeCodes.Contains(code, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>آیا slug برند متعلق به demo/junk است؟</summary>
    public static bool IsDemoOrJunkBrandSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        if (slug.StartsWith(BrandSlugPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return LegacyJunkBrandSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>آیا کد برچسب متعلق به دانه است؟</summary>
    public static bool IsDemoTagCode(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && code.StartsWith(TagCodePrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>آیا slug رده متعلق به دانه است؟</summary>
    public static bool IsDemoCategorySlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.StartsWith(CategorySlugPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>آیا نام فایل رسانه متعلق به دانه است؟</summary>
    public static bool IsDemoMediaFileName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.StartsWith(MediaFilePrefix, StringComparison.OrdinalIgnoreCase);
}
