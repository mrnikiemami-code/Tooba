using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.RequestReturn;

public sealed record RequestReturnCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class RequestReturnHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<RequestReturnCommand, Result<object>>
{
    public Task<Result<object>> Handle(RequestReturnCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "request_return" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}