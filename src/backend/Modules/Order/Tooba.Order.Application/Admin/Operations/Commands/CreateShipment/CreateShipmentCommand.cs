using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.CreateShipment;

public sealed record CreateShipmentCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class CreateShipmentHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<CreateShipmentCommand, Result<object>>
{
    public Task<Result<object>> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "create_shipment" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}