using Tooba.Shipping.Application.Carriers.Ports;

namespace Tooba.Shipping.Infrastructure.Carriers;

internal sealed class CarrierRateProviderRegistry : ICarrierRateProviderRegistry
{
    private readonly IReadOnlyDictionary<string, ICarrierRateProvider> _providers;

    public CarrierRateProviderRegistry(IEnumerable<ICarrierRateProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var map = new Dictionary<string, ICarrierRateProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            var code = provider.ProviderCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("A carrier provider has an empty provider code.");
            }

            if (!map.TryAdd(code, provider))
            {
                throw new InvalidOperationException($"Carrier provider code '{code}' is registered more than once.");
            }
        }

        _providers = map;
    }

    public bool TryResolve(string providerCode, out ICarrierRateProvider? provider)
    {
        provider = null;
        return !string.IsNullOrWhiteSpace(providerCode)
               && _providers.TryGetValue(providerCode.Trim(), out provider);
    }
}
