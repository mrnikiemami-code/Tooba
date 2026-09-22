using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Queries.ListSellerPayoutRequests;

/// <summary>فهرست payoutهای فروشنده.</summary>
public sealed record ListSellerPayoutRequestsQuery(Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<PayoutRequestSnapshot>>>;

/// <summary>Handler فهرست payout فروشنده.</summary>
public sealed class ListSellerPayoutRequestsQueryHandler(ISettlementDirectory settlement)
    : IRequestHandler<ListSellerPayoutRequestsQuery, Result<IReadOnlyList<PayoutRequestSnapshot>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PayoutRequestSnapshot>>> Handle(
        ListSellerPayoutRequestsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await settlement.ListPayoutRequestsForSellerAsync(request.SellerPartyId, cancellationToken));
}
