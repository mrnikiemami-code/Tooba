using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Composition;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Ports;

namespace Tooba.Fulfillment.Application.Shipping.Commands;

public sealed record DeactivateShippingServiceCommand(Guid ServiceId) : IRequest<Result>;

public sealed class DeactivateShippingServiceHandler : IRequestHandler<DeactivateShippingServiceCommand, Result>
{
    private readonly IShippingServiceDirectory _directory;
    public DeactivateShippingServiceHandler(IShippingServiceDirectory directory) => _directory = directory;

    public Task<Result> Handle(DeactivateShippingServiceCommand request, CancellationToken cancellationToken) =>
        FulfillmentOperation.ExecuteAsync(() => _directory.DeactivateAsync(request.ServiceId, cancellationToken));
}
