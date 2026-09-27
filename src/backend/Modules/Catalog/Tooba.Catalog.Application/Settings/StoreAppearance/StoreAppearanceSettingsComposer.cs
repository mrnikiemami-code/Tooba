using MediatR;
using Tooba.Catalog.Application.Settings.StoreAppearance.Models;
using Tooba.Catalog.Application.Settings.StoreAppearance.Ports;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Settings.StoreAppearance;

/// <summary>خواندن ظاهر Store و ارسال فرمان نوشتن از طریق CQRS.</summary>
public sealed class StoreAppearanceSettingsComposer
{
    private readonly IStoreAppearanceProjector _projector;
    private readonly ISender _sender;

    /// <summary>نویسنده ظاهر Store را به پروژکتور و ISender وصل می‌کند.</summary>
    public StoreAppearanceSettingsComposer(
        IStoreAppearanceProjector projector,
        ISender sender)
    {
        _projector = projector;
        _sender = sender;
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
    public Task<StoreAppearanceAdminView> SaveAsync(string? paletteKey, string? themeMode, string? productCardSkin, CancellationToken cancellationToken)
        => SaveAsync(paletteKey, themeMode, productCardSkin, backgroundStyle: null, cancellationToken);

    /// <summary>PaletteKey و ThemeMode و پوستهٔ کارت و پس‌زمینه را از طریق Command می‌نویسد.</summary>
    public async Task<StoreAppearanceAdminView> SaveAsync(
        string? paletteKey,
        string? themeMode,
        string? productCardSkin,
        string? backgroundStyle,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new SaveStoreAppearanceSettingsCommand(
                new StoreAppearanceSettingsWriteModel(paletteKey, themeMode, productCardSkin, backgroundStyle)),
            cancellationToken);
        // Save handler invalidates projector cache; re-read effective view.
        return ToView(await _projector.GetEffectiveAsync(cancellationToken));
    }

    /// <summary>Maps effective projection + registries to Admin view.</summary>
    public static StoreAppearanceAdminView ToView(StoreAppearanceProjection current) =>
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
            current.BackgroundStyle,
            new StoreAppearanceTintView(
                current.PageBackgroundRgb,
                current.SectionBackgroundRgb,
                current.PageBackgroundDarkRgb,
                current.SectionBackgroundDarkRgb,
                current.SectionAlternateRgb,
                current.SectionAccentRgb,
                current.SectionAlternateDarkRgb,
                current.SectionAccentDarkRgb),
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
                        item.Tokens.PrimaryOnDarkRgb),
                    new StoreAppearanceTintView(
                        item.Tint.PageBackgroundRgb,
                        item.Tint.SectionBackgroundRgb,
                        item.Tint.PageBackgroundDarkRgb,
                        item.Tint.SectionBackgroundDarkRgb,
                        item.Tint.SectionAlternateRgb,
                        item.Tint.SectionAccentRgb,
                        item.Tint.SectionAlternateDarkRgb,
                        item.Tint.SectionAccentDarkRgb)))
                .ToArray(),
            StoreAppearanceProductCardSkinRegistry.All
                .Select(item => new StoreAppearanceSkinView(item.Key, item.NameFa, item.NameEn))
                .ToArray());
}
