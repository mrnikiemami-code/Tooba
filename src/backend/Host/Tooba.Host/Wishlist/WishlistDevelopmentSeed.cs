using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Storefront;
using Tooba.Wishlist.Domain;
using Tooba.Wishlist.Infrastructure.Persistence;

namespace Tooba.Host.Wishlist;

/// <summary>دانهٔ قطعی Development که مرزهای مستقل Catalog و Wishlist را در Host هماهنگ می‌کند.</summary>
public static class WishlistDevelopmentSeed
{
    /// <summary>برای مشتری نمایشی از حداکثر سه ردهٔ متفاوت محصول Published انتخاب و idempotent درج می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var catalog = services.GetRequiredService<CatalogDbContext>();
        var wishlist = services.GetRequiredService<WishlistDbContext>();
        var actor = StorefrontCheckoutComposer.StorefrontGuestActorId;
        var links = await catalog.ProductCategories.AsNoTracking()
            .Where(link => catalog.Products.Any(product =>
                product.ProductId == link.ProductId && product.Status == CatalogPublicationStatus.Published))
            .OrderBy(link => link.CategoryId).ThenBy(link => link.ProductId)
            .ToListAsync(cancellationToken);
        var productIds = links.GroupBy(x => x.CategoryId).Select(x => x.First().ProductId).Take(3).ToList();
        foreach (var productId in productIds)
        {
            if (await wishlist.Items.AnyAsync(x => x.OwnerUserId == actor && x.ProductId == productId, cancellationToken)) continue;
            wishlist.Items.Add(WishlistItem.Create(actor, productId, new DateTimeOffset(2026, 8, 25, 12, 30, 0, TimeSpan.Zero)));
        }
        await wishlist.SaveChangesAsync(cancellationToken);
    }
}
