using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native صف payout Admin (Pending|Failed) با batch attempt.</summary>
internal sealed class AdminPayoutGridQueryEngine
{
    private readonly SettlementDbContext _db;

    public AdminPayoutGridQueryEngine(SettlementDbContext db) => _db = db;

    public async Task<GridPageResponse<PayoutRequestSnapshot>> QueryAsync(
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

    private async Task<IReadOnlyList<PayoutRequestSnapshot>> MapPageAsync(
        List<PayoutRequest> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.PayoutRequestId).ToList();
        var attempts = await _db.PayoutAttempts.AsNoTracking()
            .Where(x => ids.Contains(x.PayoutRequestId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var byRequest = attempts.GroupBy(x => x.PayoutRequestId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return rows.Select(request =>
        {
            byRequest.TryGetValue(request.PayoutRequestId, out var list);
            list ??= [];
            return new PayoutRequestSnapshot(
                request.PayoutRequestId,
                request.SettlementAccountId,
                request.SellerPartyId,
                request.Amount,
                request.Currency,
                request.Status,
                request.IdempotencyKey,
                request.CreatedAt,
                request.UpdatedAt,
                list.Select(x => new PayoutAttemptSnapshot(
                    x.PayoutAttemptId,
                    x.PayoutRequestId,
                    x.Status,
                    x.IdempotencyKey,
                    x.ProviderReference,
                    x.FailureCode,
                    x.CreatedAt,
                    x.CompletedAt)).ToArray());
        }).ToList();
    }
}
