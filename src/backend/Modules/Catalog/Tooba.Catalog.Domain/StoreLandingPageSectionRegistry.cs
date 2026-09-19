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
    public const string NavigationMenu = "NavigationMenu";
    public const string StoryRail = "StoryRail";
    public const string BannerShowcase = "BannerShowcase";

    public const int MaxSectionsPerPage = 40;
    public const int MaxTake = 48;
    public const int DefaultTake = 8;
    public const int ConfigMaxLength = 24000;
    public const int TitleMaxLength = 200;
    public const int TextMaxLength = 2000;
    public const int MaxBannerSlots = 8;
    public const int MaxStoryItems = 24;
    public const int MaxHeroSlides = 8;
    public const int DefaultHeroSlideIntervalSec = 5;

    public static readonly IReadOnlySet<string> ApprovedTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        Hero, ProductCollection, CategoryGrid, BrandStrip, PromoBanner, ArticleList, Reviews, RichText, NavigationMenu,
        StoryRail, BannerShowcase,
    };

    public static readonly IReadOnlySet<string> ProductSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Manual", "Category", "Brand", "Newest",
    };

    public static readonly IReadOnlySet<string> UnsupportedProductSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BestSelling", "Featured", "Discounted",
    };

    public static readonly IReadOnlySet<string> SizePresets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Compact", "Medium", "Large", "ExtraLarge",
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
