using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.InventoryRecovery.Models;
using Tooba.Order.Application.Admin.InventoryRecovery.Services;

namespace Tooba.Order.Application.Admin.InventoryRecovery.Queries.AuditOrderInventoryRecovery;

public sealed record AuditOrderInventoryRecoveryQuery(int Take)
    : IRequest<Result<OrderInventoryRecoveryAuditPage>>;

public sealed class AuditOrderInventoryRecoveryHandler(OrderInventoryRecoveryService recovery)
    : IRequestHandler<AuditOrderInventoryRecoveryQuery, Result<OrderInventoryRecoveryAuditPage>>
{
    public async Task<Result<OrderInventoryRecoveryAuditPage>> Handle(
        AuditOrderInventoryRecoveryQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await recovery.AuditAsync(request.Take, cancellationToken));
}
