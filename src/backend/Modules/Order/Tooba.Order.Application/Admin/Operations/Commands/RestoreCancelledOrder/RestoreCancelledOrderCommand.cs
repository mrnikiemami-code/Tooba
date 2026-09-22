using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RestoreCancelledOrder;

public sealed record RestoreCancelledOrderCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RestoreCancelledOrderHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RestoreCancelledOrderCommand, Result<object>>
{
    public Task<Result<object>> Handle(RestoreCancelledOrderCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "restore_cancelled_order" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}