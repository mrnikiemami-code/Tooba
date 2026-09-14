using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>دانهٔ idempotent منوی دمو برای بازرسی بعدی کاربر.</summary>
internal static class StoreMenuDevelopmentSeed
{
    /// <summary>منوی دمو را اگر نیست می‌سازد و آیتم‌های بازرسی را بدون بازنویسی کامل تکمیل می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<CatalogDbContext>();
        var composer = provider.GetRequiredService<StoreMenuComposer>();
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
            var menu = await composer.CreateAsync(
                new StoreMenuWriteRequest("منوی دموی فروشگاه", "fa", StoreMenu.DemoMenuKey, true),
                cancellationToken);
            menuId = menu.MenuId;
        }

        await EnsureDemoItemsAsync(catalog, composer, menuId, cancellationToken);
    }

    private static async Task EnsureDemoItemsAsync(
        CatalogDbContext catalog,
        StoreMenuComposer composer,
        Guid menuId,
        CancellationToken cancellationToken)
    {
        var detail = await composer.GetAsync(menuId, cancellationToken);
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
                var created = await composer.AddItemAsync(
                    menuId,
                    new StoreMenuItemWriteRequest(label, linkType, parentId, targetId, null, sortOrder, true),
                    cancellationToken);
                detail = await composer.GetAsync(menuId, cancellationToken);
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
            var categoryId = await catalog.Categories.AsNoTracking().Select(x => (Guid?)x.CategoryId).FirstOrDefaultAsync(cancellationToken);
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
                .Where(x => x.Locale == "fa" && x.Slug == LandingPageDevelopmentSeed.PublishedSlug && x.Status == StoreLandingPageStatus.Published)
                .Select(x => (Guid?)x.PageId)
                .FirstOrDefaultAsync(cancellationToken);
            if (landing is { } pageId)
            {
                await Ensure("صفحهٔ دمو", "LandingPage", null, pageId, 2);
            }

            var campaign = await catalog.StoreLandingPages.AsNoTracking()
                .Where(x => x.Locale == "fa" && x.Slug == LandingPageDevelopmentSeed.CampaignSlug && x.Status == StoreLandingPageStatus.Published)
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

/// <summary>اعمال دانه با زمینهٔ فروشگاه توسعه.</summary>
internal static class StoreMenuDevelopmentSeedHost
{
    /// <summary>دانه را روی Host توسعه اجرا می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
        {
            return;
        }

        var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(new CommerceContext(
            new EditionContext(registry.Edition, registry.DeploymentId),
            new TenantContext(
                tenant.TenantId,
                tenant.Status,
                tenant.ConnectionReference,
                tenant.DisplayName,
                tenant.ThemeReference,
                tenant.DefaultMarketReference,
                tenant.Hosts[0],
                tenant.PrimaryDomain),
            tenant.ConnectionReference,
            "menu-dev-seed"));
        await StoreMenuDevelopmentSeed.ApplyAsync(provider);
    }
}
