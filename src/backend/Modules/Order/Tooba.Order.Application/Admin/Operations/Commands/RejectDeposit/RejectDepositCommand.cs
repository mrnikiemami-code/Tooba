using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RejectDeposit;

public sealed record RejectDepositCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RejectDepositHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RejectDepositCommand, Result<object>>
{
    public Task<Result<object>> Handle(RejectDepositCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "reject_deposit" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}