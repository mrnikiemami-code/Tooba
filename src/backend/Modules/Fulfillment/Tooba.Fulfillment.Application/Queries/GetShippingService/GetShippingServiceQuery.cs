using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Queries.GetShippingService;

public sealed record GetShippingServiceQuery(Guid ServiceId) : IRequest<Result<ShippingServiceDetailDto>>;

public sealed class GetShippingServiceHandler
    : IRequestHandler<GetShippingServiceQuery, Result<ShippingServiceDetailDto>>
{
    private readonly IShippingCatalogReader _catalog;
    public GetShippingServiceHandler(IShippingCatalogReader catalog) => _catalog = catalog;

    public async Task<Result<ShippingServiceDetailDto>> Handle(GetShippingServiceQuery request, CancellationToken cancellationToken)
    {
        var entity = await _catalog.GetAsync(request.ServiceId, cancellationToken);
        return entity is null
            ? ShippingServiceSemantic.Failure<ShippingServiceDetailDto>(FulfillmentErrorCodes.ShippingServiceNotFound)
            : Result.Success(ShippingServiceSemantic.ToDetail(entity));
    }
}
