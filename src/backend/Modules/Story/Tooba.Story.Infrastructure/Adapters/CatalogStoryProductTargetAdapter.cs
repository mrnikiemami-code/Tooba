using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>مالکیت محصول CTA استوری از طریق کارت‌های محصول ویترین Catalog.</summary>
public sealed class CatalogStoryProductTargetAdapter(ICatalogStorefrontProductCardLookup products)
    : IStoryProductTargetPort
{
    /// <inheritdoc />
    public async Task EnsureOwnedBySellerAsync(
        Guid productId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty || sellerPartyId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

        var cards = await products.ComposeProductCardsAsync([productId], cancellationToken);
        if (!cards.TryGetValue(productId, out var card) || card.SellerPartyId != sellerPartyId)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
    }
}
