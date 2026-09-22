using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.PackFulfillmentSelected;

public sealed record PackFulfillmentSelectedCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class PackFulfillmentSelectedHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<PackFulfillmentSelectedCommand, Result<object>>
{
    public Task<Result<object>> Handle(PackFulfillmentSelectedCommand request, CancellationToken cancellationToken)
    {
        return operations.PackSelectedAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}