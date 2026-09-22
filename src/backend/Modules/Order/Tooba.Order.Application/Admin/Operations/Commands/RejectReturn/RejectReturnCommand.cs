using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RejectReturn;

public sealed record RejectReturnCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RejectReturnHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RejectReturnCommand, Result<object>>
{
    public Task<Result<object>> Handle(RejectReturnCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "reject_return" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}