using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>دانهٔ idempotent منوی دمو برای بازرسی بعدی کاربر.</summary>
internal static class StoreMenuDevelopmentSeed
{
    /// <summary>منوی دمو را اگر نیست می‌سازد و بازنویسی نمی‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<CatalogDbContext>();
        var composer = provider.GetRequiredService<StoreMenuComposer>();
        if (await catalog.StoreMenus.AnyAsync(x => x.Locale == "fa" && x.MenuKey == StoreMenu.DemoMenuKey, cancellationToken))
        {
            return;
        }

        var menu = await composer.CreateAsync(
            new StoreMenuWriteRequest("منوی دموی فروشگاه", "fa", StoreMenu.DemoMenuKey, true),
            cancellationToken);

        var home = await composer.AddItemAsync(
            menu.MenuId,
            new StoreMenuItemWriteRequest("خانه", "Home", null, null, null, 0, true),
            cancellationToken);
        var group = await composer.AddItemAsync(
            menu.MenuId,
            new StoreMenuItemWriteRequest("خرید", "Group", null, null, null, 1, true),
            cancellationToken);

        try
        {
            var categoryId = await catalog.Categories.AsNoTracking().Select(x => (Guid?)x.CategoryId).FirstOrDefaultAsync(cancellationToken);
            StoreMenuItemAdminView? l2 = null;
            if (categoryId is { } cat)
            {
                l2 = await composer.AddItemAsync(
                    menu.MenuId,
                    new StoreMenuItemWriteRequest("دسته‌ها", "Category", group.MenuItemId, cat, null, 0, true),
                    cancellationToken);
            }

            var productId = await catalog.Products.AsNoTracking()
                .Where(x => x.Status == CatalogPublicationStatus.Published)
                .Select(x => (Guid?)x.ProductId)
                .FirstOrDefaultAsync(cancellationToken);
            if (l2 is not null && productId is { } product)
            {
                await composer.AddItemAsync(
                    menu.MenuId,
                    new StoreMenuItemWriteRequest("کالای منتخب", "Product", l2.MenuItemId, product, null, 0, true),
                    cancellationToken);
            }
        }
        catch (PlatformHttpException)
        {
        }

        try
        {
            var landing = await catalog.StoreLandingPages.AsNoTracking()
                .Where(x => x.Locale == "fa" && x.Slug == LandingPageDevelopmentSeed.PublishedSlug && x.Status == StoreLandingPageStatus.Published)
                .Select(x => (Guid?)x.PageId)
                .FirstOrDefaultAsync(cancellationToken);
            if (landing is { } pageId)
            {
                await composer.AddItemAsync(
                    menu.MenuId,
                    new StoreMenuItemWriteRequest("صفحهٔ دمو", "LandingPage", null, pageId, null, 2, true),
                    cancellationToken);
            }
        }
        catch (PlatformHttpException)
        {
        }

        _ = home;
    }
}

/// <summary>اعمال دانه با زمینهٔ فروشگاه توسعه.</summary>
internal static class StoreMenuDevelopmentSeedHost
{
    /// <summary>دانه را روی Host توسعه اجرا می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        await StoreMenuDevelopmentSeed.ApplyAsync(provider);
    }
}
