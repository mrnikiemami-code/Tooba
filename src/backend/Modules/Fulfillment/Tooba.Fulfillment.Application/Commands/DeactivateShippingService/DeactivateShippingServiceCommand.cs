using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.Shipping;

namespace Tooba.Fulfillment.Application.Commands.DeactivateShippingService;

public sealed record DeactivateShippingServiceCommand(Guid ServiceId) : IRequest<Result>;

public sealed class DeactivateShippingServiceHandler : IRequestHandler<DeactivateShippingServiceCommand, Result>
{
    private readonly IShippingServiceDirectory _directory;
    public DeactivateShippingServiceHandler(IShippingServiceDirectory directory) => _directory = directory;

    public Task<Result> Handle(DeactivateShippingServiceCommand request, CancellationToken cancellationToken) =>
        FulfillmentExceptionMapper.TryAsync(() => _directory.DeactivateAsync(request.ServiceId, cancellationToken));
}
