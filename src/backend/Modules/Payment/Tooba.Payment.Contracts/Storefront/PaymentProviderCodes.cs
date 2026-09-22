namespace Tooba.Payment.Contracts.Storefront;

/// <summary>Stable payment provider codes for storefront projection (Contracts-only).</summary>
public static class PaymentProviderCodes
{
    public const string Wallet = "wallet";
    public const string Manual = "manual";
    public const string GatewayCatalog = "gateway";

    public static bool IsManual(string? providerCode) =>
        string.Equals(providerCode, Manual, StringComparison.OrdinalIgnoreCase);

    public static bool IsWallet(string? providerCode) =>
        string.Equals(providerCode, Wallet, StringComparison.OrdinalIgnoreCase);
}
