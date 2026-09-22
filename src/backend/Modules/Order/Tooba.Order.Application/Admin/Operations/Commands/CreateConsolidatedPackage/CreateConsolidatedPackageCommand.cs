using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.CreateConsolidatedPackage;

public sealed record CreateConsolidatedPackageCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class CreateConsolidatedPackageHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<CreateConsolidatedPackageCommand, Result<object>>
{
    public Task<Result<object>> Handle(CreateConsolidatedPackageCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request with { Code = "create_consolidated_package" };
        return operations.ExecuteAsync(request.CheckoutId, request.ActorUserId, body, cancellationToken);
    }
}