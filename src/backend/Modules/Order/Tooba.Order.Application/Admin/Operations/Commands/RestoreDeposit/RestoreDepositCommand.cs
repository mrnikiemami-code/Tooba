using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RestoreDeposit;

public sealed record RestoreDepositCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RestoreDepositHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RestoreDepositCommand, Result<object>>
{
    public Task<Result<object>> Handle(RestoreDepositCommand request, CancellationToken cancellationToken)
    {
        return operations.RestoreDepositAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}