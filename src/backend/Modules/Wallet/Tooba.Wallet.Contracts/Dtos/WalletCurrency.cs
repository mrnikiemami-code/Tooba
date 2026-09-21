namespace Tooba.Wallet.Contracts.Dtos;

/// <summary>
/// Wallet-owned currency normalization for ledger/account identity.
/// Shared across Wallet internals and Payment wallet-gateway reference composition.
/// Not a general Pricing/Catalog currency type — Pricing retains its own CurrencyCode.
/// </summary>
public static class WalletCurrency
{
    /// <summary>Normalizes and validates a wallet ledger currency code.</summary>
    public static string Normalize(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidOperationException("wallet.currency_required");
        var trimmed = currency.Trim().ToUpperInvariant();
        if (trimmed.Length is < 3 or > 8)
            throw new InvalidOperationException("wallet.currency_invalid");
        return trimmed;
    }
}
