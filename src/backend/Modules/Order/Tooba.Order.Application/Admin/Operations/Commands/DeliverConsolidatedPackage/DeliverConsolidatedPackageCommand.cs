using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.DeliverConsolidatedPackage;

public sealed record DeliverConsolidatedPackageCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class DeliverConsolidatedPackageHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<DeliverConsolidatedPackageCommand, Result<object>>
{
    public Task<Result<object>> Handle(DeliverConsolidatedPackageCommand request, CancellationToken cancellationToken)
    {
        return operations.DeliverConsolidatedPackageAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}