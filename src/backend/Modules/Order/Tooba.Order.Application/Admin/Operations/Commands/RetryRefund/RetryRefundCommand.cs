using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RetryRefund;

public sealed record RetryRefundCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RetryRefundHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RetryRefundCommand, Result<object>>
{
    public Task<Result<object>> Handle(RetryRefundCommand request, CancellationToken cancellationToken)
    {
        return operations.RetryRefundAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}