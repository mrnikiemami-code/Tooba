using Tooba.Catalog.Application.Storefront.Models;
using Tooba.Catalog.Application.Storefront.Ports;
using Tooba.Catalog.Application.StoreLandingPages.Models;
using Tooba.Catalog.Application.StoreLandingPages.Ports;

namespace Tooba.Catalog.Infrastructure.StoreLanding;

/// <summary>Catalog adapter: IStorefrontComposer → Catalog Landing shell port.</summary>
internal sealed class StoreLandingShellAdapter(IStorefrontComposer storefront) : IStoreLandingShellPort
{
    public async Task<IReadOnlyDictionary<Guid, StoreLandingShellProductCard>> ComposeProductCardsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var composed = await storefront.ComposeProductCardsAsync(productIds, cancellationToken);
        return composed.ToDictionary(kv => kv.Key, kv => Map(kv.Value));
    }

    public async Task<IReadOnlyList<StoreLandingShellCategoryItem>> ListCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var items = await storefront.ListCategoriesAsync(cancellationToken);
        return items.Select(x => new StoreLandingShellCategoryItem(x.CategoryId, x.ParentCategoryId, x.Name)).ToList();
    }

    public async Task<IReadOnlyList<StoreLandingShellBrandItem>> ListBrandsAsync(CancellationToken cancellationToken)
    {
        var items = await storefront.ListBrandsAsync(cancellationToken);
        return items.Select(x => new StoreLandingShellBrandItem(
            x.BrandId,
            x.Slug,
            x.Name,
            x.ProductCount,
            x.LogoMediaAssetId)).ToList();
    }

    public async Task<IReadOnlyList<StoreLandingShellArticleItem>> BuildLatestArticlesAsync(
        string pageLocale,
        CancellationToken cancellationToken)
    {
        var items = await storefront.BuildLatestArticlesAsync(pageLocale, cancellationToken);
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

    public async Task<IReadOnlyList<StoreLandingShellFeaturedReviewItem>> BuildFeaturedReviewsAsync(
        CancellationToken cancellationToken)
    {
        var items = await storefront.BuildFeaturedReviewsAsync(cancellationToken);
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
