namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Models;

/// <summary>Admin checkout-identity settings view (parity with Host CheckoutIdentitySettingsView).</summary>
public sealed record CheckoutIdentitySettingsView(
    string Policy,
    string LabelFa,
    string LabelEn,
    string? WarningFa);
