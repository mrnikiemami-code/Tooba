namespace Tooba.Settlement.Application.Payouts.Models;

/// <summary>
/// Display-label composition owned by Settlement presentation. These are human-readable fallbacks for
/// optional Party display data (not error text), so they are deliberately kept as module-owned
/// constants instead of error-localization resources. The certified Returns precedent treats
/// data-display labels exactly this way; error codes remain the only localized surface.
/// </summary>
public static class SettlementDisplayLabels
{
    /// <summary>Fallback display name when Party has no display name for a seller.</summary>
    public const string UnknownSeller = "فروشنده";
}
