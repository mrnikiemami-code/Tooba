using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.StoreMenus.Models;
using Tooba.Catalog.Application.StoreMenus.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>دانهٔ idempotent منوی دمو برای بازرسی بعدی کاربر.</summary>
public static class StoreMenuDevelopmentSeed
{
    /// <summary>Slug صفحهٔ دمو Landing — owned by <see cref="LandingPageDevelopmentSeed"/>.</summary>
    public const string LandingDemoSlug = LandingPageDevelopmentSeed.PublishedSlug;

    /// <summary>Slug صفحهٔ کمپین Landing — owned by <see cref="LandingPageDevelopmentSeed"/>.</summary>
    public const string LandingCampaignSlug = LandingPageDevelopmentSeed.CampaignSlug;

    /// <summary>منوی دمو را اگر نیست می‌سازد و آیتم‌های بازرسی را بدون بازنویسی کامل تکمیل می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<CatalogDbContext>();
        var workspace = provider.GetRequiredService<IStoreMenuWorkspace>();
        var existingId = await catalog.StoreMenus.AsNoTracking()
            .Where(x => x.Locale == "fa" && x.MenuKey == StoreMenu.DemoMenuKey)
            .Select(x => (Guid?)x.MenuId)
            .FirstOrDefaultAsync(cancellationToken);
        Guid menuId;
        if (existingId is { } id)
        {
            menuId = id;
        }
        else
        {
            var menu = await workspace.CreateAsync(
                new StoreMenuWriteRequest("منوی دموی فروشگاه", "fa", StoreMenu.DemoMenuKey, true),
                cancellationToken);
            menuId = menu.MenuId;
        }

        await EnsureDemoItemsAsync(catalog, workspace, menuId, cancellationToken);
    }

    private static async Task EnsureDemoItemsAsync(
        CatalogDbContext catalog,
        IStoreMenuWorkspace workspace,
        Guid menuId,
        CancellationToken cancellationToken)
    {
        var detail = await workspace.GetAsync(menuId, cancellationToken);
        StoreMenuItemAdminView? Find(string label) =>
            detail.Items.FirstOrDefault(x => x.Label == label);

        async Task<StoreMenuItemAdminView?> Ensure(string label, string linkType, Guid? parentId, Guid? targetId, int sortOrder)
        {
            var found = Find(label);
            if (found is not null)
            {
                return found;
            }

            try
            {
                var created = await workspace.AddItemAsync(
                    menuId,
                    new StoreMenuItemWriteRequest(label, linkType, parentId, targetId, null, sortOrder, true),
                    cancellationToken);
                detail = await workspace.GetAsync(menuId, cancellationToken);
                return created;
            }
            catch (PlatformHttpException)
            {
                return Find(label);
            }
        }

        await Ensure("خانه", "Home", null, null, 0);
        var group = await Ensure("خرید", "Group", null, null, 1);

        try
        {
            var categoryId = await catalog.Categories.AsNoTracking()
                .Select(x => (Guid?)x.CategoryId)
                .FirstOrDefaultAsync(cancellationToken);
            StoreMenuItemAdminView? l2 = null;
            if (categoryId is { } cat && group is not null)
            {
                l2 = await Ensure("دسته‌ها", "Category", group.MenuItemId, cat, 0);
            }

            var productId = await catalog.Products.AsNoTracking()
                .Where(x => x.Status == CatalogPublicationStatus.Published)
                .Select(x => (Guid?)x.ProductId)
                .FirstOrDefaultAsync(cancellationToken);
            if (l2 is not null && productId is { } product)
            {
                await Ensure("کالای منتخب", "Product", l2.MenuItemId, product, 0);
            }
        }
        catch (PlatformHttpException)
        {
        }

        try
        {
            var landing = await catalog.StoreLandingPages.AsNoTracking()
                .Where(x => x.Locale == "fa"
                    && x.Slug == LandingDemoSlug
                    && x.Status == StoreLandingPageStatus.Published)
                .Select(x => (Guid?)x.PageId)
                .FirstOrDefaultAsync(cancellationToken);
            if (landing is { } pageId)
            {
                await Ensure("صفحهٔ دمو", "LandingPage", null, pageId, 2);
            }

            var campaign = await catalog.StoreLandingPages.AsNoTracking()
                .Where(x => x.Locale == "fa"
                    && x.Slug == LandingCampaignSlug
                    && x.Status == StoreLandingPageStatus.Published)
                .Select(x => (Guid?)x.PageId)
                .FirstOrDefaultAsync(cancellationToken);
            if (campaign is { } campaignId)
            {
                await Ensure("صفحهٔ کمپین", "LandingPage", null, campaignId, 3);
            }
        }
        catch (PlatformHttpException)
        {
        }
    }
}
