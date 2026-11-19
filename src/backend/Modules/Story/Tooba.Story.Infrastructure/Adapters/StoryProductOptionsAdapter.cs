using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Story.Application.Stories.Ports;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>انتخاب محصول استوری: مالکیت پیشنهاد فروشنده یا جستجوی Published Catalog.</summary>
public sealed class StoryProductOptionsAdapter(
    IOfferSellerProductIdLookup? sellerProducts,
    ICatalogAdminProductTitleIdLookup? titles,
    ICatalogPublishedProductSearchLookup? publishedSearch) : IStoryProductOptionsPort
{
    /// <inheritdoc />
    public async Task<StoryProductOptionsPage> SearchForSellerAsync(
        Guid sellerPartyId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (sellerPartyId == Guid.Empty || sellerProducts is null || titles is null)
            return new StoryProductOptionsPage([], 0);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var term = (search ?? string.Empty).Trim();

        var ids = await sellerProducts.ListDistinctProductIdsForSellerAsync(sellerPartyId, cancellationToken);
        if (ids.Count == 0)
            return new StoryProductOptionsPage([], 0);

        var titleMap = await titles.GetProductTitlesByIdsAsync(ids, cancellationToken);
        IEnumerable<StoryProductOption> eligible = ids
            .Where(titleMap.ContainsKey)
            .Select(id => new StoryProductOption(id, titleMap[id]));

        if (term.Length > 0)
        {
            eligible = eligible.Where(row =>
                row.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = eligible
            .OrderBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var total = ordered.Count;
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new StoryProductOptionsPage(pageItems, total);
    }

    /// <inheritdoc />
    public async Task<StoryProductOptionsPage> SearchPublishedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (publishedSearch is null)
            return new StoryProductOptionsPage([], 0);

        var (items, total) = await publishedSearch.SearchPublishedAsync(
            search,
            page,
            pageSize,
            cancellationToken);
        return new StoryProductOptionsPage(
            items.Select(x => new StoryProductOption(x.ProductId, x.Title)).ToList(),
            total);
    }
}
