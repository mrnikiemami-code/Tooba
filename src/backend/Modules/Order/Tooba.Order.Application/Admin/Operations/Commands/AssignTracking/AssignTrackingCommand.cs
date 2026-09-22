using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.AssignTracking;

public sealed record AssignTrackingCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class AssignTrackingHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<AssignTrackingCommand, Result<object>>
{
    public Task<Result<object>> Handle(AssignTrackingCommand request, CancellationToken cancellationToken)
    {
        return operations.AssignTrackingAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}