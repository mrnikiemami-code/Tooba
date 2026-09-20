using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// orchestration موقت نوشتن ظاهر فروشگاه روی Catalog DbContext تا جابه‌جایی BC.
/// </summary>
public sealed class StoreAppearanceSettingsDirectory : IStoreAppearanceSettingsDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>دایرکتوری را به schema catalog وصل می‌کند.</summary>
    public StoreAppearanceSettingsDirectory(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task SaveAsync(StoreAppearanceSettingsWriteModel model, CancellationToken cancellationToken)
    {
        if (!StoreAppearancePaletteRegistry.IsKnown(model.PaletteKey))
        {
            throw new PlatformHttpException(400, "پالت انتخاب‌شده مجاز نیست.", "appearance.palette.invalid");
        }

        StoreAppearanceThemeMode? parsedTheme = null;
        if (model.ThemeMode is not null)
        {
            if (!StoreAppearanceSettings.TryParseThemeMode(model.ThemeMode, out var parsed))
            {
                throw new PlatformHttpException(400, "حالت تم انتخاب‌شده مجاز نیست.", "appearance.theme.invalid");
            }

            parsedTheme = parsed;
        }

        string? canonicalSkin = null;
        if (model.ProductCardSkin is not null)
        {
            if (!StoreAppearanceProductCardSkinRegistry.IsKnown(model.ProductCardSkin))
            {
                throw new PlatformHttpException(400, "پوستهٔ کارت انتخاب‌شده مجاز نیست.", "appearance.skin.invalid");
            }

            canonicalSkin = StoreAppearanceProductCardSkinRegistry.ResolveKey(model.ProductCardSkin);
        }

        StoreAppearanceBackgroundStyle? parsedBackground = null;
        if (model.BackgroundStyle is not null)
        {
            if (!StoreAppearanceSettings.TryParseBackgroundStyle(model.BackgroundStyle, out var parsed))
            {
                throw new PlatformHttpException(400, "پس‌زمینهٔ انتخاب‌شده مجاز نیست.", "appearance.background.invalid");
            }

            parsedBackground = parsed;
        }

        var canonical = StoreAppearancePaletteRegistry.ResolveKey(model.PaletteKey);
        var now = _clock.UtcNow;
        var row = await _catalog.StoreAppearanceSettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreAppearanceSettings.SingletonId, cancellationToken);
        if (row is null)
        {
            row = StoreAppearanceSettings.CreateDefault(now);
            _catalog.StoreAppearanceSettings.Add(row);
        }

        row.Replace(canonical, parsedTheme ?? row.ThemeMode, canonicalSkin, parsedBackground, now);
        await _catalog.SaveChangesAsync(cancellationToken);
    }
}
