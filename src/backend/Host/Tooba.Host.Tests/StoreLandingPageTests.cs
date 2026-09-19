using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T010 — قرارداد صفحهٔ Landing.</summary>
public sealed class StoreLandingPageTests
{
    [Fact]
    public void Reserved_and_invalid_slugs_are_rejected()
    {
        Assert.True(StoreLandingPageSlug.IsReserved("cart"));
        Assert.True(StoreLandingPageSlug.IsReserved("admin"));
        Assert.Equal("summer-sale", StoreLandingPageSlug.Normalize("Summer Sale"));
        var error = Assert.Throws<PlatformHttpException>(() =>
            StoreLandingPage.Create("fa", "cart", "سبد", null, null, DateTimeOffset.UtcNow));
        Assert.Equal("landing.slug.reserved", error.ErrorCode);
    }

    [Fact]
    public async Task Duplicate_slug_same_locale_is_rejected()
    {
        var composer = CreateComposer(out _);
        await composer.CreateAsync(new StoreLandingPageWriteRequest("تابستان", "summer-sale", "fa", null, null, null), CancellationToken.None);
        var error = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.CreateAsync(new StoreLandingPageWriteRequest("دیگر", "summer-sale", "fa", null, null, null), CancellationToken.None));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal("landing.slug.duplicate", error.ErrorCode);
    }

    [Fact]
    public async Task Draft_is_not_public_published_is()
    {
        var composer = CreateComposer(out _);
        var created = await composer.CreateAsync(new StoreLandingPageWriteRequest("تابستان", "summer-sale", "fa", "SEO", "desc", null), CancellationToken.None);
        Assert.Null(await composer.ResolvePublicAsync("fa", "summer-sale", CancellationToken.None));
        await composer.SetStatusAsync(created.PageId, "Published", CancellationToken.None);
        var published = await composer.ResolvePublicAsync("fa", "summer-sale", CancellationToken.None);
        Assert.NotNull(published);
        Assert.Equal("summer-sale", published!.Slug);
    }

    [Fact]
    public async Task Home_selection_requires_same_store_published_page()
    {
        var composer = CreateComposer(out _);
        var foreign = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var missing = await Assert.ThrowsAsync<PlatformHttpException>(() => composer.SetHomeAsync(foreign, CancellationToken.None));
        Assert.Equal("landing.page.missing", missing.ErrorCode);

        var draft = await composer.CreateAsync(new StoreLandingPageWriteRequest("خانه موقت", "campaign-home", "fa", null, null, null), CancellationToken.None);
        var ineligible = await Assert.ThrowsAsync<PlatformHttpException>(() => composer.SetHomeAsync(draft.PageId, CancellationToken.None));
        Assert.Equal("landing.home.ineligible", ineligible.ErrorCode);

        await composer.SetStatusAsync(draft.PageId, "Published", CancellationToken.None);
        var selected = await composer.SetHomeAsync(draft.PageId, CancellationToken.None);
        Assert.Equal(draft.PageId, selected.HomePageId);
        Assert.False(selected.UsesCanonicalHome);

        var cleared = await composer.SetHomeAsync(null, CancellationToken.None);
        Assert.Null(cleared.HomePageId);
        Assert.True(cleared.UsesCanonicalHome);
    }

    [Fact]
    public async Task Reserved_slug_does_not_resolve_publicly()
    {
        var composer = CreateComposer(out _);
        Assert.Null(await composer.ResolvePublicAsync("fa", "cart", CancellationToken.None));
        Assert.Null(await composer.ResolvePublicAsync("fa", "products", CancellationToken.None));
    }

    [Fact]
    public async Task Store_catalogs_do_not_share_pages()
    {
        var alpha = CreateComposer(out _);
        var beta = CreateComposer(out _);
        var page = await alpha.CreateAsync(new StoreLandingPageWriteRequest("آلفا", "alpha-only", "fa", null, null, null), CancellationToken.None);
        await alpha.SetStatusAsync(page.PageId, "Published", CancellationToken.None);
        Assert.Null(await beta.ResolvePublicAsync("fa", "alpha-only", CancellationToken.None));
        var missing = await Assert.ThrowsAsync<PlatformHttpException>(() => beta.SetHomeAsync(page.PageId, CancellationToken.None));
        Assert.Equal("landing.page.missing", missing.ErrorCode);
    }

    private static StoreLandingPageComposer CreateComposer(out CatalogDbContext catalog)
    {
        catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var context = OutboxTestContextFactory.SingleStore("store-a", "conn-a");
        return new StoreLandingPageComposer(catalog, new FixedCommerce(context), new MemoryCache(new MemoryCacheOptions()), new EmptyMerchandisingCampaignQuery());
    }

    private sealed class FixedCommerce : ICurrentCommerceContext
    {
        public FixedCommerce(CommerceContext current) => Current = current;

        public CommerceContext? Current { get; }
    }
}
