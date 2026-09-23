using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Order.Application.Admin.OrdersGrid.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Contracts;
using Tooba.Persistence.Grid;
using Tooba.Returns.Contracts.Operations;

namespace Tooba.Order.Infrastructure.Admin.OrdersGrid;

/// <summary>
/// پرس‌وجوی DB-native گرید سفارش‌های Admin روی Checkout + aggregates SellerOrders.
/// نام فروشنده و مرجوعی فقط از Contracts ماژول مالک می‌آید؛ هیچ JOIN بین schemaها نیست.
/// </summary>
internal sealed class AdminOrdersGridReader : IAdminOrdersGridReader
{
    private const int PartyLookupTake = 10_000;

    private readonly OrderDbContext _orders;
    private readonly IPartyLookup _parties;
    private readonly IReturnAdminOperations _returns;
    private readonly IAdminOrderSupplyStatusReader _supply;
    private readonly IReservationCycleDirectory _cycles;

    public AdminOrdersGridReader(
        OrderDbContext orders,
        IPartyLookup parties,
        IReturnAdminOperations returns,
        IAdminOrderSupplyStatusReader supply,
        IReservationCycleDirectory cycles)
    {
        _orders = orders;
        _parties = parties;
        _returns = returns;
        _supply = supply;
        _cycles = cycles;
    }

