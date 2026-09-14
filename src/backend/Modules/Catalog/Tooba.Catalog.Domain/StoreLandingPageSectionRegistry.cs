#pragma warning disable CS1591
namespace Tooba.Catalog.Domain;

/// <summary>انواع Section تأییدشدهٔ Landing. فقط کلیدهای این فهرست مجازند.</summary>
public static class StoreLandingPageSectionRegistry
{
    public const string Hero = "Hero";
    public const string ProductCollection = "ProductCollection";
    public const string CategoryGrid = "CategoryGrid";
    public const string BrandStrip = "BrandStrip";
    public const string PromoBanner = "PromoBanner";
    public const string ArticleList = "ArticleList";
    public const string Reviews = "Reviews";
    public const string RichText = "RichText";

    public const int MaxSectionsPerPage = 40;
    public const int MaxTake = 24;
    public const int DefaultTake = 8;
    public const int ConfigMaxLength = 4000;
    public const int TitleMaxLength = 200;
    public const int TextMaxLength = 2000;

    public static readonly IReadOnlySet<string> ApprovedTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        Hero, ProductCollection, CategoryGrid, BrandStrip, PromoBanner, ArticleList, Reviews, RichText,
    };

    public static readonly IReadOnlySet<string> ProductSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Manual", "Category", "Brand", "Newest",
    };

    public static readonly IReadOnlySet<string> UnsupportedProductSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BestSelling", "Featured", "Discounted",
    };

    public static bool IsApproved(string? sectionType) =>
        !string.IsNullOrWhiteSpace(sectionType) && ApprovedTypes.Contains(sectionType.Trim());

    public static string NormalizeType(string? sectionType)
    {
        var value = sectionType?.Trim() ?? string.Empty;
        foreach (var key in ApprovedTypes)
        {
            if (string.Equals(key, value, StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return value;
    }
}
