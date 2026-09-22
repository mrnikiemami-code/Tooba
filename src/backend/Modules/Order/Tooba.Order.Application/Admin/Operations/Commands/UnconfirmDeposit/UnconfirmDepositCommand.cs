using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.UnconfirmDeposit;

public sealed record UnconfirmDepositCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class UnconfirmDepositHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<UnconfirmDepositCommand, Result<object>>
{
    public Task<Result<object>> Handle(UnconfirmDepositCommand request, CancellationToken cancellationToken)
    {
        return operations.UnconfirmDepositAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}