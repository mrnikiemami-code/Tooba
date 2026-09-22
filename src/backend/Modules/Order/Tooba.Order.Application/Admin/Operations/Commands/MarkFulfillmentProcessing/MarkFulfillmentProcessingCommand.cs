using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.MarkFulfillmentProcessing;

public sealed record MarkFulfillmentProcessingCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class MarkFulfillmentProcessingHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<MarkFulfillmentProcessingCommand, Result<object>>
{
    public Task<Result<object>> Handle(MarkFulfillmentProcessingCommand request, CancellationToken cancellationToken)
    {
        return operations.MarkProcessingAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}