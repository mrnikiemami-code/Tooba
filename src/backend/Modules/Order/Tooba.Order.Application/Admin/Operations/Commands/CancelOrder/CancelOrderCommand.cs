using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.CancelOrder;

public sealed record CancelOrderCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class CancelOrderHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<CancelOrderCommand, Result<object>>
{
    public Task<Result<object>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "cancel" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}