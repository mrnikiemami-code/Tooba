using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Settings.StoreAppearance.Models;
using Tooba.Catalog.Application.Settings.StoreAppearance.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.StoreAppearance;

/// <summary>
/// ظاهر Store را از Catalog می‌خواند، cache می‌کند و هرگز HTML/CSS/JS اجرا نمی‌کند.
/// </summary>
public sealed class StoreAppearanceProjector : IStoreAppearanceProjector
{
    /// <summary>پیشوند کلید cache ظاهر Store.</summary>
    public const string CacheKeyPrefix = "store-appearance:";

    private readonly CatalogDbContext _catalog;
    private readonly ICurrentCommerceContext _commerce;
    private readonly IMemoryCache _cache;

    /// <summary>پروژکتور ظاهر را به Catalog و زمینهٔ تجارت وصل می‌کند.</summary>
    public StoreAppearanceProjector(
        CatalogDbContext catalog,
        ICurrentCommerceContext commerce,
        IMemoryCache cache)
    {
        _catalog = catalog;
        _commerce = commerce;
        _cache = cache;
    }

    /// <summary>کلید isolation بر اساس Tenant یا اتصال Marketplace.</summary>
    public static string ScopeKey(CommerceContext? context)
    {
        if (context?.Tenant is { } tenant)
        {
            return $"tenant:{tenant.TenantId.Value}";
        }

        var connection = context?.DatabaseConnectionReference.Value ?? "marketplace";
        return $"marketplace:{connection}";
    }

    /// <inheritdoc />
    public Task<StoreAppearanceProjection> GetEffectiveAsync(CancellationToken cancellationToken)
        => GetEffectiveAsync(_commerce.Current, cancellationToken);

    /// <inheritdoc />
    public async Task<StoreAppearanceProjection> GetEffectiveAsync(
        CommerceContext? context,
        CancellationToken cancellationToken)
    {
        var scope = ScopeKey(context);
        if (_cache.TryGetValue(CacheKey(scope), out StoreAppearanceProjection? cached) && cached is not null)
        {
            if (!string.Equals(cached.StoreScope, scope, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("store.appearance.isolation_violation");
            }

            return cached;
        }

        var row = await _catalog.StoreAppearanceSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        var known = StoreAppearancePaletteRegistry.IsKnown(row?.PaletteKey);
        var key = StoreAppearancePaletteRegistry.ResolveKey(row?.PaletteKey);
        var tokens = StoreAppearancePaletteRegistry.ResolveTokens(key);
        var tint = StoreAppearancePaletteRegistry.ResolveTint(key);
        var projection = new StoreAppearanceProjection(
            scope,
            key,
            known || row is null,
            StoreAppearanceSettings.NormalizeThemeMode(row?.ThemeMode ?? StoreAppearanceThemeMode.Light).ToString(),
            StoreAppearanceProductCardSkinRegistry.ResolveKey(row?.ProductCardSkin),
            StoreAppearanceSettings.NormalizeBackgroundStyle(row?.BackgroundStyle ?? StoreAppearanceBackgroundStyle.Neutral).ToString(),
            tokens.PrimaryRgb,
            tokens.PrimaryStrongRgb,
            tokens.OnPrimaryRgb,
            tokens.FocusRgb,
            tokens.PrimaryOnDarkRgb,
            tint.PageBackgroundRgb,
            tint.SectionBackgroundRgb,
            tint.PageBackgroundDarkRgb,
            tint.SectionBackgroundDarkRgb,
            tint.SectionAlternateRgb,
            tint.SectionAccentRgb,
            tint.SectionAlternateDarkRgb,
            tint.SectionAccentDarkRgb,
            row?.UpdatedAt ?? DateTimeOffset.UnixEpoch);

        _cache.Set(CacheKey(scope), projection, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        });
        return projection;
    }

    /// <inheritdoc />
    public void Invalidate(CommerceContext? context)
        => _cache.Remove(CacheKey(ScopeKey(context)));

    private static string CacheKey(string scope) => CacheKeyPrefix + scope;
}
