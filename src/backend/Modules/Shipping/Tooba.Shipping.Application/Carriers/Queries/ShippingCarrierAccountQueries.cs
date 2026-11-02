using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;

namespace Tooba.Shipping.Application.Carriers.Queries;

public sealed record ListShippingCarrierAccountsQuery(ShippingAuthoringOwnerScope Owner)
    : IRequest<Result<IReadOnlyList<ShippingCarrierAccountView>>>;

public sealed class ListShippingCarrierAccountsHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<ListShippingCarrierAccountsQuery, Result<IReadOnlyList<ShippingCarrierAccountView>>>
{
    public Task<Result<IReadOnlyList<ShippingCarrierAccountView>>> Handle(
        ListShippingCarrierAccountsQuery request,
        CancellationToken cancellationToken)
        => directory.ListAccountsAsync(request.Owner, cancellationToken);
}

public sealed record GetShippingCarrierAccountQuery(ShippingAuthoringOwnerScope Owner, Guid AccountId)
    : IRequest<Result<ShippingCarrierAccountView>>;

public sealed class GetShippingCarrierAccountHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<GetShippingCarrierAccountQuery, Result<ShippingCarrierAccountView>>
{
    public Task<Result<ShippingCarrierAccountView>> Handle(
        GetShippingCarrierAccountQuery request,
        CancellationToken cancellationToken)
        => directory.GetAccountAsync(request.Owner, request.AccountId, cancellationToken);
}

public sealed record ListShippingCarrierReferencesQuery(ShippingAuthoringOwnerScope Owner, Guid AccountId)
    : IRequest<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>>;

public sealed class ListShippingCarrierReferencesHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<ListShippingCarrierReferencesQuery, Result<IReadOnlyList<ShippingCarrierReferenceValueView>>>
{
    public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> Handle(
        ListShippingCarrierReferencesQuery request,
        CancellationToken cancellationToken)
        => directory.ListReferencesAsync(request.Owner, request.AccountId, cancellationToken);
}

public sealed record ListShippingCarrierDestinationMappingsQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid AccountId) : IRequest<Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>>;

public sealed class ListShippingCarrierDestinationMappingsHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<ListShippingCarrierDestinationMappingsQuery, Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>>
{
    public Task<Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>> Handle(
        ListShippingCarrierDestinationMappingsQuery request,
        CancellationToken cancellationToken)
        => directory.ListDestinationMappingsAsync(request.Owner, request.AccountId, cancellationToken);
}

public sealed record GetShippingCarrierMethodBindingQuery(ShippingAuthoringOwnerScope Owner, Guid DraftId)
    : IRequest<Result<ShippingCarrierMethodBindingView?>>;

public sealed class GetShippingCarrierMethodBindingHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<GetShippingCarrierMethodBindingQuery, Result<ShippingCarrierMethodBindingView?>>
{
    public Task<Result<ShippingCarrierMethodBindingView?>> Handle(
        GetShippingCarrierMethodBindingQuery request,
        CancellationToken cancellationToken)
        => directory.GetMethodBindingAsync(request.Owner, request.DraftId, cancellationToken);
}
