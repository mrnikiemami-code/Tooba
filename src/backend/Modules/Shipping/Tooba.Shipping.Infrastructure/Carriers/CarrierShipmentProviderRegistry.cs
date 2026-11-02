using Tooba.Shipping.Application.Carriers.Ports;

namespace Tooba.Shipping.Infrastructure.Carriers;

internal sealed class CarrierShipmentProviderRegistry : ICarrierShipmentProviderRegistry
{
    private readonly IReadOnlyDictionary<string, ICarrierShipmentProvider> _providers;

    public CarrierShipmentProviderRegistry(IEnumerable<ICarrierShipmentProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        var map = new Dictionary<string, ICarrierShipmentProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            var code = provider.ProviderCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("A carrier shipment provider has an empty provider code.");
            }

            if (!map.TryAdd(code, provider))
            {
                throw new InvalidOperationException($"Carrier shipment provider code '{code}' is registered more than once.");
            }
        }

        _providers = map;
    }

    public bool TryResolve(string providerCode, out ICarrierShipmentProvider? provider)
    {
        provider = null;
        return !string.IsNullOrWhiteSpace(providerCode)
               && _providers.TryGetValue(providerCode.Trim(), out provider);
    }
}
