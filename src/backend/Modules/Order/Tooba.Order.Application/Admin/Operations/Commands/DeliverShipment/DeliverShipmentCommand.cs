using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.DeliverShipment;

public sealed record DeliverShipmentCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class DeliverShipmentHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<DeliverShipmentCommand, Result<object>>
{
    public Task<Result<object>> Handle(DeliverShipmentCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "deliver_shipment" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}