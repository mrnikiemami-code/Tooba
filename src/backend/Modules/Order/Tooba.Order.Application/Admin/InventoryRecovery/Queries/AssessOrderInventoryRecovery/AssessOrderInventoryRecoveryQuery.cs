using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.InventoryRecovery.Models;
using Tooba.Order.Application.Admin.InventoryRecovery.Services;

namespace Tooba.Order.Application.Admin.InventoryRecovery.Queries.AssessOrderInventoryRecovery;

public sealed record AssessOrderInventoryRecoveryQuery(Guid CheckoutId)
    : IRequest<Result<OrderInventoryRecoveryAssessment>>;

public sealed class AssessOrderInventoryRecoveryHandler(OrderInventoryRecoveryService recovery)
    : IRequestHandler<AssessOrderInventoryRecoveryQuery, Result<OrderInventoryRecoveryAssessment>>
{
    public Task<Result<OrderInventoryRecoveryAssessment>> Handle(
        AssessOrderInventoryRecoveryQuery request,
        CancellationToken cancellationToken) =>
        recovery.AssessCheckoutAsync(request.CheckoutId, cancellationToken);
}
