using Tooba.Catalog.Application.Storefront.Models;
using Tooba.Catalog.Application.Storefront.Ports;
using Tooba.Catalog.Contracts;

namespace Tooba.Catalog.Infrastructure.Adapters;

/// <summary>Contracts adapter over storefront composer for cross-module product cards.</summary>
public sealed class CatalogStorefrontProductCardLookup(IStorefrontComposer storefront)
    : ICatalogStorefrontProductCardLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, CatalogStorefrontProductCardDto>> ComposeProductCardsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var cards = await storefront.ComposeProductCardsAsync(productIds, cancellationToken);
        return cards.ToDictionary(x => x.Key, x => Map(x.Value));
    }

    private static CatalogStorefrontProductCardDto Map(StorefrontProductCard card) =>
        new(
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
