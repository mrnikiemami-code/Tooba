using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native دریافت‌های Admin روی Payment + enrich batch از Order (بدون JOIN بین schema).</summary>
internal sealed class AdminPaymentsGridQueryEngine
{
    private readonly PaymentDbContext _payments;
    private readonly OrderDbContext _orders;

    public AdminPaymentsGridQueryEngine(PaymentDbContext payments, OrderDbContext orders)
    {
        _payments = payments;
        _orders = orders;
    }

    public async Task<GridPageResponse<AdminReceiptListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<CustomerPayment> q = _payments.Payments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var checkoutIds = await _orders.Checkouts.AsNoTracking()
                .Where(c =>
                    c.RecipientName.ToLower().Contains(term)
                    || c.CheckoutId.ToString().ToLower().Contains(term)
                    || c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(term)))
                .Select(c => c.CheckoutId)
                .ToListAsync(cancellationToken);
            q = q.Where(p =>
                p.PaymentId.ToString().ToLower().Contains(term)
                || p.CheckoutId.ToString().ToLower().Contains(term)
                || p.ProviderCode.ToLower().Contains(term)
                || checkoutIds.Contains(p.CheckoutId));
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.PaymentId));
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

        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            var ids = await ApplyFilter(_payments.Payments.AsNoTracking(), filter)
                .Select(x => x.PaymentId)
                .ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private static IQueryable<CustomerPayment> ApplyFilter(IQueryable<CustomerPayment> source, GridFilterRequest filter) =>
        filter.Field switch
        {
            "reference" => AdminEfGridQuery.ApplyTextFilter(source, x => x.CheckoutId.ToString(), filter),
            "customer" => source,
            "amount" => AdminEfGridQuery.ApplyNumberFilter(source, x => x.Amount, filter),
            "status" => AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter),
            "provider" => AdminEfGridQuery.ApplyTextFilter(source, x => x.ProviderCode, filter),
            "created" => AdminEfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter),
            "completed" => AdminEfGridQuery.ApplyDateFilter(source, x => x.CompletedAt ?? x.CreatedAt, filter),
            _ => source,
        };

    private static IOrderedQueryable<CustomerPayment> Order(IQueryable<CustomerPayment> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "reference" => asc
                ? source.OrderBy(x => x.CheckoutId).ThenBy(x => x.PaymentId)
                : source.OrderByDescending(x => x.CheckoutId).ThenBy(x => x.PaymentId),
            "amount" => asc
                ? source.OrderBy(x => x.Amount).ThenBy(x => x.PaymentId)
                : source.OrderByDescending(x => x.Amount).ThenBy(x => x.PaymentId),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.PaymentId)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.PaymentId),
            "provider" => asc
                ? source.OrderBy(x => x.ProviderCode).ThenBy(x => x.PaymentId)
                : source.OrderByDescending(x => x.ProviderCode).ThenBy(x => x.PaymentId),
            "completed" => asc
                ? source.OrderBy(x => x.CompletedAt ?? x.CreatedAt).ThenBy(x => x.PaymentId)
                : source.OrderByDescending(x => x.CompletedAt ?? x.CreatedAt).ThenBy(x => x.PaymentId),
            _ => asc
                ? source.OrderBy(x => x.CreatedAt).ThenBy(x => x.PaymentId)
                : source.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.PaymentId),
        };
    }

    private async Task<IReadOnlyList<AdminReceiptListItem>> MapPageAsync(
        List<CustomerPayment> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var checkoutIds = rows.Select(x => x.CheckoutId).Distinct().ToList();
        var checkouts = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        var checkoutMap = checkouts.ToDictionary(x => x.CheckoutId);

        return rows.Select(payment =>
        {
            checkoutMap.TryGetValue(payment.CheckoutId, out var checkout);
            var references = checkout?.SellerOrders
                .Select(x => x.OrderNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? [];
            var reference = references.Count == 0
                ? payment.CheckoutId.ToString("N")[..12]
                : string.Join(" / ", references);
            var customer = checkout is null || string.IsNullOrWhiteSpace(checkout.RecipientName)
                ? "مشتری توبا"
                : checkout.RecipientName;
            return new AdminReceiptListItem(
                payment.PaymentId,
                payment.CheckoutId,
                reference,
                customer,
                payment.Amount,
                payment.Currency,
                payment.Status.ToString(),
                payment.ProviderCode,
                payment.CreatedAt,
                payment.CompletedAt);
        }).ToList();
    }
}
