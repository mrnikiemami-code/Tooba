using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.ConfirmDeposit;

public sealed record ConfirmDepositCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class ConfirmDepositHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<ConfirmDepositCommand, Result<object>>
{
    public Task<Result<object>> Handle(ConfirmDepositCommand request, CancellationToken cancellationToken)
    {
        return operations.ConfirmDepositAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}