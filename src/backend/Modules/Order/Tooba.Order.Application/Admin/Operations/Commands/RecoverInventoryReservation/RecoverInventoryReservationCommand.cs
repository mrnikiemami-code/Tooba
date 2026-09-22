using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RecoverInventoryReservation;

public sealed record RecoverInventoryReservationCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RecoverInventoryReservationHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RecoverInventoryReservationCommand, Result<object>>
{
    public Task<Result<object>> Handle(RecoverInventoryReservationCommand request, CancellationToken cancellationToken)
    {
        return operations.RecoverInventoryReservationAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}