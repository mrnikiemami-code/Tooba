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
        Assert.Equal("classic", view.ProductCardSkin);
        Assert.Equal("Neutral", view.BackgroundStyle);
        Assert.InRange(view.Presets.Count, 6, 8);
        Assert.Equal(4, view.Skins.Count);
        Assert.Contains(view.Presets, item => item.Key == "tooba-blue");
        Assert.Contains(view.Skins, item => item.Key == "classic");
    }

    [Fact]
    public async Task Valid_palette_updates_and_preserves_theme_mode()
    {
        var composer = CreateComposer(out var catalog);
        var view = await composer.SaveAsync("forest-green", CancellationToken.None);
        Assert.Equal("forest-green", view.PaletteKey);
        Assert.Equal("LightOnly", view.ThemeMode);
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        Assert.Equal("forest-green", row.PaletteKey);
        Assert.Equal(StoreAppearanceThemeMode.LightOnly, row.ThemeMode);
    }

    [Fact]
    public async Task Valid_theme_mode_updates_and_preserves_palette()
    {
        var composer = CreateComposer(out var catalog);
        await composer.SaveAsync("forest-green", CancellationToken.None);
        var view = await composer.SaveAsync("forest-green", "DarkOnly", CancellationToken.None);
        Assert.Equal("forest-green", view.PaletteKey);
        Assert.Equal("DarkOnly", view.ThemeMode);
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        Assert.Equal(StoreAppearanceThemeMode.DarkOnly, row.ThemeMode);
    }

    [Fact]
    public async Task Invalid_theme_mode_is_rejected()
    {
        var composer = CreateComposer(out var catalog);
        var error = await Assert.ThrowsAsync<PlatformHttpException>(() => composer.SaveAsync("tooba-blue", "Sepia", CancellationToken.None));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("appearance.theme.invalid", error.ErrorCode);
        Assert.Empty(catalog.StoreAppearanceSettings);
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
        Assert.Contains("body.ThemeMode", source, StringComparison.Ordinal);
        Assert.Contains("body.ProductCardSkin", source, StringComparison.Ordinal);
        Assert.Contains("body.BackgroundStyle", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Valid_skin_updates_and_preserves_palette_and_theme()
    {
        var composer = CreateComposer(out var catalog);
        await composer.SaveAsync("forest-green", "DarkOnly", CancellationToken.None);
        var view = await composer.SaveAsync("forest-green", "DarkOnly", "clean", CancellationToken.None);
        Assert.Equal("forest-green", view.PaletteKey);
        Assert.Equal("DarkOnly", view.ThemeMode);
        Assert.Equal("clean", view.ProductCardSkin);
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        Assert.Equal("clean", row.ProductCardSkin);
    }

    [Fact]
    public async Task Missing_skin_preserves_existing_or_classic()
    {
        var composer = CreateComposer(out var catalog);
        var first = await composer.SaveAsync("tooba-blue", "LightOnly", CancellationToken.None);
        Assert.Equal("classic", first.ProductCardSkin);
        var second = await composer.SaveAsync("tooba-blue", "LightOnly", null, CancellationToken.None);
        Assert.Equal("classic", second.ProductCardSkin);
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        Assert.Equal("classic", row.ProductCardSkin);
    }

    [Fact]
    public async Task Valid_background_style_updates_and_preserves_palette_theme_and_skin()
    {
        var composer = CreateComposer(out var catalog);
        await composer.SaveAsync("forest-green", "DarkOnly", "clean", CancellationToken.None);
        var view = await composer.SaveAsync("forest-green", "DarkOnly", "clean", "PaletteTint", CancellationToken.None);
        Assert.Equal("forest-green", view.PaletteKey);
        Assert.Equal("DarkOnly", view.ThemeMode);
        Assert.Equal("clean", view.ProductCardSkin);
        Assert.Equal("PaletteTint", view.BackgroundStyle);
        Assert.Equal("236 244 238", view.Tint.PageBackgroundRgb);
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        Assert.Equal(StoreAppearanceBackgroundStyle.PaletteTint, row.BackgroundStyle);
    }

    [Fact]
    public async Task Missing_background_style_preserves_existing_or_neutral()
    {
        var composer = CreateComposer(out var catalog);
        var first = await composer.SaveAsync("tooba-blue", "LightOnly", "classic", CancellationToken.None);
        Assert.Equal("Neutral", first.BackgroundStyle);
        var second = await composer.SaveAsync("tooba-blue", "LightOnly", "classic", null, CancellationToken.None);
        Assert.Equal("Neutral", second.BackgroundStyle);
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        Assert.Equal(StoreAppearanceBackgroundStyle.Neutral, row.BackgroundStyle);
    }

    [Fact]
    public async Task Invalid_background_style_is_rejected()
    {
        var composer = CreateComposer(out var catalog);
        var error = await Assert.ThrowsAsync<PlatformHttpException>(
            () => composer.SaveAsync("tooba-blue", "LightOnly", "classic", "custom-wash", CancellationToken.None));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("appearance.background.invalid", error.ErrorCode);
        Assert.Empty(catalog.StoreAppearanceSettings);
    }

    [Fact]
    public async Task Missing_row_projects_neutral_background()
    {
        await using var catalog = CreateCatalog();
        var projector = CreateProjector(catalog, OutboxTestContextFactory.SingleStore("store-a", "conn-a"), new MemoryCache(new MemoryCacheOptions()));
        var projection = await projector.GetEffectiveAsync(CancellationToken.None);
        Assert.Equal("Neutral", projection.BackgroundStyle);
        Assert.Equal("236 241 250", projection.PageBackgroundRgb);
        Assert.NotEqual(projection.PrimaryRgb, projection.PageBackgroundRgb);
    }

    [Fact]
    public async Task Invalid_skin_is_rejected()
    {
        var composer = CreateComposer(out var catalog);
        var error = await Assert.ThrowsAsync<PlatformHttpException>(
            () => composer.SaveAsync("tooba-blue", "LightOnly", "custom-html", CancellationToken.None));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("appearance.skin.invalid", error.ErrorCode);
        Assert.Empty(catalog.StoreAppearanceSettings);
    }

    [Fact]
    public async Task Unknown_persisted_skin_reads_as_classic()
    {
        await using var catalog = CreateCatalog();
        catalog.StoreAppearanceSettings.Add(StoreAppearanceSettings.CreateDefault(DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();
        var row = await catalog.StoreAppearanceSettings.SingleAsync();
        typeof(StoreAppearanceSettings).GetProperty(nameof(StoreAppearanceSettings.ProductCardSkin))!
            .SetValue(row, "legacy-skin");
        await catalog.SaveChangesAsync();
        var projector = CreateProjector(catalog, OutboxTestContextFactory.SingleStore("store-a", "conn-a"), new MemoryCache(new MemoryCacheOptions()));
        var projection = await projector.GetEffectiveAsync(CancellationToken.None);
        Assert.Equal("classic", projection.ProductCardSkin);
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
