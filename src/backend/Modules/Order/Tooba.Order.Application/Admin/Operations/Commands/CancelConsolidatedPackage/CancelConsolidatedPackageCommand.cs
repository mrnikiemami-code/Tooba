using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.CancelConsolidatedPackage;

public sealed record CancelConsolidatedPackageCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class CancelConsolidatedPackageHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<CancelConsolidatedPackageCommand, Result<object>>
{
    public Task<Result<object>> Handle(CancelConsolidatedPackageCommand request, CancellationToken cancellationToken)
    {
        return operations.CancelConsolidatedPackageAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}