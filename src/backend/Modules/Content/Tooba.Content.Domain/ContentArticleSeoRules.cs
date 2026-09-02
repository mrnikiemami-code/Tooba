namespace Tooba.Content.Domain;

/// <summary>مسیرهای عمومی canonical مقاله بر اساس locale.</summary>
public static class ContentArticleSeoRules
{
    /// <summary>prefix URL از locale محتوا.</summary>
    public static string UrlPrefixForLocale(string locale)
    {
        var normalized = locale.Trim().ToLowerInvariant();
        return normalized.StartsWith("en", StringComparison.Ordinal) ? "en" : "fa";
    }

    /// <summary>مسیر داخلی بدون origin — /fa/blogs/{slug}.</summary>
    public static string BuildPublicPath(string locale, string slug)
    {
        var prefix = UrlPrefixForLocale(locale);
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return $"/{prefix}/blogs/{normalizedSlug}";
    }
}
