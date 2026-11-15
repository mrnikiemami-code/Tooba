using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.PlanShipmentDispatch;

/// <summary>
/// TB-270-R2: confirms the reviewed OWN dispatch plan of ONE existing resend/replacement parcel.
/// </summary>
/// <remarks>
/// The only write is the immutable Order row. The reviewed key distinguishes an exact replay from a new intent; the
/// selected customer date/slot is re-validated against a freshly computed feasible calendar and an expired selection is
/// refused instead of being clamped. A dispatch authorization is produced only by the presence of this confirmed row.
/// </remarks>
public sealed record PlanShipmentDispatchCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class PlanShipmentDispatchHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<PlanShipmentDispatchCommand, Result<object>>
{
    public Task<Result<object>> Handle(PlanShipmentDispatchCommand request, CancellationToken cancellationToken)
    {
        return operations.PlanShipmentDispatchAsync(
            request.CheckoutId,
            request.ActorUserId,
            request.Request,
            cancellationToken);
    }
}
