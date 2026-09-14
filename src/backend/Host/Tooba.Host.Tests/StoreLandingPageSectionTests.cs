using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T011 — قرارداد بخش Landing.</summary>
public sealed class StoreLandingPageSectionTests
{
    [Fact]
    public async Task Invalid_section_type_and_raw_query_are_rejected()
    {
        var composer = CreateComposer(out _);
        var page = await PublishPageAsync(composer);
        var type = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("CustomHtml", "{}", null, null), CancellationToken.None));
        Assert.Equal("landing.section.type.invalid", type.ErrorCode);

        var query = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("Hero", """{"title":"a","sql":"select 1"}""", null, null), CancellationToken.None));
        Assert.Equal("landing.section.config.forbidden", query.ErrorCode);
    }

    [Fact]
    public async Task Cross_store_page_and_product_refs_are_rejected()
    {
        var alpha = CreateComposer(out var alphaDb);
        var beta = CreateComposer(out _);
        var page = await PublishPageAsync(alpha);
        var missingPage = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            beta.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("Hero", """{"title":"هیرو"}""", null, null), CancellationToken.None));
        Assert.Equal("landing.page.missing", missingPage.ErrorCode);

        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "alpha-item", DateTimeOffset.UtcNow);
        product.Status = CatalogPublicationStatus.Published;
        alphaDb.Products.Add(product);
        await alphaDb.SaveChangesAsync();

        var betaPage = await PublishPageAsync(beta);
        var missingProduct = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            beta.AddSectionAsync(
                betaPage.PageId,
                new StoreLandingPageSectionWriteRequest("ProductCollection", JsonSerializer.Serialize(new { source = "Manual", productIds = new[] { product.ProductId } }), null, null),
                CancellationToken.None));
        Assert.Equal("landing.section.ref.missing", missingProduct.ErrorCode);
    }

    [Fact]
    public async Task Reorder_preserves_ids_and_disabled_is_hidden_publicly()
    {
        var composer = CreateComposer(out var db);
        var page = await PublishPageAsync(composer);
        var hero = await composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("Hero", """{"title":"هیرو"}""", null, null), CancellationToken.None);
        SeedPublishedProduct(db);
        var products = await composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("ProductCollection", """{"source":"Newest","take":4}""", null, null), CancellationToken.None);
        var banner = await composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("PromoBanner", """{"title":"بنر"}""", null, null), CancellationToken.None);

        var published = await composer.ResolvePublicAsync("fa", "section-lab", CancellationToken.None);
        Assert.NotNull(published);
        Assert.Equal(new[] { "Hero", "ProductCollection", "PromoBanner" }, published!.Sections.Select(x => x.SectionType).ToArray());
        Assert.Contains(published.Sections, x => x.SectionType == "ProductCollection" && x.Items.Count == 1);

        await composer.SetSectionEnabledAsync(page.PageId, products.PageSectionId, false, CancellationToken.None);
        var hidden = await composer.ResolvePublicAsync("fa", "section-lab", CancellationToken.None);
        Assert.Equal(new[] { "Hero", "PromoBanner" }, hidden!.Sections.Select(x => x.SectionType).ToArray());

        var reordered = await composer.ReorderSectionsAsync(page.PageId, [banner.PageSectionId, hero.PageSectionId, products.PageSectionId], CancellationToken.None);
        Assert.Equal(new[] { banner.PageSectionId, hero.PageSectionId, products.PageSectionId }, reordered.Select(x => x.PageSectionId).ToArray());
        var after = await composer.ResolvePublicAsync("fa", "section-lab", CancellationToken.None);
        Assert.Equal(new[] { banner.PageSectionId, hero.PageSectionId }, after!.Sections.Select(x => x.PageSectionId).ToArray());
    }

    [Fact]
    public async Task Draft_page_stays_public_404_with_sections()
    {
        var composer = CreateComposer(out _);
        var draft = await composer.CreateAsync(new StoreLandingPageWriteRequest("پیش‌نویس", "draft-lab", "fa", null, null, null), CancellationToken.None);
        await composer.AddSectionAsync(draft.PageId, new StoreLandingPageSectionWriteRequest("Hero", """{"title":"هیرو"}""", null, null), CancellationToken.None);
        Assert.Null(await composer.ResolvePublicAsync("fa", "draft-lab", CancellationToken.None));
    }

    private static async Task<StoreLandingPageAdminView> PublishPageAsync(StoreLandingPageComposer composer)
    {
        var created = await composer.CreateAsync(new StoreLandingPageWriteRequest("آزمایش بخش", "section-lab", "fa", null, null, null), CancellationToken.None);
        return await composer.SetStatusAsync(created.PageId, "Published", CancellationToken.None);
    }

    private static void SeedPublishedProduct(CatalogDbContext catalog)
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "seed-item", DateTimeOffset.UtcNow);
        product.Status = CatalogPublicationStatus.Published;
        catalog.Products.Add(product);
        catalog.SaveChanges();
    }

    private static StoreLandingPageComposer CreateComposer(out CatalogDbContext catalog)
    {
        catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var context = OutboxTestContextFactory.SingleStore("store-a", "conn-a");
        return new StoreLandingPageComposer(catalog, new FixedCommerce(context), new MemoryCache(new MemoryCacheOptions()));
    }

    private sealed class FixedCommerce : ICurrentCommerceContext
    {
        public FixedCommerce(CommerceContext current) => Current = current;

        public CommerceContext? Current { get; }
    }
}
