using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RecordNonDelivery;

/// <summary>TB-258: records a courier non-delivery attempt on one dispatched parcel.</summary>
public sealed record RecordNonDeliveryCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RecordNonDeliveryHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RecordNonDeliveryCommand, Result<object>>
{
    public Task<Result<object>> Handle(RecordNonDeliveryCommand request, CancellationToken cancellationToken)
    {
        return operations.RecordNonDeliveryAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}
