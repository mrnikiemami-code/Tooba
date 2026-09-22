using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.UnprocessFulfillment;

public sealed record UnprocessFulfillmentCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class UnprocessFulfillmentHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<UnprocessFulfillmentCommand, Result<object>>
{
    public Task<Result<object>> Handle(UnprocessFulfillmentCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "unprocess" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}