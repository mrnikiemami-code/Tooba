namespace Tooba.Pricing.Contracts.Ports;

/// <summary>
/// One active system currency from the Pricing-owned authoritative reference.
/// Decimal places are the authored precision policy, not a locale setting.
/// </summary>
public sealed record SystemCurrencyView(
    string Code,
    string EnglishName,
    string NativeName,
    int DecimalPlaces,
    int SortOrder);

/// <summary>
/// Read-only Pricing boundary over the trusted commercial currency configuration.
/// Consumers must not derive currencies from locale, market or free text.
/// </summary>
public interface ICurrencyReferenceReader
{
    /// <summary>Active configured system currencies, ordered for display.</summary>
    Task<IReadOnlyList<SystemCurrencyView>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>True only for an active configured system currency code.</summary>
    Task<bool> IsActiveAsync(string code, CancellationToken cancellationToken);
}
