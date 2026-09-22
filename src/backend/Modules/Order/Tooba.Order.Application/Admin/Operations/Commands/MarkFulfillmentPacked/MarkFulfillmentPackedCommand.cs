using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.MarkFulfillmentPacked;

public sealed record MarkFulfillmentPackedCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class MarkFulfillmentPackedHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<MarkFulfillmentPackedCommand, Result<object>>
{
    public Task<Result<object>> Handle(MarkFulfillmentPackedCommand request, CancellationToken cancellationToken)
    {
        return operations.MarkPackedAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}