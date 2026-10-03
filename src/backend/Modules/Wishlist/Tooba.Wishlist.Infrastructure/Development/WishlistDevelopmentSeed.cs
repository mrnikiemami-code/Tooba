using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Wishlist.Domain.Aggregates;
using Tooba.Wishlist.Infrastructure.Persistence;

namespace Tooba.Wishlist.Infrastructure.Development;

/// <summary>دانهٔ قطعی Development که مرزهای مستقل Catalog و Wishlist را از طریق Contracts هماهنگ می‌کند.</summary>
public static class WishlistDevelopmentSeed
{
    /// <summary>برای مشتری نمایشی از حداکثر سه ردهٔ متفاوت محصول Published انتخاب و idempotent درج می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var wishlist = services.GetRequiredService<WishlistDbContext>();
        var sampler = services.GetRequiredService<ICatalogDevelopmentPublishedProductSampler>();
        var actor = StorefrontGuestActor.ActorId;
        var productIds = await sampler.TakePublishedProductIdsFromDistinctCategoriesAsync(3, cancellationToken);
        foreach (var productId in productIds)
        {
            if (await wishlist.Items.AnyAsync(
                    x => x.OwnerUserId == actor && x.ProductId == productId,
                    cancellationToken))
            {
                continue;
            }

            wishlist.Items.Add(WishlistItem.Create(
                actor,
                productId,
                new DateTimeOffset(2026, 8, 25, 12, 30, 0, TimeSpan.Zero)));
        }

        await wishlist.SaveChangesAsync(cancellationToken);
    }
}
