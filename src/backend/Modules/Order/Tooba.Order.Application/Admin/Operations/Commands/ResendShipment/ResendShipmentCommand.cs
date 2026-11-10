using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.ResendShipment;

/// <summary>TB-258: allocates a new resend parcel for goods physically received back at origin.</summary>
public sealed record ResendShipmentCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class ResendShipmentHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<ResendShipmentCommand, Result<object>>
{
    public Task<Result<object>> Handle(ResendShipmentCommand request, CancellationToken cancellationToken)
    {
        return operations.ResendShipmentAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}
