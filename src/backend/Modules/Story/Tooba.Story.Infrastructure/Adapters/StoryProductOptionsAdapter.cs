using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Story.Application.Stories.Ports;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>انتخاب محصول استوری: مالکیت پیشنهاد فروشنده به‌همراه عنوان/کارت Catalog.</summary>
public sealed class StoryProductOptionsAdapter(
    IOfferSellerProductIdLookup? sellerProducts,
    ICatalogStorefrontProductCardLookup? products,
    ICatalogDevelopmentPublishedProductSampler? publishedSampler = null) : IStoryProductOptionsPort
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryProductOption>> ListForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        if (sellerPartyId == Guid.Empty || sellerProducts is null || products is null)
            return [];

        var ids = await sellerProducts.ListDistinctProductIdsForSellerAsync(sellerPartyId, cancellationToken);
        if (ids.Count == 0)
            return [];

        var cards = await products.ComposeProductCardsAsync(ids, cancellationToken);
        return ids
            .Where(cards.ContainsKey)
            .Select(id => new StoryProductOption(id, cards[id].Title))
            .OrderBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryProductOption>> ListPublishedAsync(CancellationToken cancellationToken)
    {
        if (products is null || publishedSampler is null)
            return [];

        var ids = await publishedSampler.TakePublishedProductIdsFromDistinctCategoriesAsync(40, cancellationToken);
        if (ids.Count == 0)
            return [];

        var cards = await products.ComposeProductCardsAsync(ids, cancellationToken);
        return ids
            .Where(cards.ContainsKey)
            .Select(id => new StoryProductOption(id, cards[id].Title))
            .OrderBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
