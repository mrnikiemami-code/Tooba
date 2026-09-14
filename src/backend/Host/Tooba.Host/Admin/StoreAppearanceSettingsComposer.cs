using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Storefront;

namespace Tooba.Host.Admin;

/// <summary>ذخیره PaletteKey تأییدشده و باطل‌کردن cache همان Store.</summary>
public sealed class StoreAppearanceSettingsComposer
{
    private readonly CatalogDbContext _catalog;
    private readonly ICurrentCommerceContext _commerce;
    private readonly StoreAppearanceProjector _projector;

    /// <summary>نویسنده ظاهر Store را به Catalog و پروژکتور وصل می‌کند.</summary>
    public StoreAppearanceSettingsComposer(
        CatalogDbContext catalog,
        ICurrentCommerceContext commerce,
        StoreAppearanceProjector projector)
    {
        _catalog = catalog;
        _commerce = commerce;
        _projector = projector;
    }

    /// <summary>ظاهر مؤثر و فهرست پالت‌های مجاز را برمی‌گرداند.</summary>
    public async Task<StoreAppearanceAdminView> GetAsync(CancellationToken cancellationToken)
    {
        var current = await _projector.GetEffectiveAsync(cancellationToken);
        return ToView(current);
    }

    /// <summary>PaletteKey مجاز را می‌نویسد؛ ThemeMode در صورت نبود حفظ می‌شود.</summary>
    public Task<StoreAppearanceAdminView> SaveAsync(string? paletteKey, CancellationToken cancellationToken)
        => SaveAsync(paletteKey, themeMode: null, cancellationToken);

    /// <summary>PaletteKey و ThemeMode را اتمیک می‌نویسد.</summary>
    public Task<StoreAppearanceAdminView> SaveAsync(string? paletteKey, string? themeMode, CancellationToken cancellationToken)
        => SaveAsync(paletteKey, themeMode, productCardSkin: null, cancellationToken);

    /// <summary>PaletteKey و ThemeMode و پوستهٔ کارت را اتمیک می‌نویسد.</summary>
    public async Task<StoreAppearanceAdminView> SaveAsync(string? paletteKey, string? themeMode, string? productCardSkin, CancellationToken cancellationToken)
    {
        if (!StoreAppearancePaletteRegistry.IsKnown(paletteKey))
        {
            throw new PlatformHttpException(400, "پالت انتخاب‌شده مجاز نیست.", "appearance.palette.invalid");
        }

        StoreAppearanceThemeMode? parsedTheme = null;
        if (themeMode is not null)
        {
            if (!StoreAppearanceSettings.TryParseThemeMode(themeMode, out var parsed))
            {
                throw new PlatformHttpException(400, "حالت تم انتخاب‌شده مجاز نیست.", "appearance.theme.invalid");
            }

            parsedTheme = parsed;
        }

        string? canonicalSkin = null;
        if (productCardSkin is not null)
        {
            if (!StoreAppearanceProductCardSkinRegistry.IsKnown(productCardSkin))
            {
                throw new PlatformHttpException(400, "پوستهٔ کارت انتخاب‌شده مجاز نیست.", "appearance.skin.invalid");
            }

            canonicalSkin = StoreAppearanceProductCardSkinRegistry.ResolveKey(productCardSkin);
        }

        var canonical = StoreAppearancePaletteRegistry.ResolveKey(paletteKey);
        var now = DateTimeOffset.UtcNow;
        var row = await _catalog.StoreAppearanceSettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (row is null)
        {
            row = StoreAppearanceSettings.CreateDefault(now);
            _catalog.StoreAppearanceSettings.Add(row);
        }

        row.Replace(canonical, parsedTheme ?? row.ThemeMode, canonicalSkin, now);
        await _catalog.SaveChangesAsync(cancellationToken);
        _projector.Invalidate(_commerce.Current);
        return ToView(await _projector.GetEffectiveAsync(cancellationToken));
    }

    private static StoreAppearanceAdminView ToView(StoreAppearanceProjection current) =>
        new(
            current.StoreScope,
            current.PaletteKey,
            current.PaletteKeyWasKnown,
            current.ThemeMode,
            new StoreAppearanceTokenView(
                current.PrimaryRgb,
                current.PrimaryStrongRgb,
                current.OnPrimaryRgb,
                current.FocusRgb,
                current.PrimaryOnDarkRgb),
            current.ProductCardSkin,
            StoreAppearancePaletteRegistry.All
                .Select(item => new StoreAppearancePresetView(
                    item.Key,
                    item.NameFa,
                    item.NameEn,
                    new StoreAppearanceTokenView(
                        item.Tokens.PrimaryRgb,
                        item.Tokens.PrimaryStrongRgb,
                        item.Tokens.OnPrimaryRgb,
                        item.Tokens.FocusRgb,
                        item.Tokens.PrimaryOnDarkRgb)))
                .ToArray(),
            StoreAppearanceProductCardSkinRegistry.All
                .Select(item => new StoreAppearanceSkinView(item.Key, item.NameFa, item.NameEn))
                .ToArray());
}

/// <summary>نمایه Admin ظاهر Store.</summary>
public sealed record StoreAppearanceAdminView(
    string StoreScope,
    string PaletteKey,
    bool PaletteKeyWasKnown,
    string ThemeMode,
    StoreAppearanceTokenView Tokens,
    string ProductCardSkin,
    IReadOnlyList<StoreAppearancePresetView> Presets,
    IReadOnlyList<StoreAppearanceSkinView> Skins);

/// <summary>یک پوستهٔ curated برای کارت انتخاب.</summary>
public sealed record StoreAppearanceSkinView(
    string Key,
    string NameFa,
    string NameEn);

/// <summary>یک پالت curated برای کارت انتخاب.</summary>
public sealed record StoreAppearancePresetView(
    string Key,
    string NameFa,
    string NameEn,
    StoreAppearanceTokenView Tokens);

/// <summary>توکن برند بدون رنگ وضعیت.</summary>
public sealed record StoreAppearanceTokenView(
    string PrimaryRgb,
    string PrimaryStrongRgb,
    string OnPrimaryRgb,
    string FocusRgb,
    string PrimaryOnDarkRgb);

/// <summary>بدنه ذخیره ظاهر؛ PaletteKey الزامی و ThemeMode اختیاری.</summary>
public sealed record StoreAppearanceSettingsWriteRequest(string? PaletteKey, string? ThemeMode = null, string? ProductCardSkin = null);
