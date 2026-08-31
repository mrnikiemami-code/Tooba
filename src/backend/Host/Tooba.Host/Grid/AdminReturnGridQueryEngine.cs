using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native گرید مرجوعی Admin با batch map آیتم/attempt.</summary>
internal sealed class AdminReturnGridQueryEngine
{
    private readonly ReturnsDbContext _db;

    public AdminReturnGridQueryEngine(ReturnsDbContext db) => _db = db;

    public async Task<GridPageResponse<ReturnSnapshot>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<ReturnRequest> q = _db.ReturnRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            q = q.Where(x =>
                x.ReturnRequestId.ToString().ToLower().Contains(term)
                || x.SellerOrderId.ToString().ToLower().Contains(term));
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.ReturnRequestId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("createdAt", "desc");
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

        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            var ids = await ApplyFilter(_db.ReturnRequests.AsNoTracking(), filter)
                .Select(x => x.ReturnRequestId)
                .ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private IQueryable<ReturnRequest> ApplyFilter(IQueryable<ReturnRequest> source, GridFilterRequest filter)
    {
        switch (filter.Field)
        {
            case "returnRequestId":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.ReturnRequestId.ToString(), filter);
            case "sellerOrderId":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.SellerOrderId.ToString(), filter);
            case "itemCount":
            {
                var itemCounts = _db.ReturnItems.AsNoTracking()
                    .GroupBy(x => x.ReturnRequestId)
                    .Select(g => new { ReturnRequestId = g.Key, Count = g.Count() });
                var joined = from r in source
                             join c in itemCounts on r.ReturnRequestId equals c.ReturnRequestId into cj
                             from c in cj.DefaultIfEmpty()
                             select new { Request = r, Count = c != null ? c.Count : 0 };
                joined = AdminEfGridQuery.ApplyIntFilter(joined, x => x.Count, filter);
                return joined.Select(x => x.Request);
            }
            case "refundAmount":
                return AdminEfGridQuery.ApplyNumberFilter(source, x => x.RefundAmount, filter);
            case "status":
                return AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
            case "createdAt":
                return AdminEfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter);
            default:
                return source;
        }
    }

    private IQueryable<ReturnRequest> Order(IQueryable<ReturnRequest> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        if (sort.Field == "itemCount")
        {
            var itemCounts = _db.ReturnItems.AsNoTracking()
                .GroupBy(x => x.ReturnRequestId)
                .Select(g => new { ReturnRequestId = g.Key, Count = g.Count() });
            var joined = from r in source
                         join c in itemCounts on r.ReturnRequestId equals c.ReturnRequestId into cj
                         from c in cj.DefaultIfEmpty()
                         select new { Request = r, Count = c != null ? c.Count : 0 };
            var ordered = asc
                ? joined.OrderBy(x => x.Count).ThenBy(x => x.Request.ReturnRequestId)
                : joined.OrderByDescending(x => x.Count).ThenBy(x => x.Request.ReturnRequestId);
            return ordered.Select(x => x.Request);
        }

        return sort.Field switch
        {
            "returnRequestId" => asc
                ? source.OrderBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.ReturnRequestId),
            "sellerOrderId" => asc
                ? source.OrderBy(x => x.SellerOrderId).ThenBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.SellerOrderId).ThenBy(x => x.ReturnRequestId),
            "refundAmount" => asc
                ? source.OrderBy(x => x.RefundAmount).ThenBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.RefundAmount).ThenBy(x => x.ReturnRequestId),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.ReturnRequestId),
            _ => asc
                ? source.OrderBy(x => x.CreatedAt).ThenBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ReturnRequestId),
        };
    }

    private async Task<IReadOnlyList<ReturnSnapshot>> MapPageAsync(
        List<ReturnRequest> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.ReturnRequestId).ToList();
        var items = await _db.ReturnItems.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .ToListAsync(cancellationToken);
        var attempts = await _db.RefundAttempts.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var itemsBy = items.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());
        var attemptsBy = attempts.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());

        return rows.Select(request =>
        {
            itemsBy.TryGetValue(request.ReturnRequestId, out var itemList);
            attemptsBy.TryGetValue(request.ReturnRequestId, out var attemptList);
            itemList ??= [];
            attemptList ??= [];
            return new ReturnSnapshot(
                request.ReturnRequestId,
                request.SellerOrderId,
                request.CheckoutId,
                request.SellerPartyId,
                request.RequestedByUserId,
                request.Status,
                request.Reason,
                request.Currency,
                request.RefundAmount,
                request.PaymentId,
                request.RefundDestination,
                request.CreatedAt,
                request.UpdatedAt,
                itemList.Select(x => new ReturnItemSnapshot(
                    x.ReturnItemId,
                    x.OrderLineId,
                    x.Quantity,
                    x.UnitPriceSnapshot,
                    x.Currency,
                    x.ReservationId)).ToArray(),
                attemptList.Select(x => new RefundAttemptSnapshot(
                    x.RefundAttemptId,
                    x.PaymentId,
                    x.Amount,
                    x.Currency,
                    x.Status,
                    x.IdempotencyKey,
                    x.ProviderReference,
                    x.FailureCode,
                    x.CreatedAt,
                    x.CompletedAt)).ToArray());
        }).ToList();
    }
}
