using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Queries.ListSellerSettlementEntries;

/// <summary>سطرهای posted فروشنده.</summary>
public sealed record ListSellerSettlementEntriesQuery(Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<SettlementEntrySnapshot>>>;

/// <summary>Handler سطرهای posted.</summary>
public sealed class ListSellerSettlementEntriesQueryHandler(ISettlementDirectory settlement)
    : IRequestHandler<ListSellerSettlementEntriesQuery, Result<IReadOnlyList<SettlementEntrySnapshot>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SettlementEntrySnapshot>>> Handle(
        ListSellerSettlementEntriesQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await settlement.ListEntriesAsync(request.SellerPartyId, cancellationToken));
}
