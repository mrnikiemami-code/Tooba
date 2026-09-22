using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.AssignConsolidatedPackageTracking;

public sealed record AssignConsolidatedPackageTrackingCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class AssignConsolidatedPackageTrackingHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<AssignConsolidatedPackageTrackingCommand, Result<object>>
{
    public Task<Result<object>> Handle(AssignConsolidatedPackageTrackingCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "assign_consolidated_package_tracking" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}