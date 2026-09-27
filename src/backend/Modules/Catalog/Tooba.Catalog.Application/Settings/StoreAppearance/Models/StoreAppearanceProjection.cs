namespace Tooba.Catalog.Application.Settings.StoreAppearance.Models;

/// <summary>تصویر ظاهری مؤثر فروشگاه. توکن برند و tint؛ danger/success/warning جدا می‌مانند.</summary>
public sealed record StoreAppearanceProjection(
    string StoreScope,
    string PaletteKey,
    bool PaletteKeyWasKnown,
    string ThemeMode,
    string ProductCardSkin,
    string BackgroundStyle,
    string PrimaryRgb,
    string PrimaryStrongRgb,
    string OnPrimaryRgb,
    string FocusRgb,
    string PrimaryOnDarkRgb,
    string PageBackgroundRgb,
    string SectionBackgroundRgb,
    string PageBackgroundDarkRgb,
    string SectionBackgroundDarkRgb,
    string SectionAlternateRgb,
    string SectionAccentRgb,
    string SectionAlternateDarkRgb,
    string SectionAccentDarkRgb,
    DateTimeOffset UpdatedAt);
