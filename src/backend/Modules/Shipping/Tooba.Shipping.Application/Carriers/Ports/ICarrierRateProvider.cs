namespace Tooba.Shipping.Application.Carriers.Ports;

/// <summary>
/// One carrier plug-in. Implementations own authentication and wire mapping; Shipping only sees the
/// normalized request/result above.
/// </summary>
public interface ICarrierRateProvider
{
    string ProviderCode { get; }

    Task<CarrierRateResult> QuoteAsync(
        CarrierRateRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Resolves a provider without a carrier-specific switch in Checkout or Shipping.</summary>
public interface ICarrierRateProviderRegistry
{
    bool TryResolve(string providerCode, out ICarrierRateProvider? provider);
}
