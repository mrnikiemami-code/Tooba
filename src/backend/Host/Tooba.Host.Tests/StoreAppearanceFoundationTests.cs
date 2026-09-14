using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Storefront;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T005 — ظاهر Store: پالت پیش‌فرض، کلید ناشناخته، isolation.</summary>
public sealed class StoreAppearanceFoundationTests
{
    [Fact]
    public void Unknown_palette_key_falls_back_to_tooba_blue()
    {
        Assert.Equal("tooba-blue", StoreAppearancePaletteRegistry.ResolveKey(null));
        Assert.Equal("tooba-blue", StoreAppearancePaletteRegistry.ResolveKey("not-a-palette"));
        Assert.False(StoreAppearancePaletteRegistry.IsKnown("not-a-palette"));
        Assert.True(StoreAppearancePaletteRegistry.IsKnown("tooba-blue"));
        Assert.True(StoreAppearancePaletteRegistry.IsKnown("forest-green"));
        Assert.Equal("forest-green", StoreAppearancePaletteRegistry.ResolveKey("forest-green"));
        Assert.InRange(StoreAppearancePaletteRegistry.All.Count, 6, 8);
        Assert.Equal("37 99 235", StoreAppearancePaletteRegistry.ToobaBlue.PrimaryRgb);
        Assert.Equal("29 78 216", StoreAppearancePaletteRegistry.ToobaBlue.PrimaryStrongRgb);
        Assert.Equal("59 115 237", StoreAppearancePaletteRegistry.ToobaBlue.PrimaryOnDarkRgb);
        Assert.Equal("189 91 118", StoreAppearancePaletteRegistry.ResolveTokens("wine-burgundy").PrimaryOnDarkRgb);
    }

    [Fact]
    public async Task Default_row_projects_tooba_blue_and_does_not_touch_status_colors()
    {
        await using var catalog = CreateCatalog();
        catalog.StoreAppearanceSettings.Add(StoreAppearanceSettings.CreateDefault(DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();
        var projector = CreateProjector(catalog, OutboxTestContextFactory.SingleStore("store-a", "conn-a"));

        var projection = await projector.GetEffectiveAsync(CancellationToken.None);

        Assert.Equal("tooba-blue", projection.PaletteKey);
        Assert.True(projection.PaletteKeyWasKnown);
        Assert.Equal("LightOnly", projection.ThemeMode);
        Assert.Equal("37 99 235", projection.PrimaryRgb);
        Assert.Equal("tenant:store-a", projection.StoreScope);
        Assert.DoesNotContain("danger", projection.PaletteKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Store_A_cache_cannot_be_read_as_Store_B()
    {
        await using var catalogA = CreateCatalog();
        catalogA.StoreAppearanceSettings.Add(StoreAppearanceSettings.CreateDefault(DateTimeOffset.UtcNow));
        await catalogA.SaveChangesAsync();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storeA = OutboxTestContextFactory.SingleStore("store-a", "conn-a");
        var storeB = OutboxTestContextFactory.SingleStore("store-b", "conn-b");
        var projectorA = new StoreAppearanceProjector(catalogA, new FixedCommerce(storeA), cache);
        var fromA = await projectorA.GetEffectiveAsync(storeA, CancellationToken.None);

        Assert.Equal("tenant:store-a", fromA.StoreScope);
        Assert.False(cache.TryGetValue(StoreAppearanceProjector.CacheKeyPrefix + "tenant:store-b", out _));
        var projectorB = new StoreAppearanceProjector(catalogA, new FixedCommerce(storeB), cache);
        var fromB = await projectorB.GetEffectiveAsync(storeB, CancellationToken.None);
        Assert.Equal("tenant:store-b", fromB.StoreScope);
        Assert.NotEqual(fromA.StoreScope, fromB.StoreScope);
    }

    [Fact]
    public void Theme_reference_is_not_the_appearance_owner()
    {
        var context = OutboxTestContextFactory.SingleStore("store-a", "conn-a");
        Assert.Null(context.Tenant!.ThemeReference);
        Assert.Equal("tenant:store-a", StoreAppearanceProjector.ScopeKey(context));
    }

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private static StoreAppearanceProjector CreateProjector(CatalogDbContext catalog, CommerceContext context)
        => new(catalog, new FixedCommerce(context), new MemoryCache(new MemoryCacheOptions()));

    private sealed class FixedCommerce : ICurrentCommerceContext
    {
        public FixedCommerce(CommerceContext current) => Current = current;

        public CommerceContext? Current { get; }
    }
}
