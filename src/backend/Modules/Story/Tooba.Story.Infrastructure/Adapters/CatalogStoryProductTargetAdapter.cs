using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>
/// اعتبار CTA محصول استوری: مالکیت از پیشنهاد واجد شرایط فروشنده، نه برنده‌ی کارت ویترین.
/// مسیر عمومی همچنان از کارت Catalog برای slug می‌آید.
/// </summary>
public sealed class CatalogStoryProductTargetAdapter(
    ICatalogStorefrontProductCardLookup products,
    IOfferSellerProductIdLookup? sellerProducts = null)
    : IStoryProductTargetPort, IStoryProductPathBatchPort
{
    /// <inheritdoc />
    public async Task EnsureOwnedBySellerAsync(
        Guid productId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty || sellerPartyId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

        if (sellerProducts is not null)
        {
            var owned = await sellerProducts.ListDistinctProductIdsForSellerAsync(sellerPartyId, cancellationToken);
            if (owned.Contains(productId))
                return;
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
        }

        // بدون lookup پیشنهاد، به‌جای اعتماد به برنده‌ی کارت، رد می‌شود.
        throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
    }

    /// <inheritdoc />
    public async Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

        var cards = await products.ComposeProductCardsAsync([productId], cancellationToken);
        if (!cards.ContainsKey(productId))
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
    }

    /// <inheritdoc />
    public async Task<string?> TryResolvePublicProductPathAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
            return null;
        var cards = await products.ComposeProductCardsAsync([productId], cancellationToken);
        if (!cards.TryGetValue(productId, out var card) || string.IsNullOrWhiteSpace(card.Slug))
            return null;
        return "/products/" + Uri.EscapeDataString(card.Slug.Trim());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> ResolvePublicProductPathsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var paths = new Dictionary<Guid, string>(ids.Count);
        if (ids.Count == 0)
            return paths;

        // ONE Catalog composition for the whole page instead of one call per CTA.
        var cards = await products.ComposeProductCardsAsync(ids, cancellationToken);
        foreach (var id in ids)
        {
            if (cards.TryGetValue(id, out var card) && !string.IsNullOrWhiteSpace(card.Slug))
                paths[id] = "/products/" + Uri.EscapeDataString(card.Slug.Trim());
        }
        return paths;
    }
}
