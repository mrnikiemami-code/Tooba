using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.StoreLandingPages.Models;
using Tooba.Catalog.Application.StoreLandingPages.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>دانهٔ توسعهٔ idempotent برای صفحات دموی Composer.</summary>
public static class LandingPageDevelopmentSeed
{
    /// <summary>Slug صفحهٔ دموی منتشرشده.</summary>
    public const string PublishedSlug = "landing-demo";

    /// <summary>Slug صفحهٔ دموی کمپین.</summary>
    public const string CampaignSlug = "landing-campaign";

    /// <summary>Slug صفحهٔ دموی پیش‌نویس.</summary>
    public const string DraftSlug = "landing-demo-draft";

    /// <summary>صفحات دمو را اگر نیستند می‌سازد؛ صفحات موجود را بازنویسی نمی‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var catalog = provider.GetRequiredService<CatalogDbContext>();
        var composer = provider.GetRequiredService<IStoreLandingPageWorkspace>();
        await EnsurePageAsync(
            catalog,
            composer,
            PublishedSlug,
            "صفحهٔ دموی منتشرشده",
            "صفحهٔ فرود دمو",
            "نمونهٔ منتشرشده برای بررسی Composer و رندر ویترین",
            publish: true,
            profile: "main",
            cancellationToken);
        await EnsurePageAsync(
            catalog,
            composer,
            CampaignSlug,
            "صفحهٔ دموی کمپین",
            "کمپین فروش",
            "ترکیب متفاوت برای بازرسی بصری کمپین",
            publish: true,
            profile: "campaign",
            cancellationToken);
        await EnsurePageAsync(
            catalog,
            composer,
            DraftSlug,
            "صفحهٔ دموی پیش‌نویس",
            "پیش‌نویس دمو",
            "فقط Admin می‌تواند این پیش‌نویس را ببیند",
            publish: false,
            profile: "draft",
            cancellationToken);
    }

    private static async Task EnsurePageAsync(
        CatalogDbContext catalog,
        IStoreLandingPageWorkspace composer,
        string slug,
        string title,
        string seoTitle,
        string seoDescription,
        bool publish,
        string profile,
        CancellationToken cancellationToken)
    {
        if (await catalog.StoreLandingPages.AnyAsync(x => x.Locale == "fa" && x.Slug == slug, cancellationToken))
        {
            return;
        }

        var page = await composer.CreateAsync(
            new StoreLandingPageWriteRequest(title, slug, "fa", seoTitle, seoDescription, null),
            cancellationToken);

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

        if (profile == "campaign")
        {
            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.PromoBanner,
                    JsonSerializer.Serialize(new { title = "فرصت محدود کمپین", href = "/products" }),
                    null,
                    true),
                cancellationToken);
            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.Hero,
                    JsonSerializer.Serialize(new { title, subtitle = seoDescription, href = "/products" }),
                    null,
                    true),
                cancellationToken);
            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.ProductCollection,
                    JsonSerializer.Serialize(new { title = "کالاهای کمپین", source = "Newest", take = 8 }),
                    null,
                    true),
                cancellationToken);
            if (categoryId is { } campaignCategory)
            {
                await composer.AddSectionAsync(
                    page.PageId,
                    new StoreLandingPageSectionWriteRequest(
                        StoreLandingPageSectionRegistry.CategoryGrid,
                        JsonSerializer.Serialize(new { title = "دسته‌های کمپین", categoryIds = new[] { campaignCategory } }),
                        null,
                        true),
                    cancellationToken);
            }

            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.RichText,
                    JsonSerializer.Serialize(new { title = "توضیح کمپین", text = seoDescription }),
                    null,
                    true),
                cancellationToken);
        }
        else
        {
            await composer.AddSectionAsync(
                page.PageId,
                new StoreLandingPageSectionWriteRequest(
                    StoreLandingPageSectionRegistry.Hero,
                    JsonSerializer.Serialize(new { title, subtitle = seoDescription, href = "/products" }),
                    null,
                    true),
                cancellationToken);

            if (profile == "main")
            {
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
        }

        if (publish)
        {
            await composer.SetStatusAsync(page.PageId, "Published", cancellationToken);
        }
    }
}
