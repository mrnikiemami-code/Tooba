using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.PrepareOperationalGroup;

/// <summary>
/// TB-253-R2: one group workflow — create/reuse constituent parcels and their common package.
/// </summary>
public sealed record PrepareOperationalGroupCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class PrepareOperationalGroupHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<PrepareOperationalGroupCommand, Result<object>>
{
    public Task<Result<object>> Handle(PrepareOperationalGroupCommand request, CancellationToken cancellationToken)
    {
        return operations.PrepareOperationalGroupAsync(
            request.CheckoutId,
            request.ActorUserId,
            request.Request,
            cancellationToken);
    }
}
