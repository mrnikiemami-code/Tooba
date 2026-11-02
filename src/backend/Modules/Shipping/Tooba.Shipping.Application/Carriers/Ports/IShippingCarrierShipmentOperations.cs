using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Carriers;

namespace Tooba.Shipping.Application.Carriers.Ports;

public interface IShippingCarrierShipmentOperations
{
    Task<Result<ShippingCarrierShipmentOperationView>> CreateAsync(
        ShippingAuthoringOwnerScope owner,
        ShippingCarrierShipmentCreateRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ShippingCarrierShipmentOperationView>>> ListAsync(
        ShippingAuthoringOwnerScope owner,
        Guid checkoutId,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierShipmentOperationView>> GetAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierShipmentOperationView>> TrackAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierShipmentOperationView>> CancelAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierShipmentOperationView>> LabelAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken);
}
