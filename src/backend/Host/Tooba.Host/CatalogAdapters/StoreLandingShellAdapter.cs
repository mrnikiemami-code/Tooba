using Tooba.Catalog.Application.StoreLandingPages.Models;
using Tooba.Catalog.Application.StoreLandingPages.Ports;
using Tooba.Content.Domain.Rules;
using Tooba.Host.Storefront;

namespace Tooba.Host.CatalogAdapters;

/// <summary>Host adapter: StorefrontComposer → Catalog Landing shell port.</summary>
public sealed class StoreLandingShellAdapter : IStoreLandingShellPort
{
    private readonly StorefrontComposer _storefront;

    /// <summary>Creates the adapter.</summary>
    public StoreLandingShellAdapter(StorefrontComposer storefront) => _storefront = storefront;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, StoreLandingShellProductCard>> ComposeProductCardsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var composed = await _storefront.ComposeProductCardsAsync(productIds, cancellationToken);
        return composed.ToDictionary(kv => kv.Key, kv => Map(kv.Value));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreLandingShellCategoryItem>> ListCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var items = await _storefront.ListCategoriesAsync(cancellationToken);
        return items.Select(x => new StoreLandingShellCategoryItem(x.CategoryId, x.ParentCategoryId, x.Name)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreLandingShellBrandItem>> ListBrandsAsync(CancellationToken cancellationToken)
    {
        var items = await _storefront.ListBrandsAsync(cancellationToken);
        return items.Select(x => new StoreLandingShellBrandItem(
            x.BrandId,
            x.Slug,
            x.Name,
            x.ProductCount,
            x.LogoMediaAssetId)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreLandingShellArticleItem>> BuildLatestArticlesAsync(
        string pageLocale,
        CancellationToken cancellationToken)
    {
        var contentLocale = ContentTaxonomySeoRules.ResolveContentLocale(pageLocale);
        var items = await _storefront.BuildLatestArticlesAsync(contentLocale, cancellationToken);
        return items.Select(x => new StoreLandingShellArticleItem(
            x.ArticleId,
            x.Slug,
            x.Title,
            x.Excerpt,
            x.CoverMediaAssetId,
            x.PublishDate,
            x.AuthorDisplayName,
            x.Tags,
            x.IsFeatured)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreLandingShellFeaturedReviewItem>> BuildFeaturedReviewsAsync(
        CancellationToken cancellationToken)
    {
        var items = await _storefront.BuildFeaturedReviewsAsync(cancellationToken);
        return items.Select(x => new StoreLandingShellFeaturedReviewItem(
            x.PublicId,
            x.AuthorDisplayName,
            x.Rating,
            x.Title,
            x.Body,
            x.VerifiedPurchase,
            x.CreatedAt,
            x.ProductTitle,
            x.ProductSlug)).ToList();
    }

    private static StoreLandingShellProductCard Map(StorefrontProductCard card) => new(
        card.ProductId,
        card.Slug,
        card.Title,
        card.CategoryName,
        card.CategoryId,
        card.MediaAssetId,
        card.PrimaryOfferId,
        card.SellerPartyId,
        card.SellerDisplayName,
        card.OfferAmountExclusiveOfTax,
        card.PromotionalAmountExclusiveOfTax,
        card.Currency,
        card.AvailableUnits,
        card.InStock,
        card.PromotionLabel,
        card.AverageRating,
        card.ReviewCount,
        card.BrandId,
        card.MerchandisingCampaignId);
}
