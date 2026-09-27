namespace Tooba.Catalog.Application.Settings.StoreAppearance.Models;

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
