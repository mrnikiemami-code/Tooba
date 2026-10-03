using System.Text.Json;

namespace Tooba.PageComposition.Domain.Catalog;

/// <summary>کاتالوگ ثابت انواع section تأییدشده.</summary>
public static class SectionCatalog
{
    /// <summary>نوع section.</summary>
    public const string Hero = "hero";
    /// <summary>نوع section.</summary>
    public const string Stories = "stories";
    /// <summary>نوع section.</summary>
    public const string CategoryGrid = "category_grid";
    /// <summary>نوع section.</summary>
    public const string ProductRailFlash = "product_rail_flash";
    /// <summary>نوع section.</summary>
    public const string BestSellers = "best_sellers";
    /// <summary>نوع section.</summary>
    public const string ProductRailMostViewed = "product_rail_most_viewed";
    /// <summary>نوع section.</summary>
    public const string MiddleBanners = "middle_banners";
    /// <summary>نوع section.</summary>
    public const string Brands = "brands";
    /// <summary>نوع section.</summary>
    public const string NewestProducts = "newest_products";
    /// <summary>نوع section.</summary>
    public const string CustomerReviews = "customer_reviews";
    /// <summary>نوع section.</summary>
    public const string LatestArticles = "latest_articles";

    /// <summary>variant پیش‌فرض.</summary>
    public const string DefaultVariant = "default";

    /// <summary>حداکثر طول عنوان config.</summary>
    public const int TitleMaxLength = 120;
    /// <summary>حداقل itemCount.</summary>
    public const int ItemCountMin = 1;
    /// <summary>حداکثر itemCount.</summary>
    public const int ItemCountMax = 24;
    /// <summary>حداکثر طول href.</summary>
    public const int HrefMaxLength = 256;

    private static readonly string[] DefaultHomeSectionOrder =
    [
        Hero,
        Stories,
        CategoryGrid,
        ProductRailFlash,
        BestSellers,
        ProductRailMostViewed,
        MiddleBanners,
        Brands,
        NewestProducts,
        CustomerReviews,
        LatestArticles,
    ];

