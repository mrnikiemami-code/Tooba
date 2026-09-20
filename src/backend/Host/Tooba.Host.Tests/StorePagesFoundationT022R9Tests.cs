using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T022-R9 — Store Pages foundation (type, Home invariant, SEO, landing route).</summary>
public sealed class StorePagesFoundationT022R9Tests
{
    [Fact]
    public void Existing_create_defaults_to_Landing_page_type()
    {
        var page = StoreLandingPage.Create("fa", "summer-r9", "تابستان", null, null, DateTimeOffset.UtcNow);
        Assert.Equal(StorePageType.Landing, page.PageType);
        Assert.True(page.RobotsIndex);
        Assert.True(page.RobotsFollow);
    }

    [Fact]
    public void Create_Home_page_type_is_explicit()
    {
        var page = StoreLandingPage.Create("fa", "home-draft", "خانه", null, null, DateTimeOffset.UtcNow, pageType: "Home");
        Assert.Equal(StorePageType.Home, page.PageType);
    }

    [Fact]
    public void Landing_slug_is_reserved()
    {
        Assert.True(StoreLandingPageSlug.IsReserved("landing"));
        var error = Assert.Throws<PlatformHttpException>(() =>
            StoreLandingPage.Create("fa", "landing", "مسیر", null, null, DateTimeOffset.UtcNow));
        Assert.Equal("landing.slug.reserved", error.ErrorCode);
    }

    [Fact]
    public async Task Public_resolve_serves_only_Landing_type()
    {
        var composer = CreateComposer(out var catalog);
        var page = await composer.CreateAsync(
            new StoreLandingPageWriteRequest("کمپین", "campaign-r9", "fa", null, null, null, "Landing"),
            CancellationToken.None);
        await composer.SetStatusAsync(page.PageId, "Published", CancellationToken.None);
        Assert.NotNull(await composer.ResolvePublicAsync("fa", "campaign-r9", CancellationToken.None));

        await composer.SetHomeAsync(page.PageId, CancellationToken.None);
        catalog.ChangeTracker.Clear();
        var persisted = await catalog.StoreLandingPages.AsNoTracking().SingleAsync(x => x.PageId == page.PageId);
        Assert.Equal(StorePageType.Home, persisted.PageType);
        Assert.Null(await composer.ResolvePublicAsync("fa", "campaign-r9", CancellationToken.None));
    }

    [Fact]
    public async Task Atomic_home_replacement_demotes_previous_home()
    {
        var composer = CreateComposer(out var catalog);
        var first = await composer.CreateAsync(
            new StoreLandingPageWriteRequest("خانه ۱", "home-one", "fa", null, null, null),
            CancellationToken.None);
        var second = await composer.CreateAsync(
            new StoreLandingPageWriteRequest("خانه ۲", "home-two", "fa", null, null, null),
            CancellationToken.None);
        await composer.SetStatusAsync(first.PageId, "Published", CancellationToken.None);
        await composer.SetStatusAsync(second.PageId, "Published", CancellationToken.None);

        await composer.SetHomeAsync(first.PageId, CancellationToken.None);
        await composer.SetHomeAsync(second.PageId, CancellationToken.None);

        var firstEntity = await catalog.StoreLandingPages.SingleAsync(x => x.PageId == first.PageId);
        var secondEntity = await catalog.StoreLandingPages.SingleAsync(x => x.PageId == second.PageId);
        Assert.Equal(StorePageType.Landing, firstEntity.PageType);
        Assert.Equal(StorePageType.Home, secondEntity.PageType);

        var selection = await composer.GetHomeSelectionAsync(CancellationToken.None);
        Assert.Equal(second.PageId, selection.HomePageId);
        Assert.False(selection.UsesCanonicalHome);
    }

    [Fact]
    public async Task Restore_default_home_does_not_delete_catalog_rows()
    {
        var composer = CreateComposer(out var catalog);
        var page = await composer.CreateAsync(
            new StoreLandingPageWriteRequest("خانه", "home-restore", "fa", null, null, null),
            CancellationToken.None);
        await composer.SetStatusAsync(page.PageId, "Published", CancellationToken.None);
        await composer.SetHomeAsync(page.PageId, CancellationToken.None);

        var productCountBefore = await catalog.Products.CountAsync();
        var pageCountBefore = await catalog.StoreLandingPages.CountAsync();

        var restored = await composer.SetHomeAsync(null, CancellationToken.None);
        Assert.Null(restored.HomePageId);
        Assert.True(restored.UsesCanonicalHome);

        Assert.Equal(productCountBefore, await catalog.Products.CountAsync());
        Assert.Equal(pageCountBefore, await catalog.StoreLandingPages.CountAsync());
        var entity = await catalog.StoreLandingPages.SingleAsync(x => x.PageId == page.PageId);
        Assert.Equal(StorePageType.Landing, entity.PageType);
    }

    [Fact]
    public async Task Seo_fields_round_trip_and_noindex_excludes_sitemap()
    {
        var composer = CreateComposer(out _);
        var created = await composer.CreateAsync(
            new StoreLandingPageWriteRequest(
                "سئو",
                "seo-page",
                "fa",
                "SEO Title",
                "SEO Desc",
                null,
                "Landing",
                RobotsIndex: false,
                RobotsFollow: false,
                CanonicalUrl: "https://example.test/landing/seo-page",
                OgTitle: "OG",
                OgDescription: "OG Desc",
                OgImageUrl: "https://cdn.example/og.jpg",
                PrimaryH1: "H1 سفارشی"),
            CancellationToken.None);
        Assert.False(created.RobotsIndex);
        Assert.Equal("H1 سفارشی", created.PrimaryH1);
        Assert.Equal("OG", created.OgTitle);

        await composer.SetStatusAsync(created.PageId, "Published", CancellationToken.None);
        var sitemap = await composer.ListIndexableLandingsAsync(CancellationToken.None);
        Assert.DoesNotContain(sitemap, x => x.Slug == "seo-page");

        var indexed = await composer.CreateAsync(
            new StoreLandingPageWriteRequest("ایندکس", "indexed-page", "fa", null, null, null, "Landing", true, true),
            CancellationToken.None);
        await composer.SetStatusAsync(indexed.PageId, "Published", CancellationToken.None);
        sitemap = await composer.ListIndexableLandingsAsync(CancellationToken.None);
        Assert.Contains(sitemap, x => x.Slug == "indexed-page");
    }

    [Fact]
    public void Primary_h1_falls_back_to_title()
    {
        var page = StoreLandingPage.Create("fa", "h1-page", "عنوان صفحه", null, null, DateTimeOffset.UtcNow);
        Assert.Equal("عنوان صفحه", page.ResolvePrimaryH1());
        page.Update("h1-page", "عنوان صفحه", null, null, DateTimeOffset.UtcNow, primaryH1: "H1");
        Assert.Equal("H1", page.ResolvePrimaryH1());
    }

    private static StoreLandingPageComposer CreateComposer(out CatalogDbContext catalog) =>
        StoreLandingPageComposerTestFactory.Create(out catalog);
}
