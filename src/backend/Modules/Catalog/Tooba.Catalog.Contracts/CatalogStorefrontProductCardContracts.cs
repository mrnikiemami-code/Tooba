namespace Tooba.Catalog.Contracts;

/// <summary>
/// کارت فهرست فروشگاه برای مصرف‌کنندگان بین‌ماژولی (مثلاً Wishlist) بدون نشت Catalog.Application.
/// شکل JSON با <c>StorefrontProductCard</c> هم‌تراز نگه داشته می‌شود.
/// </summary>
public sealed record CatalogStorefrontProductCardDto(
    Guid ProductId,
    string Slug,
    string Title,
    string CategoryName,
    Guid? CategoryId,
    Guid? MediaAssetId,
    Guid PrimaryOfferId,
    Guid SellerPartyId,
    string SellerDisplayName,
    decimal OfferAmountExclusiveOfTax,
    decimal? PromotionalAmountExclusiveOfTax,
    string Currency,
    decimal AvailableUnits,
    bool InStock,
    string? PromotionLabel,
    decimal? AverageRating = null,
    long ReviewCount = 0,
    Guid? BrandId = null,
    Guid? MerchandisingCampaignId = null);

/// <summary>مرز Contracts ترکیب کارت محصول فروشگاه برای شناسه‌های درخواستی.</summary>
public interface ICatalogStorefrontProductCardLookup
{
    /// <summary>
    /// کارت‌های قابل‌فروش برای شناسه‌ها؛ محصول unpublished یا بدون Offer/Price در دیکشنری نیست.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, CatalogStorefrontProductCardDto>> ComposeProductCardsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
