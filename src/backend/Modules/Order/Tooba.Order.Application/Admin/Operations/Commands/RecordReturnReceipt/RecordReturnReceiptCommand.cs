using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RecordReturnReceipt;

/// <summary>TB-258: confirms the physical origin receipt of one returning parcel with per-line condition.</summary>
public sealed record RecordReturnReceiptCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RecordReturnReceiptHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RecordReturnReceiptCommand, Result<object>>
{
    public Task<Result<object>> Handle(RecordReturnReceiptCommand request, CancellationToken cancellationToken)
    {
        return operations.RecordReturnReceiptAsync(
            request.CheckoutId,
            request.ActorUserId,
            request.Request,
            cancellationToken);
    }
}
