using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Queries.ListSellerSettlementStatements;

/// <summary>صورت‌حساب‌های فروشنده.</summary>
public sealed record ListSellerSettlementStatementsQuery(Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<SettlementStatementSnapshot>>>;

/// <summary>Handler صورت‌حساب‌ها.</summary>
public sealed class ListSellerSettlementStatementsQueryHandler(ISettlementDirectory settlement)
    : IRequestHandler<ListSellerSettlementStatementsQuery, Result<IReadOnlyList<SettlementStatementSnapshot>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SettlementStatementSnapshot>>> Handle(
        ListSellerSettlementStatementsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await settlement.ListStatementsAsync(request.SellerPartyId, cancellationToken));
}
