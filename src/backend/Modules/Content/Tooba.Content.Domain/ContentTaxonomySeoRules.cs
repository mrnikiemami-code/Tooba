namespace Tooba.Content.Domain;

/// <summary>مسیرهای عمومی دسته و نویسندهٔ محتوا.</summary>
public static class ContentTaxonomySeoRules
{
    /// <summary>locale محتوای پیش‌فرض (فارسی).</summary>
    public const string DefaultContentLocale = "fa-IR";

    /// <summary>fa / en / fa-IR / en-US را به locale محتوا نگاشت می‌کند.</summary>
    public static string ResolveContentLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return DefaultContentLocale;
        }

        var trimmed = locale.Trim();
        if (trimmed.Equals("fa", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("fa-", StringComparison.OrdinalIgnoreCase))
        {
            return "fa-IR";
        }

        if (trimmed.Equals("en", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
        {
            return "en-US";
        }

        return trimmed;
    }

    /// <summary>prefix URL از locale محتوا.</summary>
    public static string UrlPrefixForLocale(string locale)
    {
        var normalized = locale.Trim().ToLowerInvariant();
        return normalized.StartsWith("en", StringComparison.Ordinal) ? "en" : "fa";
    }

    /// <summary>مسیر عمومی دسته — /fa/blogs/category/{slug}.</summary>
    public static string BuildCategoryPublicPath(string locale, string slug) =>
        $"/{UrlPrefixForLocale(locale)}/blogs/category/{slug.Trim().ToLowerInvariant()}";

    /// <summary>مسیر عمومی نویسنده — /fa/blogs/author/{slug}.</summary>
    public static string BuildAuthorPublicPath(string locale, string slug) =>
        $"/{UrlPrefixForLocale(locale)}/blogs/author/{slug.Trim().ToLowerInvariant()}";
}
