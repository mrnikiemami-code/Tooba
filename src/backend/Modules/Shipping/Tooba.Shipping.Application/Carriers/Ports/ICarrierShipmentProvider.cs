namespace Tooba.Shipping.Application.Carriers.Ports;

/// <summary>Provider-neutral shipment lifecycle. Unsupported operations return Unsupported, never a simulated success.</summary>
public interface ICarrierShipmentProvider
{
    string ProviderCode { get; }

    CarrierShipmentCapabilities Capabilities { get; }

    Task<CarrierShipmentResult> CreateAsync(
        CarrierShipmentCreateRequest request,
        CancellationToken cancellationToken);

    Task<CarrierShipmentResult> TrackAsync(
        CarrierShipmentTrackRequest request,
        CancellationToken cancellationToken);

    Task<CarrierShipmentResult> CancelAsync(
        CarrierShipmentCancelRequest request,
        CancellationToken cancellationToken);
}

public interface ICarrierShipmentProviderRegistry
{
    bool TryResolve(string providerCode, out ICarrierShipmentProvider? provider);
}