    public async Task<GridPageResponse<AdminOrderListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IQueryable<CheckoutGroup> q = _orders.Checkouts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var matchingSellerIds = (await _parties.SearchIdsByDisplayNameAsync(term, PartyLookupTake, cancellationToken))
                .ToList();
            q = q.Where(c =>
                c.RecipientName.ToLower().Contains(term)
                || c.CheckoutId.ToString().ToLower().Contains(term)
                || c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(term))
                || (matchingSellerIds.Count > 0
                    && c.SellerOrders.Any(o => matchingSellerIds.Contains(o.SellerPartyId))));
        }

        foreach (var filter in request.Filters)
        {
            q = filter.Field == "sellers"
                ? await ApplySellerNamesFilterAsync(q, filter, cancellationToken)
                : filter.Field == "status"
                    ? await ApplyStatusFilterAsync(q, filter, cancellationToken)
                    : filter.Field == "supply"
                        ? await ApplySupplyFilterAsync(q, filter, cancellationToken)
                        : filter.Field == "reservation"
                            ? await ApplyReservationFilterAsync(q, filter, cancellationToken)
                            : ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.CheckoutId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("created", "desc");
        return await EfGridQuery.PageAsync(
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
            IQueryable<CheckoutGroup> filtered = _orders.Checkouts.AsNoTracking();
            filtered = condition.Field == "status"
                ? await ApplyStatusFilterAsync(filtered, filter, cancellationToken)
                : condition.Field == "sellers"
                    ? await ApplySellerNamesFilterAsync(filtered, filter, cancellationToken)
                    : ApplyFilter(filtered, filter);
            var ids = await filtered
                .Select(x => x.CheckoutId)
                .ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private static IQueryable<CheckoutGroup> ApplyFilter(IQueryable<CheckoutGroup> source, GridFilterRequest filter)
    {
        switch (filter.Field)
        {
            case "reference":
            {
                var op = (filter.Operator ?? string.Empty).Trim();
                var value = (filter.Value ?? string.Empty).Trim().ToLower();
                return op switch
                {
                    "blank" => source.Where(c => !c.SellerOrders.Any(o => o.OrderNumber != "")),
                    "notBlank" => source.Where(c => c.SellerOrders.Any(o => o.OrderNumber != "")),
                    "equals" => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower() == value)
                        || c.CheckoutId.ToString().ToLower() == value),
                    "notEqual" => source.Where(c =>
                        !c.SellerOrders.Any(o => o.OrderNumber.ToLower() == value)
                        && c.CheckoutId.ToString().ToLower() != value),
                    "startsWith" => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower().StartsWith(value))
                        || c.CheckoutId.ToString().ToLower().StartsWith(value)),
                    "endsWith" => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower().EndsWith(value))
                        || c.CheckoutId.ToString().ToLower().EndsWith(value)),
                    "notContains" => source.Where(c =>
                        !c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(value))
                        && !c.CheckoutId.ToString().ToLower().Contains(value)),
                    _ => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(value))
                        || c.CheckoutId.ToString().ToLower().Contains(value)),
                };
            }
            case "customer":
                return EfGridQuery.ApplyTextFilter(source, x => x.RecipientName, filter);
            case "sellers":
                return source;
            case "lines":
                return EfGridQuery.ApplyIntFilter(source, c => c.SellerOrders.Sum(o => o.TotalItemCount), filter);
            case "payment":
                return ApplyPaymentFilter(source, filter);
            case "status":
                return source;
            case "amount":
                return EfGridQuery.ApplyNumberFilter(source, c => c.SellerOrders.Sum(o => o.GrandTotalSnapshot), filter);
            case "created":
                return EfGridQuery.ApplyDateFilter(source, x => x.SubmittedAt, filter);
            default:
                return source;
        }
    }

    private static IQueryable<CheckoutGroup> ApplyPaymentFilter(IQueryable<CheckoutGroup> source, GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (op is "blank")
        {
            return source.Where(_ => false);
        }

        if (op is "notBlank")
        {
            return source;
        }

        bool wantsPaid = values.Contains("Paid");
        bool wantsPending = values.Contains("PendingPayment");

        return op switch
        {
            "notEqual" or "notIn" when wantsPaid && !wantsPending =>
                source.Where(c => !(c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid))),
            "notEqual" or "notIn" when wantsPending && !wantsPaid =>
                source.Where(c => c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid)),
            _ when wantsPaid && !wantsPending =>
                source.Where(c => c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid)),
            _ when wantsPending && !wantsPaid =>
                source.Where(c => !(c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid))),
            _ => source,
        };
    }

    private async Task<IQueryable<CheckoutGroup>> ApplyStatusFilterAsync(
        IQueryable<CheckoutGroup> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (op is "blank")
        {
            return source.Where(_ => false);
        }

        if (op is "notBlank")
        {
            return source;
        }

        var overlayKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ReturnRequested",
            "ReturnApproved",
            "RefundPending",
            "RefundCompleted",
            "RefundFailed",
        };
        var overlayWanted = new HashSet<string>(values.Where(overlayKeys.Contains), StringComparer.OrdinalIgnoreCase);
        HashSet<Guid> overlaySellerIds = [];
        if (overlayWanted.Count > 0)
        {
            var rows = await _returns.ListStatusOverlayAsync(cancellationToken);
            overlaySellerIds = rows
                .GroupBy(r => r.SellerOrderId)
                .Where(g => overlayWanted.Contains(
                    AdminOrdersGridProjection.ComposeOperationalStatus(
                        [SellerOrderStatus.Paid],
                        g.Select(x => x.Status).ToList())))
                .Select(g => g.Key)
                .ToHashSet();
        }

        var wantsMixed = values.Contains("Mixed");
        var parsed = values
            .Where(v => !overlayKeys.Contains(v) && !string.Equals(v, "Mixed", StringComparison.OrdinalIgnoreCase))
            .Select(v => Enum.TryParse<SellerOrderStatus>(v, true, out var s) ? (SellerOrderStatus?)s : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (op is "notEqual" or "notIn")
        {
            var exclude = overlaySellerIds;
            return source.Where(c =>
                !(wantsMixed && c.SellerOrders.Select(o => o.Status).Distinct().Count() > 1)
                && !(parsed.Count > 0
                    && c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                    && parsed.Contains(c.SellerOrders.Select(o => o.Status).First()))
                && !(exclude.Count > 0 && c.SellerOrders.Any(o => exclude.Contains(o.SellerOrderId))));
        }

        if (overlaySellerIds.Count == 0 && parsed.Count == 0 && !wantsMixed)
        {
            return source.Where(_ => false);
        }

        return source.Where(c =>
            (overlaySellerIds.Count > 0 && c.SellerOrders.Any(o => overlaySellerIds.Contains(o.SellerOrderId)))
            || (wantsMixed && c.SellerOrders.Select(o => o.Status).Distinct().Count() > 1)
            || (parsed.Count > 0
                && c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                && parsed.Contains(c.SellerOrders.Select(o => o.Status).First())));
    }

    private async Task<IQueryable<CheckoutGroup>> ApplySellerNamesFilterAsync(
        IQueryable<CheckoutGroup> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        if (op is "blank")
        {
            return source.Where(c => !c.SellerOrders.Any());
        }

        if (op is "notBlank")
        {
            return source.Where(c => c.SellerOrders.Any());
        }

        var value = (filter.Value ?? string.Empty).Trim();
        var values = (filter.Values ?? [])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();
        if (values.Count == 0 && !string.IsNullOrWhiteSpace(value))
        {
            values = [value];
        }

        var ids = await ResolveSellerPartyIdsAsync(op, values, cancellationToken);
        if (ids.Count == 0)
        {
            return op is "notEqual" or "notContains" or "notIn" ? source : source.Where(_ => false);
        }

        return op switch
        {
            "notEqual" or "notIn" or "notContains" =>
                source.Where(c => c.SellerOrders.Any(o => !ids.Contains(o.SellerPartyId))),
            _ => source.Where(c => c.SellerOrders.Any(o => ids.Contains(o.SellerPartyId))),
        };
    }

    /// <summary>
    /// نام فروشنده فقط از Contracts؛ چند مقداری با اجتماع (شمول) و اشتراک (نفی) مثل رفتار پیشین.
    /// </summary>
    private async Task<List<Guid>> ResolveSellerPartyIdsAsync(
        string op,
        IReadOnlyList<string> values,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0)
        {
            return (await _parties.FilterIdsByDisplayNameAsync(op, string.Empty, null, PartyLookupTake, cancellationToken))
                .ToList();
        }

        var negated = op is "notEqual" or "notContains" or "notIn";
        HashSet<Guid>? accumulated = null;
        foreach (var value in values)
        {
            var matched = (await _parties.FilterIdsByDisplayNameAsync(op, value, null, PartyLookupTake, cancellationToken))
                .ToHashSet();
            if (accumulated is null)
            {
                accumulated = matched;
                continue;
            }

            if (negated)
            {
                accumulated.IntersectWith(matched);
            }
            else
            {
                accumulated.UnionWith(matched);
            }
        }

        return accumulated is null ? [] : accumulated.ToList();
    }

    private static IQueryable<CheckoutGroup> Order(IQueryable<CheckoutGroup> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "reference" => asc
                ? source.OrderBy(c => c.SellerOrders.Select(o => o.OrderNumber).FirstOrDefault() ?? c.CheckoutId.ToString())
                    .ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Select(o => o.OrderNumber).FirstOrDefault() ?? c.CheckoutId.ToString())
                    .ThenBy(c => c.CheckoutId),
            "customer" => asc
                ? source.OrderBy(c => c.RecipientName).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.RecipientName).ThenBy(c => c.CheckoutId),
            "sellers" => asc
                ? source.OrderBy(c => c.SellerOrders.Count).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Count).ThenBy(c => c.CheckoutId),
            "lines" => asc
                ? source.OrderBy(c => c.SellerOrders.Sum(o => o.TotalItemCount)).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Sum(o => o.TotalItemCount)).ThenBy(c => c.CheckoutId),
            "payment" => asc
                ? source.OrderBy(c => c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid) ? 1 : 0).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid) ? 1 : 0).ThenBy(c => c.CheckoutId),
            "status" => asc
                ? source.OrderBy(c => c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                        ? c.SellerOrders.Select(o => o.Status).First().ToString()
                        : "Mixed")
                    .ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                        ? c.SellerOrders.Select(o => o.Status).First().ToString()
                        : "Mixed")
                    .ThenBy(c => c.CheckoutId),
            "amount" => asc
                ? source.OrderBy(c => c.SellerOrders.Sum(o => o.GrandTotalSnapshot)).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Sum(o => o.GrandTotalSnapshot)).ThenBy(c => c.CheckoutId),
            _ => asc
                ? source.OrderBy(c => c.SubmittedAt).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SubmittedAt).ThenBy(c => c.CheckoutId),
        };
    }

    private async Task<IReadOnlyList<AdminOrderListItem>> MapPageAsync(
        List<CheckoutGroup> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.CheckoutId).ToList();
        var groups = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => ids.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        var byId = groups.ToDictionary(x => x.CheckoutId);
        var sellerIds = groups.SelectMany(g => g.SellerOrders.Select(o => o.SellerPartyId)).Distinct().ToList();
        var sellerNames = await _parties.GetDisplayNamesAsync(sellerIds, cancellationToken);
        var sellerOrderIds = groups.SelectMany(g => g.SellerOrders.Select(o => o.SellerOrderId)).Distinct().ToList();
        var returnsBySellerOrder = await _returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var returnsLookup = returnsBySellerOrder
            .GroupBy(x => x.SellerOrderId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ReturnSnapshot>)g.ToList());
        var supply = await _supply.GetStatusesAsync(ids, cancellationToken);
        var supplyByCheckout = supply.ToDictionary(x => x.Key, x => (string?)x.Value);
        var cycles = await _cycles.GetProjectionsAsync(
            ids,
            DateTimeOffset.UtcNow,
            supplyByCheckout,
            cancellationToken);
        return rows
            .Where(r => byId.ContainsKey(r.CheckoutId))
            .Select(r => AdminOrdersGridProjection.MapOrderListItem(
                byId[r.CheckoutId],
                sellerNames,
                returnsLookup,
                supply.TryGetValue(r.CheckoutId, out var st) ? st : "NotApplicable",
                cycles.TryGetValue(r.CheckoutId, out var projection)
                    ? OrderReservationCycleSummaryMapper.ToSummary(projection)
                    : OrderReservationCycleSummaryMapper.EmptySummary()))
            .ToList();
    }

    private async Task<IQueryable<CheckoutGroup>> ApplySupplyFilterAsync(
        IQueryable<CheckoutGroup> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var wanted = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
        {
            return source;
        }

        var ids = await source.Select(x => x.CheckoutId).ToListAsync(cancellationToken);
        var statuses = await _supply.GetStatusesAsync(ids, cancellationToken);
        var match = ids.Where(id => statuses.TryGetValue(id, out var st) && wanted.Contains(st)).ToHashSet();
        return source.Where(x => match.Contains(x.CheckoutId));
    }

    private async Task<IQueryable<CheckoutGroup>> ApplyReservationFilterAsync(
        IQueryable<CheckoutGroup> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var wanted = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
        {
            return source;
        }

        var ids = await source.Select(x => x.CheckoutId).ToListAsync(cancellationToken);
        var supply = await _supply.GetStatusesAsync(ids, cancellationToken);
        var supplyByCheckout = supply.ToDictionary(x => x.Key, x => (string?)x.Value);
        var cycles = await _cycles.GetProjectionsAsync(
            ids,
            DateTimeOffset.UtcNow,
            supplyByCheckout,
            cancellationToken);
        var match = ids.Where(id =>
        {
            var summary = cycles.TryGetValue(id, out var projection)
                ? OrderReservationCycleSummaryMapper.ToSummary(projection)
                : OrderReservationCycleSummaryMapper.EmptySummary();
            return wanted.Contains(summary.State) || wanted.Contains(summary.CompactLabelFa);
        }).ToHashSet();
        return source.Where(x => match.Contains(x.CheckoutId));
    }
}
