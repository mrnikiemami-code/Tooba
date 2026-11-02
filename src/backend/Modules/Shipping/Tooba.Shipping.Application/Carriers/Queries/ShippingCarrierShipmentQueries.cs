using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;

namespace Tooba.Shipping.Application.Carriers.Queries;

public sealed record ListShippingCarrierShipmentsQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid CheckoutId)
    : IRequest<Result<IReadOnlyList<ShippingCarrierShipmentOperationView>>>;

public sealed class ListShippingCarrierShipmentsHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<ListShippingCarrierShipmentsQuery, Result<IReadOnlyList<ShippingCarrierShipmentOperationView>>>
{
    public Task<Result<IReadOnlyList<ShippingCarrierShipmentOperationView>>> Handle(
        ListShippingCarrierShipmentsQuery request,
        CancellationToken cancellationToken)
        => operations.ListAsync(request.Owner, request.CheckoutId, cancellationToken);
}
