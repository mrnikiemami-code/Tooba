using Tooba.Catalog.Contracts;
using Tooba.Wishlist.Application.Models;
using Tooba.Wishlist.Application.Ports;

namespace Tooba.Wishlist.Application.Composition;

/// <summary>نمای خصوصی Wishlist را با کارت‌های زندهٔ فروشگاه از مرز Catalog.Contracts ترکیب می‌کند.</summary>
public sealed class WishlistPresentationComposer(
    IWishlistDirectory wishlist,
    ICatalogStorefrontProductCardLookup productCards)
{
    /// <summary>
    /// ردیف‌های Actor را ترکیب می‌کند؛ محصول unpublished یا فاقد Offer/Price به‌صورت صادقانه unavailable و بدون کارت بازمی‌گردد.
    /// </summary>
    public async Task<WishlistPage> ListAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var entries = await wishlist.ListAsync(actorUserId, cancellationToken);
        var cards = await productCards.ComposeProductCardsAsync(
            entries.Select(x => x.ProductId).ToArray(),
            cancellationToken);
        return new WishlistPage(entries.Select(entry => new WishlistPageItem(
            entry.WishlistItemId,
            entry.ProductId,
            entry.CreatedAt,
            cards.GetValueOrDefault(entry.ProductId),
            cards.ContainsKey(entry.ProductId) ? null : "product-unavailable")).ToList());
    }
}
