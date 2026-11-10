using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.StartReturn;

/// <summary>TB-258: the carrier started the physical return of a non-delivered parcel to origin.</summary>
public sealed record StartReturnCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class StartReturnHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<StartReturnCommand, Result<object>>
{
    public Task<Result<object>> Handle(StartReturnCommand request, CancellationToken cancellationToken)
    {
        return operations.StartReturnAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}
