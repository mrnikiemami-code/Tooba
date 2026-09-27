namespace Tooba.Catalog.Application.Settings.Quantity.Models;

/// <summary>Admin read model for quantity-rounding settings (success JSON shape).</summary>
public sealed record StoreQuantitySettingsView(
    string GlobalRoundingMode,
    string LabelFa,
    string LabelEn);