    private static readonly HashSet<string> ForbiddenConfigKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "css",
        "html",
        "js",
        "className",
    };

    private static readonly HashSet<string> AllowedConfigKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "title",
        "href",
        "itemCount",
        "sourceKind",
    };

    private static readonly HashSet<string> AllowedSourceKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "offers",
        "most_viewed",
        "new_arrivals",
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> AllowedVariants =
        new(StringComparer.Ordinal)
        {
            [Hero] = [DefaultVariant],
            [Stories] = [DefaultVariant],
            [CategoryGrid] = [DefaultVariant],
            [ProductRailFlash] = [DefaultVariant],
            [BestSellers] = [DefaultVariant],
            [ProductRailMostViewed] = [DefaultVariant],
            [MiddleBanners] = [DefaultVariant],
            [Brands] = [DefaultVariant],
            [NewestProducts] = [DefaultVariant],
            [CustomerReviews] = [DefaultVariant],
            [LatestArticles] = [DefaultVariant],
        };

    /// <summary>همهٔ انواع section ثابت.</summary>
    public static IReadOnlyList<string> AllSectionTypes => DefaultHomeSectionOrder;

    /// <summary>ترتیب پیش‌فرض sectionهای خانه.</summary>
    public static IReadOnlyList<string> DefaultHomeSectionTypes => DefaultHomeSectionOrder;

    /// <summary>variantهای مجاز برای یک نوع section.</summary>
    public static IReadOnlyList<string> GetAllowedVariants(string sectionType)
    {
        EnsureKnownSectionType(sectionType);
        return AllowedVariants[sectionType];
    }

    /// <summary>نوع section ناشناخته را رد می‌کند.</summary>
    public static void EnsureKnownSectionType(string sectionType)
    {
        if (string.IsNullOrWhiteSpace(sectionType) || !AllowedVariants.ContainsKey(sectionType))
            throw new InvalidOperationException("نوع section در کاتالوگ تأییدشده نیست.");
    }

    /// <summary>variant را برای نوع section اعتبارسنجی می‌کند.</summary>
    public static void EnsureAllowedVariant(string sectionType, string variant)
    {
        EnsureKnownSectionType(sectionType);
        if (string.IsNullOrWhiteSpace(variant) || !AllowedVariants[sectionType].Contains(variant))
            throw new InvalidOperationException("variant section مجاز نیست.");
    }

    /// <summary>JSON config امن را اعتبارسنجی و نرمال می‌کند.</summary>
    public static string ValidateAndNormalizeConfiguration(string sectionType, string? configurationJson)
    {
        EnsureKnownSectionType(sectionType);
        if (string.IsNullOrWhiteSpace(configurationJson))
            return "{}";

        using var document = JsonDocument.Parse(configurationJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("پیکربندی section باید شیء JSON باشد.");

        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (ForbiddenConfigKeys.Contains(property.Name))
                throw new InvalidOperationException($"کلید config ممنوع است: {property.Name}");
            if (!AllowedConfigKeys.Contains(property.Name))
                throw new InvalidOperationException($"کلید config ناشناخته است: {property.Name}");

            switch (property.Name.ToLowerInvariant())
            {
                case "title":
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidOperationException("title باید رشته باشد.");
                    var title = property.Value.GetString()?.Trim() ?? string.Empty;
                    if (title.Length == 0 || title.Length > TitleMaxLength)
                        throw new InvalidOperationException("title معتبر نیست.");
                    normalized["title"] = title;
                    break;
                case "href":
                    if (!SupportsHref(sectionType))
                        throw new InvalidOperationException("href برای این section مجاز نیست.");
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidOperationException("href باید رشته باشد.");
                    var href = property.Value.GetString()?.Trim() ?? string.Empty;
                    if (href.Length == 0 || href.Length > HrefMaxLength)
                        throw new InvalidOperationException("href معتبر نیست.");
                    normalized["href"] = href;
                    break;
                case "itemcount":
                    if (!SupportsItemCount(sectionType))
                        throw new InvalidOperationException("itemCount برای این section مجاز نیست.");
                    if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out var itemCount))
                        throw new InvalidOperationException("itemCount باید عدد صحیح باشد.");
                    if (itemCount < ItemCountMin || itemCount > ItemCountMax)
                        throw new InvalidOperationException("itemCount خارج از بازهٔ مجاز است.");
                    normalized["itemCount"] = itemCount;
                    break;
                case "sourcekind":
                    if (!SupportsSourceKind(sectionType))
                        throw new InvalidOperationException("sourceKind برای این section مجاز نیست.");
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidOperationException("sourceKind باید رشته باشد.");
                    var sourceKind = property.Value.GetString()?.Trim() ?? string.Empty;
                    if (!AllowedSourceKinds.Contains(sourceKind))
                        throw new InvalidOperationException("sourceKind مجاز نیست.");
                    normalized["sourceKind"] = sourceKind;
                    break;
            }
        }

        return JsonSerializer.Serialize(normalized);
    }

    /// <summary>متادیتای schema config برای catalog API.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ConfigSchemaMetadata =>
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["title"] = ["string", $"max:{TitleMaxLength}"],
            ["href"] = ["string", $"max:{HrefMaxLength}", "rails"],
            ["itemCount"] = ["integer", $"min:{ItemCountMin}", $"max:{ItemCountMax}", "rails", "articles"],
            ["sourceKind"] = ["enum:offers,most_viewed,new_arrivals", "rails"],
        };

    private static bool SupportsHref(string sectionType) =>
        sectionType is ProductRailFlash or ProductRailMostViewed or BestSellers or NewestProducts or LatestArticles;

    private static bool SupportsItemCount(string sectionType) =>
        sectionType is ProductRailFlash or ProductRailMostViewed or BestSellers or NewestProducts or LatestArticles;

    private static bool SupportsSourceKind(string sectionType) =>
        sectionType is ProductRailFlash or ProductRailMostViewed or NewestProducts;
}
