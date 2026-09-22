using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.CancelShipment;

public sealed record CancelShipmentCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class CancelShipmentHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<CancelShipmentCommand, Result<object>>
{
    public Task<Result<object>> Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "cancel_shipment" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}