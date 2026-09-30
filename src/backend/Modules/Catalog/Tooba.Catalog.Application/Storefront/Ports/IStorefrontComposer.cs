using Tooba.Catalog.Application.Storefront.Models;

namespace Tooba.Catalog.Application.Storefront.Ports;

/// <summary>
/// Catalog-owned storefront read composition (home/PLP/PDP/brands/sellers/merchandising/cards).
/// FOUNDATION_PARTIAL: Infrastructure implements via CatalogDbContext + Contracts enrichment.
/// </summary>
public interface IStorefrontComposer
{
    /// <summary>Composes the storefront home page.</summary>
    Task<StorefrontHomePage> GetHomeAsync(string? locale, CancellationToken cancellationToken);

    /// <summary>Builds featured review rail items.</summary>
    Task<IReadOnlyList<StorefrontFeaturedReviewItem>> BuildFeaturedReviewsAsync(CancellationToken cancellationToken);

    /// <summary>Builds latest article rail items.</summary>
    Task<IReadOnlyList<StorefrontArticleItem>> BuildLatestArticlesAsync(
        string? locale,
        CancellationToken cancellationToken);

    /// <summary>Lists published brands for storefront rails.</summary>
    Task<IReadOnlyList<StorefrontBrandItem>> ListBrandsAsync(CancellationToken cancellationToken);

    /// <summary>Loads a brand landing page by slug.</summary>
    Task<StorefrontBrandPage?> GetBrandAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Lists public sellers derived from active offers.</summary>
    Task<IReadOnlyList<StorefrontPublicSellerItem>> ListPublicSellersAsync(CancellationToken cancellationToken);

    /// <summary>Loads a public seller page by opaque public id.</summary>
    Task<StorefrontPublicSellerPage?> GetPublicSellerAsync(string publicId, CancellationToken cancellationToken);

    /// <summary>Composes a merchandising kind page.</summary>
    Task<StorefrontMerchandisingPage> GetMerchandisingAsync(string kind, CancellationToken cancellationToken);

    /// <summary>Lists published categories.</summary>
    Task<IReadOnlyList<StorefrontCategoryItem>> ListCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>Composes a filtered product listing page.</summary>
    Task<StorefrontListingPage> GetListingAsync(
        string? q,
        Guid? categoryId,
        Guid? sellerPartyId,
        bool? inStock,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Composes a category PLP with facets/filters.</summary>
    Task<StorefrontCategoryPlpPage?> GetCategoryPlpAsync(
        string locale,
        string slug,
        IReadOnlyList<StorefrontPlpFilterInput> filters,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Composes a product detail page by slug.</summary>
    Task<StorefrontProductDetailPage?> GetDetailAsync(
        string slug,
        Guid? variantId,
        CancellationToken cancellationToken);

    /// <summary>Composes live product cards for the requested product ids only.</summary>
    Task<IReadOnlyDictionary<Guid, StorefrontProductCard>> ComposeProductCardsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
