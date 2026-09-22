using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Returns.Contracts.Operations;

namespace Tooba.Order.Application.Admin.Operations.Queries.ListAdminOrderReturnEligibility;

public sealed record ListAdminOrderReturnEligibilityQuery(Guid CheckoutId)
    : IRequest<Result<IReadOnlyList<ReturnEligibilityResult>>>;

public sealed class ListAdminOrderReturnEligibilityHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<ListAdminOrderReturnEligibilityQuery, Result<IReadOnlyList<ReturnEligibilityResult>>>
{
    public Task<Result<IReadOnlyList<ReturnEligibilityResult>>> Handle(
        ListAdminOrderReturnEligibilityQuery request,
        CancellationToken cancellationToken) =>
        operations.ListReturnEligibilityAsync(request.CheckoutId, cancellationToken);
}