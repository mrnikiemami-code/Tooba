using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>دانهٔ توسعهٔ idempotent برای صفحات دموی Composer.</summary>
internal static class LandingPageDevelopmentSeed
{
    internal const string PublishedSlug = "landing-demo";
    internal const string DraftSlug = "landing-demo-draft";

    /// <summary>صفحات دمو را اگر نیستند می‌سازد؛ صفحات موجود را بازنویسی نمی‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<CatalogDbContext>();
        var composer = provider.GetRequiredService<StoreLandingPageComposer>();
        await EnsurePageAsync(
            catalog,
            composer,
            PublishedSlug,
            "صفحهٔ دموی منتشرشده",
            "صفحهٔ فرود دمو",
            "نمونهٔ منتشرشده برای بررسی Composer و رندر ویترین",
            publish: true,
            includeCatalogSections: true,
            cancellationToken);
        await EnsurePageAsync(
            catalog,
            composer,
            DraftSlug,
            "صفحهٔ دموی پیش‌نویس",
            "پیش‌نویس دمو",
            "فقط Admin می‌تواند این پیش‌نویس را ببیند",
            publish: false,
            includeCatalogSections: false,
            cancellationToken);
    }

    private static async Task EnsurePageAsync(
        CatalogDbContext catalog,
        StoreLandingPageComposer composer,
        string slug,
        string title,
        string seoTitle,
        string seoDescription,
        bool publish,
        bool includeCatalogSections,
        CancellationToken cancellationToken)
    {
        if (await catalog.StoreLandingPages.AnyAsync(x => x.Locale == "fa" && x.Slug == slug, cancellationToken))
        {
            return;
        }

        var page = await composer.CreateAsync(
            new StoreLandingPageWriteRequest(title, slug, "fa", seoTitle, seoDescription, null),
            cancellationToken);

        await composer.AddSectionAsync(
            page.PageId,
            new StoreLandingPageSectionWriteRequest(
                StoreLandingPageSectionRegistry.Hero,
                JsonSerializer.Serialize(new { title, subtitle = seoDescription, href = "/products" }),
                null,
                true),
            cancellationToken);

        if (includeCatalogSections)
        {
            var productIds = await catalog.Products.AsNoTracking()
                .Where(x => x.Status == CatalogPublicationStatus.Published)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(6)
                .Select(x => x.ProductId)
                .ToListAsync(cancellationToken);
            var categoryId = await catalog.Categories.AsNoTracking()
                .Select(x => (Guid?)x.CategoryId)
                .FirstOrDefaultAsync(cancellationToken);
            var brandId = await catalog.Brands.AsNoTracking()
                .Select(x => (Guid?)x.BrandId)
                .FirstOrDefaultAsync(cancellationToken);

            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.ProductCollection,
                    JsonSerializer.Serialize(new { title = "تازه‌های فروشگاه", source = "Newest", take = 8 }),
                    null,
                    true),
                cancellationToken);

            if (productIds.Count > 0)
            {
                await composer.AddSectionAsync(
                    page.PageId,
                    new StoreLandingPageSectionWriteRequest(
                        StoreLandingPageSectionRegistry.ProductCollection,
                        JsonSerializer.Serialize(new { title = "انتخاب سردبیر", source = "Manual", take = productIds.Count, productIds }),
                        null,
                        true),
                    cancellationToken);
            }

            if (categoryId is { } cid)
            {
                await composer.AddSectionAsync(
                    page.PageId,
                    new StoreLandingPageSectionWriteRequest(
                        StoreLandingPageSectionRegistry.CategoryGrid,
                        JsonSerializer.Serialize(new { title = "دسته‌ها", categoryIds = new[] { cid } }),
                        null,
                        true),
                    cancellationToken);
            }

            if (brandId is { } bid)
            {
                await composer.AddSectionAsync(
                    page.PageId,
                    new StoreLandingPageSectionWriteRequest(
                        StoreLandingPageSectionRegistry.BrandStrip,
                        JsonSerializer.Serialize(new { title = "برندها", brandIds = new[] { bid } }),
                        null,
                        true),
                    cancellationToken);
            }

            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.PromoBanner,
                    JsonSerializer.Serialize(new { title = "پیشنهاد ویژه", href = "/offers" }),
                    null,
                    true),
                cancellationToken);
            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.ArticleList,
                    JsonSerializer.Serialize(new { title = "آخرین مطالب", source = "Latest", take = 4 }),
                    null,
                    true),
                cancellationToken);
            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.Reviews,
                    JsonSerializer.Serialize(new { title = "نظر خریداران" }),
                    null,
                    true),
                cancellationToken);
        }

        await composer.AddSectionAsync(
            page.PageId,
            new StoreLandingPageSectionWriteRequest(
                StoreLandingPageSectionRegistry.RichText,
                JsonSerializer.Serialize(new { title = "دربارهٔ این صفحه", text = seoDescription }),
                null,
                true),
            cancellationToken);

        if (publish)
        {
            await composer.SetStatusAsync(page.PageId, "Published", cancellationToken);
        }
    }
}

/// <summary>اعمال دانه روی tenant توسعه با CommerceContext.</summary>
internal static class LandingPageDevelopmentSeedHost
{
    /// <summary>scope و tenant آلفا را برای دانهٔ Landing آماده می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider root)
    {
        await using var scope = root.CreateAsyncScope();
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
            "landing-dev-seed"));
        await LandingPageDevelopmentSeed.ApplyAsync(provider);
    }
}
