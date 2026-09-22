using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.DispatchConsolidatedPackage;

public sealed record DispatchConsolidatedPackageCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class DispatchConsolidatedPackageHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<DispatchConsolidatedPackageCommand, Result<object>>
{
    public Task<Result<object>> Handle(DispatchConsolidatedPackageCommand request, CancellationToken cancellationToken)
    {
        return operations.DispatchConsolidatedPackageAsync(request.CheckoutId, request.ActorUserId, request.Request, cancellationToken);
    }
}