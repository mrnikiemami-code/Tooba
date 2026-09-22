using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.DispatchShipment;

public sealed record DispatchShipmentCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class DispatchShipmentHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<DispatchShipmentCommand, Result<object>>
{
    public Task<Result<object>> Handle(DispatchShipmentCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "dispatch_shipment" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}