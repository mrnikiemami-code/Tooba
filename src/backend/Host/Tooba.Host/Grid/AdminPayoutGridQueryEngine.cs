using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native صف payout Admin (Pending|Failed) با batch attempt و نام فروشنده.</summary>
internal sealed class AdminPayoutGridQueryEngine
{
    private readonly SettlementDbContext _db;
    private readonly PartyDbContext _parties;

    public AdminPayoutGridQueryEngine(SettlementDbContext db, PartyDbContext parties)
    {
        _db = db;
        _parties = parties;
    }

    public async Task<GridPageResponse<AdminPayoutListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<PayoutRequest> q = _db.PayoutRequests.AsNoTracking()
            .Where(x => x.Status == PayoutStatus.Pending || x.Status == PayoutStatus.Failed);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            q = q.Where(x => x.SellerPartyId.ToString().ToLower().Contains(term));
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.PayoutRequestId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("created", "desc");
        return await AdminEfGridQuery.PageAsync(
            q,
            request,
            filtered => Order(filtered, sort),
            MapPageAsync,
            cancellationToken);
    }

    private async Task<HashSet<Guid>?> EvaluateAdvancedAsync(
        GridAdvancedFilterExpression? expression,
        CancellationToken cancellationToken)
    {
        if (expression?.Conditions is not { Count: > 0 })
        {
            return null;
        }

        var baseQ = _db.PayoutRequests.AsNoTracking()
            .Where(x => x.Status == PayoutStatus.Pending || x.Status == PayoutStatus.Failed);
        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            var ids = await ApplyFilter(baseQ, filter).Select(x => x.PayoutRequestId).ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private static IQueryable<PayoutRequest> ApplyFilter(IQueryable<PayoutRequest> source, GridFilterRequest filter) =>
        filter.Field switch
        {
            "seller" => AdminEfGridQuery.ApplyTextFilter(source, x => x.SellerPartyId.ToString(), filter),
            "amount" => AdminEfGridQuery.ApplyNumberFilter(source, x => x.Amount, filter),
            "status" => AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter),
            "created" => AdminEfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter),
            _ => source,
        };

    private static IOrderedQueryable<PayoutRequest> Order(IQueryable<PayoutRequest> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "seller" => asc
                ? source.OrderBy(x => x.SellerPartyId).ThenBy(x => x.CreatedAt)
                : source.OrderByDescending(x => x.SellerPartyId).ThenBy(x => x.CreatedAt),
            "amount" => asc
                ? source.OrderBy(x => x.Amount).ThenBy(x => x.SellerPartyId)
                : source.OrderByDescending(x => x.Amount).ThenBy(x => x.SellerPartyId),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.SellerPartyId)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.SellerPartyId),
            _ => asc
                ? source.OrderBy(x => x.CreatedAt).ThenBy(x => x.SellerPartyId)
                : source.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.SellerPartyId),
        };
    }

    private async Task<IReadOnlyList<AdminPayoutListItem>> MapPageAsync(
        List<PayoutRequest> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var sellerIds = rows.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerRows = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName })
            .ToListAsync(cancellationToken);
        var sellerNames = sellerRows.ToDictionary(x => x.PartyId, x => x.DisplayName);

        return rows.Select(request =>
        {
            sellerNames.TryGetValue(request.SellerPartyId, out var sellerName);
            return new AdminPayoutListItem(
                request.PayoutRequestId,
                request.SettlementAccountId,
                request.SellerPartyId,
                sellerName ?? "فروشنده",
                request.Amount,
                request.Currency,
                request.Status.ToString(),
                request.IdempotencyKey,
                request.CreatedAt,
                request.UpdatedAt);
        }).ToList();
    }
}
