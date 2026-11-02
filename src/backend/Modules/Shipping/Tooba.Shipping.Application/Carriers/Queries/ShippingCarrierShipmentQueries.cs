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
    IOrderCommittedCarrierShipmentFactsReader orderFacts)
    : IRequestHandler<ListShippingGroupCarrierExecutionCapabilitiesQuery, Result<IReadOnlyList<ShippingGroupCarrierExecutionCapabilityView>>>
{
    public async Task<Result<IReadOnlyList<ShippingGroupCarrierExecutionCapabilityView>>> Handle(
        ListShippingGroupCarrierExecutionCapabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await orderFacts.ListByCheckoutAsync(request.CheckoutId, cancellationToken);
        var filtered = rows
            .Where(facts => ShippingCarrierOwnerScopeMatching.Matches(request.Owner, facts))
            .Select(Map)
            .ToArray();
        return Result.Success<IReadOnlyList<ShippingGroupCarrierExecutionCapabilityView>>(filtered);
    }

    private static ShippingGroupCarrierExecutionCapabilityView Map(OrderCommittedCarrierShipmentFacts facts)
        => new(
            facts.CheckoutId,
            facts.ShippingGroupId,
            facts.SellerPartyId,
            facts.SellerOrderId,
            facts.MethodCode,
            facts.MethodLabel,
            facts.TransportMode,
            facts.OwnerKind,
            facts.OwnerTenantKey,
            facts.OwnerSellerPartyId,
            facts.RecipientName,
            facts.RecipientMobile,
            facts.RecipientAddress,
            facts.PostalCode,
            facts.ProvinceName,
            facts.CityName,
            facts.Currency,
            facts.PayableAtCheckoutAmount,
            facts.CarrierCollectAmount,
            facts.HasCarrierQuote,
            facts.CarrierAccountId,
            facts.ConfigurationVersion,
            facts.ProviderCode,
            facts.ProviderServiceCode,
            facts.QuoteInputIdentity,
            facts.OriginLocationCode,
            facts.DestinationLocationCode,
            facts.WeightGrams,
            facts.DeclaredValue,
            facts.LengthCentimeters,
            facts.WidthCentimeters,
            facts.HeightCentimeters,
            facts.SellerOrderStatus,
            facts.IsCancelled,
            facts.PaymentLocked,
            facts.CanExecuteCarrierShipment,
            facts.IneligibilityReasonCode,
            facts.OrderLineIds);
}
