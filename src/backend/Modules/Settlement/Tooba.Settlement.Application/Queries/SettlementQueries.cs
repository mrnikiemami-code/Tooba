using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Party.Contracts;
using Tooba.Settlement.Application.Errors;
using Tooba.Settlement.Application.Models;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Queries;

/// <summary>مانده فروشنده.</summary>
public sealed record GetSellerSettlementBalanceQuery(Guid SellerPartyId)
    : IRequest<Result<SettlementBalanceSnapshot>>;

/// <summary>Handler مانده فروشنده.</summary>
public sealed class GetSellerSettlementBalanceQueryHandler(ISettlementDirectory settlement)
    : IRequestHandler<GetSellerSettlementBalanceQuery, Result<SettlementBalanceSnapshot>>
{
    /// <inheritdoc />
    public async Task<Result<SettlementBalanceSnapshot>> Handle(
        GetSellerSettlementBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var balance = await settlement.GetBalanceAsync(request.SellerPartyId, cancellationToken);
        return balance is null
            ? Result.Failure<SettlementBalanceSnapshot>(new SemanticError(SettlementErrorCodes.AccountMissing))
            : Result.Success(balance);
    }
}

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

/// <summary>مانده همه فروشندگان (admin) با نام نمایشی.</summary>
public sealed record ListAdminSettlementBalancesQuery
    : IRequest<Result<IReadOnlyList<AdminSettlementBalanceListItem>>>;

/// <summary>Handler مانده admin.</summary>
public sealed class ListAdminSettlementBalancesQueryHandler(
    ISettlementDirectory settlement,
    IPartyLookup parties)
    : IRequestHandler<ListAdminSettlementBalancesQuery, Result<IReadOnlyList<AdminSettlementBalanceListItem>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AdminSettlementBalanceListItem>>> Handle(
        ListAdminSettlementBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var balances = await settlement.ListAllBalancesAsync(cancellationToken);
        if (balances.Count == 0)
        {
            return Result.Success<IReadOnlyList<AdminSettlementBalanceListItem>>([]);
        }

        var sellerIds = balances.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerNames = await parties.GetDisplayNamesAsync(sellerIds, cancellationToken);
        var items = balances.Select(balance =>
        {
            sellerNames.TryGetValue(balance.SellerPartyId, out var displayName);
            return new AdminSettlementBalanceListItem(
                balance.SettlementAccountId,
                balance.SellerPartyId,
                displayName ?? "فروشنده",
                balance.Currency,
                balance.PostedCredits,
                balance.PostedDebits,
                balance.ReservedPayouts,
                balance.AvailableBalance);
        }).ToList();
        return Result.Success<IReadOnlyList<AdminSettlementBalanceListItem>>(items);
    }
}

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

/// <summary>گرید DB-native صف payout (admin).</summary>
public sealed record QueryAdminPayoutGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminPayoutListItem>>>;

/// <summary>Handler گرید payout.</summary>
public sealed class QueryAdminPayoutGridQueryHandler(IAdminPayoutGridQuery grid)
    : IRequestHandler<QueryAdminPayoutGridQuery, Result<GridPageResponse<AdminPayoutListItem>>>
{
    /// <inheritdoc />
    public async Task<Result<GridPageResponse<AdminPayoutListItem>>> Handle(
        QueryAdminPayoutGridQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await grid.QueryAsync(request.Request, cancellationToken));
}
