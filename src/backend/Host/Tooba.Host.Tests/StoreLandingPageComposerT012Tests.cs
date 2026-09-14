using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T012 — پیش‌نمایش، انتشار و انتخاب خانه.</summary>
public sealed class StoreLandingPageComposerT012Tests
{
    [Fact]
    public async Task Admin_can_create_page_and_add_valid_section()
    {
        var composer = CreateComposer(out _);
        var page = await composer.CreateAsync(new StoreLandingPageWriteRequest("کمپین", "campaign-lab", "fa", null, null, null), CancellationToken.None);
        var section = await composer.AddSectionAsync(
            page.PageId,
            new StoreLandingPageSectionWriteRequest("Hero", """{"title":"هیرو"}""", null, null),
            CancellationToken.None);
        Assert.Equal("Hero", section.SectionType);
        var listed = await composer.ListSectionsAsync(page.PageId, CancellationToken.None);
        Assert.Single(listed);
    }

    [Fact]
    public async Task Product_collection_newest_saves_and_resolves()
    {
        var composer = CreateComposer(out var db);
        var page = await composer.CreateAsync(new StoreLandingPageWriteRequest("کالا", "goods-lab", "fa", null, null, null), CancellationToken.None);
        SeedPublishedProduct(db, "goods-one");
        await composer.AddSectionAsync(
            page.PageId,
            new StoreLandingPageSectionWriteRequest("ProductCollection", """{"source":"Newest","take":4}""", null, null),
            CancellationToken.None);
        await composer.SetStatusAsync(page.PageId, "Published", CancellationToken.None);
        var published = await composer.ResolvePublicAsync("fa", "goods-lab", CancellationToken.None);
        Assert.NotNull(published);
        var collection = Assert.Single(published!.Sections, x => x.SectionType == "ProductCollection");
        Assert.Contains(collection.Items, x => x.Slug == "goods-one");
    }

    [Fact]
    public async Task Draft_preview_is_authorized_while_public_stays_missing()
    {
        var composer = CreateComposer(out _);
        var draft = await composer.CreateAsync(new StoreLandingPageWriteRequest("پیش‌نویس", "preview-lab", "fa", null, null, null), CancellationToken.None);
        await composer.AddSectionAsync(draft.PageId, new StoreLandingPageSectionWriteRequest("Hero", """{"title":"هیرو"}""", null, null), CancellationToken.None);
        var preview = await composer.ResolvePreviewAsync(draft.PageId, CancellationToken.None);
        Assert.Equal("preview-lab", preview.Slug);
        Assert.Contains(preview.Sections, x => x.SectionType == "Hero");
        Assert.Null(await composer.ResolvePublicAsync("fa", "preview-lab", CancellationToken.None));
    }

    [Fact]
    public async Task Publish_exposes_public_route_and_home_selection_falls_back()
    {
        var composer = CreateComposer(out _);
        var page = await composer.CreateAsync(new StoreLandingPageWriteRequest("خانه کمپین", "home-lab", "fa", null, null, null), CancellationToken.None);
        await composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("RichText", """{"text":"متن"}""", null, null), CancellationToken.None);
        Assert.Null(await composer.ResolvePublicAsync("fa", "home-lab", CancellationToken.None));
        await composer.SetStatusAsync(page.PageId, "Published", CancellationToken.None);
        Assert.NotNull(await composer.ResolvePublicAsync("fa", "home-lab", CancellationToken.None));

        var selected = await composer.SetHomeAsync(page.PageId, CancellationToken.None);
        Assert.False(selected.UsesCanonicalHome);
        Assert.Equal("home-lab", selected.SelectedPage?.Slug);

        await composer.SetStatusAsync(page.PageId, "Draft", CancellationToken.None);
        var afterUnpublish = await composer.GetHomeSelectionAsync(CancellationToken.None);
        Assert.True(afterUnpublish.UsesCanonicalHome);
        Assert.Null(afterUnpublish.SelectedPage);
        Assert.Null(await composer.ResolvePublicAsync("fa", "home-lab", CancellationToken.None));
    }

    [Fact]
    public async Task Disabled_section_is_hidden_from_preview_and_public()
    {
        var composer = CreateComposer(out _);
        var page = await composer.CreateAsync(new StoreLandingPageWriteRequest("خاموش", "off-lab", "fa", null, null, null), CancellationToken.None);
        var hero = await composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("Hero", """{"title":"هیرو"}""", null, null), CancellationToken.None);
        var text = await composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("RichText", """{"text":"متن"}""", null, null), CancellationToken.None);
        await composer.SetSectionEnabledAsync(page.PageId, text.PageSectionId, false, CancellationToken.None);
        var preview = await composer.ResolvePreviewAsync(page.PageId, CancellationToken.None);
        Assert.Equal(new[] { hero.PageSectionId }, preview.Sections.Select(x => x.PageSectionId).ToArray());
        await composer.SetStatusAsync(page.PageId, "Published", CancellationToken.None);
        var published = await composer.ResolvePublicAsync("fa", "off-lab", CancellationToken.None);
        Assert.Equal(new[] { hero.PageSectionId }, published!.Sections.Select(x => x.PageSectionId).ToArray());
    }

    [Fact]
    public async Task Unknown_config_is_rejected()
    {
        var composer = CreateComposer(out _);
        var page = await composer.CreateAsync(new StoreLandingPageWriteRequest("رد", "reject-lab", "fa", null, null, null), CancellationToken.None);
        var error = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.AddSectionAsync(page.PageId, new StoreLandingPageSectionWriteRequest("Hero", """{"title":"a","query":"x"}""", null, null), CancellationToken.None));
        Assert.Equal("landing.section.config.forbidden", error.ErrorCode);
    }

    private static void SeedPublishedProduct(CatalogDbContext catalog, string slug)
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, slug, DateTimeOffset.UtcNow);
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
