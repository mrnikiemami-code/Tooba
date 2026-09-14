using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Host.Storefront;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T006 — ذخیره پالت Admin، رد کلید نامعتبر، isolation و cache.</summary>
public sealed class StoreAppearanceAdminTests
{
    [Fact]
    public async Task Default_get_is_tooba_blue()
    {
        var composer = CreateComposer(out _);
        var view = await composer.GetAsync(CancellationToken.None);
        Assert.Equal("tooba-blue", view.PaletteKey);
        Assert.True(view.PaletteKeyWasKnown);
        Assert.InRange(view.Presets.Count, 6, 8);
        Assert.Contains(view.Presets, item => item.Key == "tooba-blue");
    }

    [Fact]
    public async Task Valid_palette_updates_and_preserves_theme_mode()
    {
        var composer = CreateComposer(out var catalog);
        var view = await composer.SaveAsync("forest-green", CancellationToken.None);
        Assert.Equal("forest-green", view.PaletteKey);
        Assert.Equal("Light", view.ThemeMode);
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        Assert.Equal("forest-green", row.PaletteKey);
        Assert.Equal(StoreAppearanceThemeMode.Light, row.ThemeMode);
    }

    [Fact]
    public async Task Invalid_palette_key_is_rejected()
    {
        var composer = CreateComposer(out var catalog);
        var error = await Assert.ThrowsAsync<PlatformHttpException>(() => composer.SaveAsync("#ff00aa", CancellationToken.None));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("appearance.palette.invalid", error.ErrorCode);
        Assert.Empty(catalog.StoreAppearanceSettings);
    }

    [Fact]
    public async Task Unknown_persisted_key_reads_as_tooba_blue()
    {
        await using var catalog = CreateCatalog();
        catalog.StoreAppearanceSettings.Add(StoreAppearanceSettings.CreateDefault(DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        typeof(StoreAppearanceSettings).GetProperty(nameof(StoreAppearanceSettings.PaletteKey))!
            .SetValue(row, "retired-preset");
        await catalog.SaveChangesAsync();
        var projector = CreateProjector(catalog, OutboxTestContextFactory.SingleStore("store-a", "conn-a"), new MemoryCache(new MemoryCacheOptions()));
        var projection = await projector.GetEffectiveAsync(CancellationToken.None);
        Assert.Equal("tooba-blue", projection.PaletteKey);
        Assert.False(projection.PaletteKeyWasKnown);
    }

    [Fact]
    public async Task Save_invalidates_only_changed_store_cache()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        await using var catalogA = CreateCatalog();
        await using var catalogB = CreateCatalog();
        var storeA = OutboxTestContextFactory.SingleStore("store-a", "conn-a");
        var storeB = OutboxTestContextFactory.SingleStore("store-b", "conn-b");
        var projectorA = CreateProjector(catalogA, storeA, cache);
        var projectorB = CreateProjector(catalogB, storeB, cache);
        catalogB.StoreAppearanceSettings.Add(StoreAppearanceSettings.CreateDefault(DateTimeOffset.UtcNow));
        await catalogB.SaveChangesAsync();
        await projectorA.GetEffectiveAsync(storeA, CancellationToken.None);
        await projectorB.GetEffectiveAsync(storeB, CancellationToken.None);
        var composerA = new StoreAppearanceSettingsComposer(catalogA, new FixedCommerce(storeA), projectorA);
        await composerA.SaveAsync("amber-gold", CancellationToken.None);

        Assert.True(cache.TryGetValue(StoreAppearanceProjector.CacheKeyPrefix + "tenant:store-b", out StoreAppearanceProjection? cachedB));
        Assert.Equal("tooba-blue", cachedB!.PaletteKey);
        var afterA = await projectorA.GetEffectiveAsync(storeA, CancellationToken.None);
        var afterB = await projectorB.GetEffectiveAsync(storeB, CancellationToken.None);
        Assert.Equal("amber-gold", afterA.PaletteKey);
        Assert.Equal("tooba-blue", afterB.PaletteKey);
        Assert.Same(cachedB, afterB);
    }

    [Fact]
    public void Admin_endpoints_reuse_existing_authorization()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin/StoreAppearanceSettingsEndpoints.cs"));
        Assert.Contains("/v1/admin/settings/appearance", source, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ThemeMode", source, StringComparison.Ordinal);
    }

    private static StoreAppearanceSettingsComposer CreateComposer(out CatalogDbContext catalog)
    {
        catalog = CreateCatalog();
        var context = OutboxTestContextFactory.SingleStore("store-a", "conn-a");
        var projector = CreateProjector(catalog, context, new MemoryCache(new MemoryCacheOptions()));
        return new StoreAppearanceSettingsComposer(catalog, new FixedCommerce(context), projector);
    }

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private static StoreAppearanceProjector CreateProjector(
        CatalogDbContext catalog,
        CommerceContext context,
        IMemoryCache cache)
        => new(catalog, new FixedCommerce(context), cache);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs/architecture/TOOBA-LOCKS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class FixedCommerce : ICurrentCommerceContext
    {
        public FixedCommerce(CommerceContext current) => Current = current;

        public CommerceContext? Current { get; }
    }
}
