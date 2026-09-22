using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Queries.ListAdminPayoutQueue;

/// <summary>صف payout (admin).</summary>
public sealed record ListAdminPayoutQueueQuery
    : IRequest<Result<IReadOnlyList<PayoutRequestSnapshot>>>;

/// <summary>Handler صف payout.</summary>
public sealed class ListAdminPayoutQueueQueryHandler(ISettlementDirectory settlement)
    : IRequestHandler<ListAdminPayoutQueueQuery, Result<IReadOnlyList<PayoutRequestSnapshot>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PayoutRequestSnapshot>>> Handle(
        ListAdminPayoutQueueQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await settlement.ListPayoutQueueAsync(cancellationToken));
}
