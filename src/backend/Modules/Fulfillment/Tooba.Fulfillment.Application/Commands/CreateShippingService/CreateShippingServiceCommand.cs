using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Commands.CreateShippingService;

public sealed record CreateShippingServiceCommand(ShippingServiceWriteModel Model)
    : IRequest<Result<ShippingServiceDetailDto>>;

public sealed class CreateShippingServiceHandler
    : IRequestHandler<CreateShippingServiceCommand, Result<ShippingServiceDetailDto>>
{
    private readonly IShippingServiceDirectory _directory;
    private readonly IShippingCatalogReader _catalog;
    public CreateShippingServiceHandler(IShippingServiceDirectory directory, IShippingCatalogReader catalog)
    { _directory = directory; _catalog = catalog; }

    public Task<Result<ShippingServiceDetailDto>> Handle(CreateShippingServiceCommand request, CancellationToken cancellationToken) =>
        FulfillmentExceptionMapper.TryAsync(async () =>
        {
            var id = await _directory.CreateAsync(request.Model, cancellationToken);
            var detail = await _catalog.GetAsync(id, cancellationToken)
                ?? throw new InvalidOperationException(FulfillmentErrorCodes.ShippingServiceNotFound);
            return ShippingServiceSemantic.ToDetail(detail);
        });
}
