namespace Tooba.Catalog.Domain;

/// <summary>نرمال‌سازی slug و فهرست مسیرهای رزروشدهٔ سامانه.</summary>
public static class StoreLandingPageSlug
{
    /// <summary>اولین قطعهٔ مسیرهای سامانه که صفحهٔ پویا نباید سایه بیندازد.</summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "account", "admin", "api", "auth", "best-seller", "blog", "blogs", "brand", "brands",
        "cart", "categories", "category", "checkout", "customer-panel", "design-system",
        "en", "fa", "favicon", "home", "icon", "login", "most-viewed", "new-products",
        "not-found", "offers", "order", "page", "pages", "payment", "product", "products",
        "sale", "search", "seller-profile", "sellers", "settings", "shipping", "trending",
        "v1", "vendor-panel",
    };

    /// <summary>slug را کوچک و hyphen می‌کند؛ slash و traversal حذف می‌شود.</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var chars = raw.Trim().ToLowerInvariant().Replace('\\', '-').Replace('/', '-').ToCharArray();
        var buffer = new char[chars.Length];
        var n = 0;
        var hyphen = false;
        foreach (var ch in chars)
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                buffer[n++] = ch;
                hyphen = false;
                continue;
            }

            if (!hyphen && n > 0)
            {
                buffer[n++] = '-';
                hyphen = true;
            }
        }

        var value = new string(buffer, 0, n).Trim('-');
        return value.Length > StoreLandingPage.SlugMaxLength ? value[..StoreLandingPage.SlugMaxLength].Trim('-') : value;
    }

    /// <summary>slug نرمال باید غیرخالی و بدون نقطهٔ traversal باشد.</summary>
    public static bool IsValid(string slug) =>
        slug.Length > 0
        && slug.Length <= StoreLandingPage.SlugMaxLength
        && !slug.Contains("..", StringComparison.Ordinal)
        && slug.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    /// <summary>آیا slug با مسیر سامانه برخورد می‌کند.</summary>
    public static bool IsReserved(string slug) => Reserved.Contains(slug);

    /// <summary>locale ورودی را به fa یا en می‌رساند.</summary>
    public static string NormalizeLocale(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant() ?? "fa";
        if (value.StartsWith("en", StringComparison.Ordinal))
        {
            return "en";
        }

        return "fa";
    }
}
