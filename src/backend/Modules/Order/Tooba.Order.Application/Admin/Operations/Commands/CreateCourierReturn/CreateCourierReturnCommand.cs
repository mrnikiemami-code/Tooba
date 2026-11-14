using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.CreateCourierReturn;

/// <summary>
/// TB-265: creates the linked Returns case for a physically received courier-origin parcel. The case claims its exact
/// received quantity in Fulfillment first, then enters the EXISTING Returns approval/refund/disposition workflow; this
/// command never refunds, never releases stock and never re-receives the parcel.
/// </summary>
public sealed record CreateCourierReturnCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class CreateCourierReturnHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<CreateCourierReturnCommand, Result<object>>
{
    public Task<Result<object>> Handle(CreateCourierReturnCommand request, CancellationToken cancellationToken)
    {
        return operations.CreateCourierReturnAsync(
            request.CheckoutId,
            request.ActorUserId,
            request.Request,
            cancellationToken);
    }
}
