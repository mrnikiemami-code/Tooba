using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Commands.UpdateShippingService;

public sealed record UpdateShippingServiceCommand(Guid ServiceId, ShippingServiceWriteModel Model)
    : IRequest<Result<ShippingServiceDetailDto>>;

public sealed class UpdateShippingServiceHandler
    : IRequestHandler<UpdateShippingServiceCommand, Result<ShippingServiceDetailDto>>
{
    private readonly IShippingServiceDirectory _directory;
    private readonly IShippingCatalogReader _catalog;
    public UpdateShippingServiceHandler(IShippingServiceDirectory directory, IShippingCatalogReader catalog)
    { _directory = directory; _catalog = catalog; }

    public Task<Result<ShippingServiceDetailDto>> Handle(UpdateShippingServiceCommand request, CancellationToken cancellationToken) =>
        FulfillmentExceptionMapper.TryAsync(async () =>
        {
            await _directory.UpdateAsync(request.ServiceId, request.Model, cancellationToken);
            var detail = await _catalog.GetAsync(request.ServiceId, cancellationToken)
                ?? throw new InvalidOperationException(FulfillmentErrorCodes.ShippingServiceNotFound);
            return ShippingServiceSemantic.ToDetail(detail);
        });
}
