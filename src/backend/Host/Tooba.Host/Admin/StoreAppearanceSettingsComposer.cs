using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Host.Storefront;

namespace Tooba.Host.Admin;

/// <summary>خواندن ظاهر Store و ارسال فرمان نوشتن از طریق CQRS.</summary>
public sealed class StoreAppearanceSettingsComposer
{
    private readonly ICurrentCommerceContext _commerce;
    private readonly StoreAppearanceProjector _projector;
    private readonly ISender _sender;

    /// <summary>نویسنده ظاهر Store را به پروژکتور و ISender وصل می‌کند.</summary>
    public StoreAppearanceSettingsComposer(
        ICurrentCommerceContext commerce,
        StoreAppearanceProjector projector,
        ISender sender)
    {
        _commerce = commerce;
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

/// <summary>نمایه Admin ظاهر Store.</summary>
public sealed record StoreAppearanceAdminView(
    string StoreScope,
    string PaletteKey,
    bool PaletteKeyWasKnown,
    string ThemeMode,
    StoreAppearanceTokenView Tokens,
    string ProductCardSkin,
    string BackgroundStyle,
    StoreAppearanceTintView Tint,
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
    StoreAppearanceTokenView Tokens,
    StoreAppearanceTintView Tint);

/// <summary>توکن برند بدون رنگ وضعیت.</summary>
public sealed record StoreAppearanceTokenView(
    string PrimaryRgb,
    string PrimaryStrongRgb,
    string OnPrimaryRgb,
    string FocusRgb,
    string PrimaryOnDarkRgb);

/// <summary>توکن tint بدون رنگ وضعیت.</summary>
public sealed record StoreAppearanceTintView(
    string PageBackgroundRgb,
    string SectionBackgroundRgb,
    string PageBackgroundDarkRgb,
    string SectionBackgroundDarkRgb,
    string SectionAlternateRgb,
    string SectionAccentRgb,
    string SectionAlternateDarkRgb,
    string SectionAccentDarkRgb);

/// <summary>بدنه ذخیره ظاهر؛ PaletteKey الزامی و ThemeMode اختیاری.</summary>
public sealed record StoreAppearanceSettingsWriteRequest(
    string? PaletteKey,
    string? ThemeMode = null,
    string? ProductCardSkin = null,
    string? BackgroundStyle = null);
