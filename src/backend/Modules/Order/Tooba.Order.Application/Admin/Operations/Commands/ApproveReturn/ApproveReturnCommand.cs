using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.ApproveReturn;

public sealed record ApproveReturnCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class ApproveReturnHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<ApproveReturnCommand, Result<object>>
{
    public Task<Result<object>> Handle(ApproveReturnCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "approve_return" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}