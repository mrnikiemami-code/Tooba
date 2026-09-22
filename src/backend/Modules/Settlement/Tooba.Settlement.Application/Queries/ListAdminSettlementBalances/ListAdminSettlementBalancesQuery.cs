using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Party.Contracts;
using Tooba.Settlement.Application.Models;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Queries.ListAdminSettlementBalances;

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
