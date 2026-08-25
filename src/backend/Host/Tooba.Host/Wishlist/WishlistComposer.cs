using Tooba.Host.Storefront;
using Tooba.Wishlist.Application;

namespace Tooba.Host.Wishlist;

/// <summary>نمای خصوصی Wishlist را با کارت‌های زندهٔ فروشگاه ترکیب می‌کند.</summary>
public sealed class WishlistComposer
{
    private readonly IWishlistDirectory _wishlist;
    private readonly StorefrontComposer _storefront;

    /// <summary>دایرکتوری مالک و ترکیب‌گر Host را دریافت می‌کند.</summary>
    public WishlistComposer(IWishlistDirectory wishlist, StorefrontComposer storefront)
    {
        _wishlist = wishlist;
        _storefront = storefront;
    }

    /// <summary>
    /// ردیف‌های Actor را ترکیب می‌کند؛ محصول unpublished یا فاقد Offer/Price به‌صورت صادقانه unavailable و بدون کارت بازمی‌گردد.
    /// </summary>
    public async Task<WishlistPage> ListAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var entries = await _wishlist.ListAsync(actorUserId, cancellationToken);
        var cards = await _storefront.ComposeProductCardsAsync(entries.Select(x => x.ProductId).ToArray(), cancellationToken);
        return new WishlistPage(entries.Select(entry => new WishlistPageItem(
            entry.WishlistItemId,
            entry.ProductId,
            entry.CreatedAt,
            cards.GetValueOrDefault(entry.ProductId),
            cards.ContainsKey(entry.ProductId) ? null : "product-unavailable")).ToList());
    }
}

/// <summary>صفحهٔ خصوصی Wishlist بدون افشای شناسهٔ مالک.</summary>
public sealed record WishlistPage(IReadOnlyList<WishlistPageItem> Items);

/// <summary>ردیف Wishlist همراه کارت زنده یا دلیل صادقانهٔ عدم امکان ترکیب.</summary>
public sealed record WishlistPageItem(
    Guid WishlistItemId,
    Guid ProductId,
    DateTimeOffset CreatedAt,
    StorefrontProductCard? Product,
    string? UnavailableReason);
