using Microsoft.Extensions.Options;
using Tooba.Pricing.Contracts.Dtos;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Pricing.Domain.ValueObjects;

namespace Tooba.Pricing.Infrastructure.Adapters;

/// <summary>
/// Pricing-owned implementation of the authoritative system currency reference.
/// Values come only from trusted commercial configuration; malformed or duplicate codes fail closed
/// at construction so no request can silently authorize an invented currency.
/// </summary>
public sealed class SystemCurrencyReference : ICurrencyReferenceReader
{
    private readonly IReadOnlyList<SystemCurrencyView> _active;
    private readonly HashSet<string> _activeCodes;

    /// <summary>Validates and freezes the configured reference.</summary>
    /// <exception cref="InvalidOperationException">Configuration is empty or malformed.</exception>
    public SystemCurrencyReference(IOptions<PricingCurrenciesOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var configured = options.Value.Currencies ?? [];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<SystemCurrencyView>();
        foreach (var entry in configured)
        {
            if (!CurrencyCode.TryParse(entry.Code, out var code, out var error))
            {
                throw new InvalidOperationException("Pricing:Currencies contains an invalid currency code: " + (error?.Code ?? entry.Code));
            }

            if (!seen.Add(code.Value))
            {
                throw new InvalidOperationException("Pricing:Currencies contains a duplicate currency code: " + code.Value);
            }

            if (entry.DecimalPlaces is < 0 or > 4)
            {
                throw new InvalidOperationException("Pricing:Currencies decimal places are out of range for " + code.Value);
            }

            // The kernel currency value object is the single scale authority; a configured precision
            // that disagrees would make the selector promise a precision the writer cannot persist.
            var kernelScale = AuthoredCurrency.Parse(code.Value).Scale;
            if (entry.DecimalPlaces != kernelScale)
            {
                throw new InvalidOperationException(
                    "Pricing:Currencies decimal places " + entry.DecimalPlaces + " disagree with the currency scale " +
                    kernelScale + " for " + code.Value);
            }

            if (entry.Active)
            {
                rows.Add(new SystemCurrencyView(
                    code.Value,
                    string.IsNullOrWhiteSpace(entry.EnglishName) ? code.Value : entry.EnglishName.Trim(),
                    string.IsNullOrWhiteSpace(entry.NativeName) ? code.Value : entry.NativeName.Trim(),
                    entry.DecimalPlaces,
                    entry.SortOrder));
            }
        }

        if (rows.Count == 0)
        {
            // Unconfigured reference is fail-closed: no currency is selectable and price writes are
            // rejected, but read models still compose without inventing a currency list.
            _active = [];
            _activeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        _active = rows.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.Ordinal).ToArray();
        _activeCodes = _active.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemCurrencyView>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_active);

    /// <inheritdoc />
    public Task<bool> IsActiveAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(code) && _activeCodes.Contains(code.Trim()));
}
