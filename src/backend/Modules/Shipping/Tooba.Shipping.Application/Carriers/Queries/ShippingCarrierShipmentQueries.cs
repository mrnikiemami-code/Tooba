using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Contracts.Shipping;
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

public sealed record ListShippingGroupCarrierExecutionCapabilitiesQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid CheckoutId)
    : IRequest<Result<IReadOnlyList<ShippingGroupCarrierExecutionCapabilityView>>>;

public sealed class ListShippingGroupCarrierExecutionCapabilitiesHandler(
    IOrderCommittedCarrierShipmentFactsReader orderFacts,
    IShippingCarrierExecutionReadiness readiness)
    : IRequestHandler<ListShippingGroupCarrierExecutionCapabilitiesQuery, Result<IReadOnlyList<ShippingGroupCarrierExecutionCapabilityView>>>
{
    public async Task<Result<IReadOnlyList<ShippingGroupCarrierExecutionCapabilityView>>> Handle(
        ListShippingGroupCarrierExecutionCapabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await orderFacts.ListByCheckoutAsync(request.CheckoutId, cancellationToken);
        var views = new List<ShippingGroupCarrierExecutionCapabilityView>();
        foreach (var facts in rows.Where(f => ShippingCarrierOwnerScopeMatching.Matches(request.Owner, f)))
        {
            views.Add(await readiness.BuildCapabilityAsync(request.Owner, facts, cancellationToken));
        }

        return Result.Success<IReadOnlyList<ShippingGroupCarrierExecutionCapabilityView>>(views);
    }
}
