using Tooba.Fulfillment.Contracts.Shipping;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence.Grid;

namespace Tooba.Fulfillment.Infrastructure.Queries;

/// <summary>
/// پرس‌وجوی DB-native صف کار ارسال و تحویل Admin با batch map و بدون N+1.
/// </summary>
/// <remarks>
/// TB-260: the queue is dispatch-aware. It joins the Fulfillment-owned physical facts with the Order-owned
/// <b>durable accepted dispatch plan</b> through <see cref="IOrderDispatchPlanFactsReader"/> (Contracts only,
/// no cross-module DbContext, no Shipping settings lookup, no browser date arithmetic) and classifies every
/// row with the single pure <see cref="AdminFulfillmentDispatchTimingAuthority"/>.
/// </remarks>
public sealed class AdminFulfillmentWorkQueueQueryEngine : IAdminFulfillmentWorkQueueQuery
{
    private readonly FulfillmentDbContext _db;
    private readonly IPartyLookup _parties;
    private readonly IOrderGridEnrichmentReader _orders;
    private readonly IOrderDispatchPlanFactsReader _dispatchPlans;
    private readonly IClock _clock;

    /// <summary>موتور صف کار را به DbContextها وصل می‌کند.</summary>
    public AdminFulfillmentWorkQueueQueryEngine(
        FulfillmentDbContext db,
        IPartyLookup parties,
        IOrderGridEnrichmentReader orders,
        IOrderDispatchPlanFactsReader dispatchPlans,
        IClock clock)
    {
        _db = db;
        _parties = parties;
        _orders = orders;
        _dispatchPlans = dispatchPlans;
        _clock = clock;
    }

    /// <summary>صفحه‌بندی و فیلتر صف کار را اجرا می‌کند.</summary>
    public async Task<GridPageResponse<AdminFulfillmentWorkQueueRow>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var (rows, timingFilter) = await ClassifyAsync(request, cancellationToken);
        var filtered = timingFilter is null
            ? rows
            : rows.Where(row => MatchesTiming(row, timingFilter)).ToList();

        var ordered = ApplySort(filtered, request);
        var page = Math.Max(1, request.Page);
        var size = Math.Max(1, request.PageSize);
        var items = ordered.Skip((page - 1) * size).Take(size).ToList();
        return new GridPageResponse<AdminFulfillmentWorkQueueRow>(items, page, size, ordered.Count);
    }

    /// <summary>TB-260: counts of the whole filtered set, never of one page.</summary>
    public async Task<AdminFulfillmentWorkQueueCounts> CountAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var (rows, _) = await ClassifyAsync(request, cancellationToken);
        // TB-260: counts always describe the set produced by search/column filters, INDEPENDENT of the
        // selected quick dispatch tab. Otherwise selecting «سررسیدشده» would zero every other tab's badge.
        return new AdminFulfillmentWorkQueueCounts(
            rows.Count(row => row.PendingDispatch),
            rows.Count(row => IsDue(row)),
            rows.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Today),
            rows.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Overdue),
            rows.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Upcoming),
            rows.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Unspecified),
            rows.Count(row => row.DispatchBucket == AdminFulfillmentDispatchBucketNames.InTransit));
    }

    private static bool IsDue(AdminFulfillmentWorkQueueRow row) =>
        row.DispatchBucket is AdminFulfillmentDispatchBucketNames.Today or AdminFulfillmentDispatchBucketNames.Overdue
        && row.HandoffWindowState is AdminFulfillmentDispatchBucketNames.WindowOpen
            or AdminFulfillmentDispatchBucketNames.WindowClosed;

    private static bool MatchesTiming(AdminFulfillmentWorkQueueRow row, string filter) =>
        filter.ToLowerInvariant() switch
        {
            AdminFulfillmentDispatchTimingAuthority.FilterDue => IsDue(row),
            AdminFulfillmentDispatchTimingAuthority.FilterToday =>
                row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Today,
            AdminFulfillmentDispatchTimingAuthority.FilterOverdue =>
                row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Overdue,
            AdminFulfillmentDispatchTimingAuthority.FilterUpcoming =>
                row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Upcoming,
            AdminFulfillmentDispatchTimingAuthority.FilterUnspecified =>
                row.DispatchBucket == AdminFulfillmentDispatchBucketNames.Unspecified,
            _ => true,
        };

    /// <summary>
    /// Applies search/filters in SQL, then classifies the whole filtered set in memory. Timing must be
    /// evaluated by ONE authority over complete rows, so a page can never be filtered after paging.
    /// </summary>
    private async Task<(List<AdminFulfillmentWorkQueueRow> Rows, string? TimingFilter)> ClassifyAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<FulfillmentUnit> q = _db.Fulfillments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var sellerIds = await _parties.SearchIdsByDisplayNameAsync(term, 200, cancellationToken);
            var matchingOrderIds = await _orders.SearchSellerOrderIdsByOrderNumberAsync(term, 200, cancellationToken);
            q = q.Where(x =>
                x.RecipientName.ToLower().Contains(term)
                || x.FulfillmentId.ToString().ToLower().Contains(term)
                || x.CheckoutId.ToString().ToLower().Contains(term)
                || x.CityName.ToLower().Contains(term)
                || x.ShippingMethodLabel.ToLower().Contains(term)
                || sellerIds.Contains(x.SellerPartyId)
                || matchingOrderIds.Contains(x.SellerOrderId));
        }

        string? timingFilter = null;
        var queueFilterRequested = false;
        foreach (var filter in request.Filters)
        {
            if (filter.Field == "queueFilter")
            {
                queueFilterRequested = true;
                var raw = (filter.Values?.FirstOrDefault() ?? filter.Value ?? string.Empty).Trim();
                if (AdminFulfillmentDispatchTimingAuthority.IsTimingFilter(raw))
                {
                    // TB-260: timing is classified from the accepted plan, never from a SQL status alone.
                    timingFilter = string.Equals(raw, AdminFulfillmentQueueFilters.All, StringComparison.OrdinalIgnoreCase)
                        ? null
                        : raw.ToLowerInvariant();
                    continue;
                }
            }

            q = await ApplyFilterAsync(q, filter, cancellationToken);
        }

        if (!queueFilterRequested)
        {
            // TB-260: the DEFAULT queue is live work, not an order archive. Finished rows stay reachable
            // through the explicit delivered/problem/history quick filters, which set queueFilter.
            q = q.Where(x => x.Status != FulfillmentStatus.Cancelled && x.Status != FulfillmentStatus.Delivered);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.FulfillmentId));
        }

        var units = await q.ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            return ([], timingFilter);
        }

        var rows = await MapPageAsync(units, _clock.UtcNow, cancellationToken);
        return (rows.ToList(), timingFilter);
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
            var filtered = await ApplyFilterAsync(_db.Fulfillments.AsNoTracking(), filter, cancellationToken);
            var ids = await filtered.Select(x => x.FulfillmentId).ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private async Task<IQueryable<FulfillmentUnit>> ApplyFilterAsync(
        IQueryable<FulfillmentUnit> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        switch (filter.Field)
        {
            case "recipientName":
                return EfGridQuery.ApplyTextFilter(source, x => x.RecipientName, filter);
            case "fulfillmentId":
                return EfGridQuery.ApplyTextFilter(source, x => x.FulfillmentId.ToString(), filter);
            case "checkoutId":
                return EfGridQuery.ApplyTextFilter(source, x => x.CheckoutId.ToString(), filter);
            case "cityName":
                return EfGridQuery.ApplyTextFilter(source, x => x.CityName, filter);
            case "orderReference":
            {
                var sellerOrderIds = await _orders.FilterSellerOrderIdsByOrderNumberAsync(
                    filter.Operator, filter.Value, filter.Values, 500, cancellationToken);
                return source.Where(x => sellerOrderIds.Contains(x.SellerOrderId));
            }
            case "shippingMethodCode":
            {
                var codes = (filter.Values ?? [])
                    .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();
                if (codes.Count == 0)
                {
                    return source.Where(_ => false);
                }

                return filter.Operator is "notEqual" or "notIn"
                    ? source.Where(x => !codes.Contains(x.ShippingMethodCode.ToLower()))
                    : source.Where(x => codes.Contains(x.ShippingMethodCode.ToLower()));
            }
            case "shippingMethodLabel":
                return EfGridQuery.ApplyTextFilter(source, x => x.ShippingMethodLabel, filter);
            case "sellerPartyId":
            {
                var ids = ParseGuidValues(filter);
                if (ids.Count == 0)
                {
                    return source.Where(_ => false);
                }

                return filter.Operator is "notEqual" or "notIn"
                    ? source.Where(x => !ids.Contains(x.SellerPartyId))
                    : source.Where(x => ids.Contains(x.SellerPartyId));
            }
            case "sellerDisplayName":
            {
                var partyIds = await _parties.FilterIdsByDisplayNameAsync(
                    filter.Operator, filter.Value, filter.Values, 500, cancellationToken);
                return source.Where(x => partyIds.Contains(x.SellerPartyId));
            }
            case "shipmentCount":
            {
                var counts = _db.Shipments.AsNoTracking()
                    .GroupBy(x => x.FulfillmentId)
                    .Select(g => new { FulfillmentId = g.Key, Count = g.Count() });
                var joined = from u in source
                             join c in counts on u.FulfillmentId equals c.FulfillmentId into cj
                             from c in cj.DefaultIfEmpty()
                             select new { Unit = u, Count = c != null ? c.Count : 0 };
                joined = EfGridQuery.ApplyIntFilter(joined, x => x.Count, filter);
                return joined.Select(x => x.Unit);
            }
            case "status":
                return await ApplyStatusFilterAsync(source, filter, cancellationToken);
            case "queueFilter":
                return await ApplyQueueFilterAsync(source, filter, cancellationToken);
            case "createdAt":
                return EfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter);
            case "updatedAt":
                return EfGridQuery.ApplyDateFilter(source, x => x.UpdatedAt, filter);
            default:
                return source;
        }
    }

    private async Task<IQueryable<FulfillmentUnit>> ApplyQueueFilterAsync(
        IQueryable<FulfillmentUnit> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var raw = (filter.Values?.FirstOrDefault() ?? filter.Value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw)
            || raw.Equals(AdminFulfillmentQueueFilters.All, StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        var key = raw.ToLowerInvariant();
        if (key is AdminFulfillmentQueueFilters.ReadyToProcess
            or AdminFulfillmentQueueFilters.ReadyToPack
            or AdminFulfillmentQueueFilters.ReadyToShip
            or AdminFulfillmentQueueFilters.InTransit
            or AdminFulfillmentQueueFilters.Delivered
            or AdminFulfillmentQueueFilters.Problem)
        {
            var processableIds = await ItemFulfillmentIdsAsync(i => i.QuantityOrdered > i.QuantityProcessing, cancellationToken);
            var packableIds = await ItemFulfillmentIdsAsync(i => i.QuantityProcessing > i.QuantityPacked, cancellationToken);
            var shippableIds = await ItemFulfillmentIdsAsync(i => i.QuantityPacked > i.QuantityShipped, cancellationToken);
            var remainingIds = await ItemFulfillmentIdsAsync(i => i.QuantityOrdered > i.QuantityShipped, cancellationToken);
            return key switch
            {
                AdminFulfillmentQueueFilters.ReadyToProcess =>
                    source.Where(x =>
                        x.Status != FulfillmentStatus.Cancelled
                        && x.Status != FulfillmentStatus.Delivered
                        && x.Status != FulfillmentStatus.Failed
                        && (x.Status == FulfillmentStatus.ReadyToFulfill || processableIds.Contains(x.FulfillmentId))),
                AdminFulfillmentQueueFilters.ReadyToPack =>
                    source.Where(x =>
                        x.Status != FulfillmentStatus.Cancelled
                        && x.Status != FulfillmentStatus.Delivered
                        && x.Status != FulfillmentStatus.Failed
                        && (x.Status == FulfillmentStatus.Processing || packableIds.Contains(x.FulfillmentId))),
                AdminFulfillmentQueueFilters.ReadyToShip =>
                    source.Where(x =>
                        x.Status != FulfillmentStatus.Cancelled
                        && x.Status != FulfillmentStatus.Delivered
                        && x.Status != FulfillmentStatus.Failed
                        && (x.Status == FulfillmentStatus.Packed || shippableIds.Contains(x.FulfillmentId))),
                AdminFulfillmentQueueFilters.InTransit =>
                    source.Where(x =>
                        (x.Status == FulfillmentStatus.Dispatched || x.Status == FulfillmentStatus.InTransit)
                        && !remainingIds.Contains(x.FulfillmentId)),
                AdminFulfillmentQueueFilters.Delivered =>
                    source.Where(x => x.Status == FulfillmentStatus.Delivered),
                AdminFulfillmentQueueFilters.Problem =>
                    source.Where(x => x.Status == FulfillmentStatus.Failed),
                _ => source,
            };
        }

        if (key == AdminFulfillmentQueueFilters.MissingTracking)
        {
            var ids = await _db.Shipments.AsNoTracking()
                .Where(s => s.Status == ShipmentStatus.Created
                    && (s.TrackingReference == null || s.TrackingReference == ""))
                .Select(s => s.FulfillmentId)
                .Distinct()
                .ToListAsync(cancellationToken);
            return source.Where(x => ids.Contains(x.FulfillmentId));
        }

        if (key == AdminFulfillmentQueueFilters.NeedsAction)
        {
            var missingTrackingIds = await _db.Shipments.AsNoTracking()
                .Where(s => s.Status == ShipmentStatus.Created
                    && (s.TrackingReference == null || s.TrackingReference == ""))
                .Select(s => s.FulfillmentId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var remainingIds = await ItemFulfillmentIdsAsync(i => i.QuantityOrdered > i.QuantityShipped, cancellationToken);
            return source.Where(x =>
                x.Status == FulfillmentStatus.ReadyToFulfill
                || x.Status == FulfillmentStatus.Failed
                || x.Status == FulfillmentStatus.Processing
                || x.Status == FulfillmentStatus.Packed
                || missingTrackingIds.Contains(x.FulfillmentId)
                || (x.Status != FulfillmentStatus.Cancelled
                    && x.Status != FulfillmentStatus.Delivered
                    && remainingIds.Contains(x.FulfillmentId)));
        }

        return source;
    }

    private async Task<IQueryable<FulfillmentUnit>> ApplyStatusFilterAsync(
        IQueryable<FulfillmentUnit> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var wantPartial = values.Any(v => v.Equals(AdminFulfillmentQueueFilters.PartialDispatched, StringComparison.OrdinalIgnoreCase));
        var domainValues = values
            .Where(v => !v.Equals(AdminFulfillmentQueueFilters.PartialDispatched, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!wantPartial && domainValues.Count == 0)
        {
            return source.Where(_ => false);
        }

        var remainingIds = wantPartial || domainValues.Any(v => v.Equals(nameof(FulfillmentStatus.Dispatched), StringComparison.OrdinalIgnoreCase)
            || v.Equals(nameof(FulfillmentStatus.InTransit), StringComparison.OrdinalIgnoreCase))
            ? await ItemFulfillmentIdsAsync(i => i.QuantityOrdered > i.QuantityShipped, cancellationToken)
            : new List<Guid>();
        var parsed = domainValues
            .Select(v => Enum.TryParse<FulfillmentStatus>(v, true, out var e) ? (FulfillmentStatus?)e : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
        var wantDispatched = parsed.Contains(FulfillmentStatus.Dispatched);
        var wantInTransit = parsed.Contains(FulfillmentStatus.InTransit);
        var otherStatuses = parsed
            .Where(s => s is not FulfillmentStatus.Dispatched and not FulfillmentStatus.InTransit)
            .ToList();
        return source.Where(x =>
            (wantPartial
                && remainingIds.Contains(x.FulfillmentId)
                && (x.Status == FulfillmentStatus.Dispatched || x.Status == FulfillmentStatus.InTransit))
            || (wantDispatched && x.Status == FulfillmentStatus.Dispatched && !remainingIds.Contains(x.FulfillmentId))
            || (wantInTransit && x.Status == FulfillmentStatus.InTransit && !remainingIds.Contains(x.FulfillmentId))
            || otherStatuses.Contains(x.Status));
    }

    private Task<List<Guid>> ItemFulfillmentIdsAsync(
        System.Linq.Expressions.Expression<Func<FulfillmentItem, bool>> predicate,
        CancellationToken cancellationToken) =>
        _db.Items.AsNoTracking()
            .Where(predicate)
            .Select(x => x.FulfillmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

    private static List<Guid> ParseGuidValues(GridFilterRequest filter)
    {
        var values = new List<string>();
        if (filter.Values is { Count: > 0 })
        {
            values.AddRange(filter.Values);
        }
        else if (!string.IsNullOrWhiteSpace(filter.Value))
        {
            values.Add(filter.Value);
        }

        return values
            .Select(v => Guid.TryParse(v, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Default queue order is the dispatch priority (overdue earliest deadline first, then today, then
    /// upcoming, then unspecified, then in-transit) with a deterministic id tie-break. An explicit column
    /// sort still wins, because an operator who asked for a column must get that column.
    /// </summary>
    private static List<AdminFulfillmentWorkQueueRow> ApplySort(
        List<AdminFulfillmentWorkQueueRow> rows,
        GridQueryRequest request)
    {
        var sort = request.Sort.FirstOrDefault();
        if (sort is null || string.IsNullOrWhiteSpace(sort.Field))
        {
            return rows
                .OrderBy(row => AdminFulfillmentDispatchTimingAuthority.PriorityRank(ParseBucket(row.DispatchBucket)))
                .ThenBy(row => row.DispatchDeadlineUtc ?? DateTimeOffset.MaxValue)
                .ThenBy(row => row.PlannedHandoffDate ?? DateOnly.MaxValue)
                // TB-260-R1: the physical work-unit key is the tie-break, so two parcels of one seller order can
                // never swap places (or collapse) between two identical pages.
                .ThenBy(row => row.WorkUnitKey, StringComparer.Ordinal)
                .ToList();
        }

        var asc = sort.Direction == "asc";
        IOrderedEnumerable<AdminFulfillmentWorkQueueRow> ordered = sort.Field switch
        {
            "fulfillmentId" => Order(rows, row => row.FulfillmentId, asc),
            "checkoutId" => Order(rows, row => row.CheckoutId, asc),
            "orderReference" => Order(rows, row => row.SellerOrderId, asc),
            "cityName" => Order(rows, row => row.CityName, asc),
            "status" => Order(rows, row => row.Status, asc),
            "shippingMethodCode" or "shippingMethodLabel" => Order(rows, row => row.ShippingMethodLabel, asc),
            "sellerPartyId" or "sellerDisplayName" => Order(rows, row => row.SellerPartyId, asc),
            "recipientName" => Order(rows, row => row.RecipientName, asc),
            "shipmentCount" => Order(rows, row => row.ShipmentCount, asc),
            "createdAt" => Order(rows, row => row.CreatedAt, asc),
            "updatedAt" => Order(rows, row => row.UpdatedAt, asc),
            "dispatchDeadline" => Order(rows, row => row.DispatchDeadlineUtc ?? DateTimeOffset.MaxValue, asc),
            "dispatchBucket" => Order(
                rows,
                row => AdminFulfillmentDispatchTimingAuthority.PriorityRank(ParseBucket(row.DispatchBucket)),
                asc),
            _ => Order(rows, row => row.UpdatedAt, asc),
        };
        return ordered.ThenBy(row => row.WorkUnitKey, StringComparer.Ordinal).ToList();
    }

    private static IOrderedEnumerable<AdminFulfillmentWorkQueueRow> Order<TKey>(
        IEnumerable<AdminFulfillmentWorkQueueRow> rows,
        Func<AdminFulfillmentWorkQueueRow, TKey> key,
        bool asc) =>
        asc ? rows.OrderBy(key) : rows.OrderByDescending(key);

    private static AdminFulfillmentDispatchBucket ParseBucket(string value) =>
        Enum.TryParse<AdminFulfillmentDispatchBucket>(value, true, out var bucket)
            ? bucket
            : AdminFulfillmentDispatchBucket.NotQueued;

    /// <summary>
    /// TB-260-R1: loads the physical facts of the filtered units — plus the COMPLETE member set of any active
    /// common package that touches them — and projects exactly ONE row per physical work unit.
    /// </summary>
    /// <remarks>
    /// The unit set answers <i>which</i> work is in scope; it never becomes the row identity. Two parcels of one
    /// seller order are two rows with two locators, and a common package is one row for all of its locked members.
    /// No plan is fetched per row: the durable Order plan is bound to its own accepted group and handed to the
    /// single timing authority together with that unit's own physical state.
    /// </remarks>
    private async Task<IReadOnlyList<AdminFulfillmentWorkQueueRow>> MapPageAsync(
        List<FulfillmentUnit> rows,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.FulfillmentId).ToList();

        var items = await _db.Items.AsNoTracking()
            .Where(x => ids.Contains(x.FulfillmentId))
            .ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.AsNoTracking()
            .Where(x => ids.Contains(x.FulfillmentId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var shipmentIds = shipments.Select(x => x.ShipmentId).ToList();

        // A package row must describe the whole physical package: membership is resolved for the loaded parcels,
        // then every ACTIVE member of those packages is loaded even when its seller order did not match the
        // filter. Otherwise one package would be split into partial rows (and inflated counts).
        var membership = shipmentIds.Count == 0
            ? []
            : await _db.ConsolidatedPackageMembers.AsNoTracking()
                .Where(x => shipmentIds.Contains(x.ShipmentId) && x.ReleasedAt == null)
                .ToListAsync(cancellationToken);
        var packageIds = membership.Select(x => x.ConsolidatedPackageId).Distinct().ToList();
        var packageHeaders = packageIds.Count == 0
            ? []
            : await _db.ConsolidatedPackages.AsNoTracking()
                .Where(x => packageIds.Contains(x.ConsolidatedPackageId)
                    && x.Status != ConsolidatedPackageStatus.Cancelled)
                .OrderBy(x => x.PackageNumber)
                .ToListAsync(cancellationToken);
        var activePackageIds = packageHeaders.Select(x => x.ConsolidatedPackageId).ToHashSet();
        var packageMembers = activePackageIds.Count == 0
            ? []
            : await _db.ConsolidatedPackageMembers.AsNoTracking()
                .Where(x => activePackageIds.Contains(x.ConsolidatedPackageId) && x.ReleasedAt == null)
                .ToListAsync(cancellationToken);

        var memberShipmentIds = packageMembers.Select(x => x.ShipmentId).Distinct().ToList();
        var memberFulfillmentIds = packageMembers.Select(x => x.FulfillmentId).Distinct().ToList();
        var extraShipments = memberShipmentIds.Count == 0
            ? []
            : await _db.Shipments.AsNoTracking()
                .Where(x => memberShipmentIds.Contains(x.ShipmentId))
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        var extraUnits = memberFulfillmentIds.Count == 0
            ? []
            : await _db.Fulfillments.AsNoTracking()
                .Where(x => memberFulfillmentIds.Contains(x.FulfillmentId))
                .ToListAsync(cancellationToken);

        var allShipments = shipments
            .Concat(extraShipments)
            .GroupBy(x => x.ShipmentId)
            .Select(g => g.First())
            .ToList();
        var allShipmentIds = allShipments.Select(x => x.ShipmentId).ToList();
        var shipmentItems = allShipmentIds.Count == 0
            ? []
            : await _db.ShipmentItems.AsNoTracking()
                .Where(x => allShipmentIds.Contains(x.ShipmentId))
                .ToListAsync(cancellationToken);
        var shipmentItemsBy = shipmentItems.GroupBy(x => x.ShipmentId).ToDictionary(g => g.Key, g => g.ToList());

        // TB-258: a resend parcel is only offered when its OWN source parcel still has received-usable capacity
        // that no live resend has consumed (same rule as the owning aggregate, read-only here).
        var returnReceipts = allShipments.Any(x => x.Status == ShipmentStatus.Returned)
            ? await _db.ShipmentReturnReceipts.AsNoTracking()
                .Where(x => allShipmentIds.Contains(x.ShipmentId))
                .ToListAsync(cancellationToken)
            : [];
        var returnReceiptIds = returnReceipts.Select(x => x.ShipmentReturnReceiptId).ToList();
        var returnReceiptItems = returnReceiptIds.Count == 0
            ? []
            : await _db.ShipmentReturnReceiptItems.AsNoTracking()
                .Where(x => returnReceiptIds.Contains(x.ShipmentReturnReceiptId))
                .ToListAsync(cancellationToken);
        var resendableBySourceLine = BuildResendableCapacity(allShipments, returnReceipts, returnReceiptItems, shipmentItemsBy);

        var unitById = rows.ToDictionary(x => x.FulfillmentId);
        foreach (var extra in extraUnits.Where(x => !unitById.ContainsKey(x.FulfillmentId)))
        {
            unitById[extra.FulfillmentId] = extra;
        }

        var sellerOrderIds = unitById.Values.Select(x => x.SellerOrderId).Distinct().ToList();
        var sellerIds = rows.Select(x => x.SellerPartyId)
            .Concat(extraUnits.Select(x => x.SellerPartyId))
            .Distinct()
            .ToList();
        var sellerNameBy = await _parties.GetDisplayNamesAsync(sellerIds, cancellationToken);
        var orderNumberBy = await _orders.GetOrderNumbersAsync(sellerOrderIds, cancellationToken);

        var planFacts = await _dispatchPlans.ListPlannedForSellerOrdersAsync(sellerOrderIds, cancellationToken);
        var plansByGroup = planFacts
            .GroupBy(plan => (plan.SellerOrderId, plan.ShippingGroupId))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>)group.ToList());

        var itemsBy = items.GroupBy(x => x.FulfillmentId).ToDictionary(g => g.Key, g => g.ToList());
        var shipmentSnapshotsBy = allShipments
            .GroupBy(x => x.FulfillmentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ShipmentSnapshot>)g
                    .OrderBy(x => x.CreatedAt)
                    .Select(shipment => ToShipmentSnapshot(
                        shipment,
                        shipmentItemsBy.TryGetValue(shipment.ShipmentId, out var lines) ? lines : []))
                    .ToList());

        var units = rows.Select(unit =>
        {
            itemsBy.TryGetValue(unit.FulfillmentId, out var itemList);
            shipmentSnapshotsBy.TryGetValue(unit.FulfillmentId, out var shipmentSnapshots);
            return ToSnapshot(unit, itemList ?? [], shipmentSnapshots ?? []);
        }).ToList();

        var packages = new Dictionary<Guid, AdminFulfillmentPackageWorkFacts>();
        foreach (var header in packageHeaders)
        {
            var members = new List<AdminFulfillmentPackageMemberFacts>();
            foreach (var member in packageMembers.Where(x => x.ConsolidatedPackageId == header.ConsolidatedPackageId))
            {
                var shipment = allShipments.FirstOrDefault(x => x.ShipmentId == member.ShipmentId);
                if (shipment is null)
                {
                    continue;
                }

                shipmentItemsBy.TryGetValue(shipment.ShipmentId, out var lines);
                lines ??= [];
                unitById.TryGetValue(member.FulfillmentId, out var memberUnit);
                members.Add(new AdminFulfillmentPackageMemberFacts(
                    member.ShipmentId,
                    member.FulfillmentId,
                    member.SellerPartyId,
                    memberUnit?.SellerOrderId ?? Guid.Empty,
                    shipment.ShippingGroupId,
                    shipment.Status,
                    lines.Select(x => x.OrderLineId).Distinct().Count(),
                    lines.Sum(x => x.Quantity)));
            }

            if (members.Count == 0)
            {
                continue;
            }

            packages[header.ConsolidatedPackageId] = new AdminFulfillmentPackageWorkFacts(
                header.ConsolidatedPackageId,
                header.PackageNumber,
                header.CheckoutId,
                header.Status,
                header.ShippingMethodCode,
                header.ShippingMethodLabel,
                header.TrackingReference,
                header.CreatedAt,
                header.UpdatedAt,
                header.DispatchedAt,
                members);
        }

        var packageIdByShipment = packageMembers
            .Where(x => activePackageIds.Contains(x.ConsolidatedPackageId))
            .GroupBy(x => x.ShipmentId)
            .ToDictionary(g => g.Key, g => g.First().ConsolidatedPackageId);

        return AdminFulfillmentWorkUnitProjection.Build(new AdminFulfillmentWorkUnitContext(
            units,
            plansByGroup,
            packages,
            packageIdByShipment,
            resendableBySourceLine,
            sellerNameBy,
            orderNumberBy,
            nowUtc));
    }

    private static ShipmentSnapshot ToShipmentSnapshot(Shipment shipment, IReadOnlyList<ShipmentItem> lines) =>
        new(
            shipment.ShipmentId,
            shipment.Status,
            shipment.CarrierDisplayName,
            shipment.TrackingReference,
            shipment.DispatchedAt,
            shipment.DeliveredAt,
            lines.Select(x => new ShipmentLineSnapshot(x.OrderLineId, x.Quantity)).ToArray(),
            shipment.CreatedAt,
            shipment.ShippingMethodCode,
            shipment.ShippingMethodLabel,
            shipment.ProviderMetadataJson,
            shipment.ProviderMetadataVersion,
            shipment.PreviousTrackingReference,
            shipment.ShippingGroupId,
            shipment.ShippingDispatchKey,
            shipment.ResendOfShipmentId,
            shipment.NonDeliveryReason,
            shipment.NonDeliveryNote,
            shipment.NonDeliveredAt,
            shipment.ReturnStartedAt,
            shipment.ReturnedAt);

    private static FulfillmentSnapshot ToSnapshot(
        FulfillmentUnit unit,
        IReadOnlyList<FulfillmentItem> itemList,
        IReadOnlyList<ShipmentSnapshot> shipmentSnapshots) =>
        new(
            unit.FulfillmentId,
            unit.SellerOrderId,
            unit.CheckoutId,
            unit.SellerPartyId,
            unit.Status,
            unit.RecipientName,
            unit.ContactMobile,
            unit.ProvinceName,
            unit.CityName,
            unit.PostalAddress,
            unit.PostalCode,
            unit.ShippingMethodCode,
            unit.ShippingMethodLabel,
            itemList.Select(x => new FulfillmentItemSnapshot(
                x.FulfillmentItemId,
                x.OrderLineId,
                x.QuantityOrdered,
                x.QuantityShipped,
                x.ReservationId,
                x.QuantityPacked,
                x.QuantityProcessing,
                x.ShippingGroupId)).ToArray(),
            shipmentSnapshots,
            unit.CreatedAt,
            unit.UpdatedAt);

    /// <summary>
    /// (source parcel, order line) → still resendable quantity: physically received USABLE units minus what live
    /// resend parcels already committed. Mirrors the owning TB-258 quantities without writing or re-deriving money.
    /// </summary>
    private static Dictionary<(Guid ShipmentId, Guid OrderLineId), decimal> BuildResendableCapacity(
        IReadOnlyList<Shipment> shipments,
        IReadOnlyList<ShipmentReturnReceipt> receipts,
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        IReadOnlyDictionary<Guid, List<ShipmentItem>> shipmentItemsBy)
    {
        var usableByLine = new Dictionary<(Guid, Guid), decimal>();
        var receiptSource = receipts.ToDictionary(x => x.ShipmentReturnReceiptId, x => x.ShipmentId);
        foreach (var item in receiptItems)
        {
            if (item.Condition != ReturnReceiptCondition.Usable
                || !receiptSource.TryGetValue(item.ShipmentReturnReceiptId, out var sourceShipmentId))
            {
                continue;
            }

            var key = (sourceShipmentId, item.OrderLineId);
            usableByLine[key] = (usableByLine.TryGetValue(key, out var current) ? current : 0m) + item.Quantity;
        }

        foreach (var resend in shipments.Where(x => x.ResendOfShipmentId is not null && x.Status != ShipmentStatus.Cancelled))
        {
            if (!shipmentItemsBy.TryGetValue(resend.ShipmentId, out var lines))
            {
                continue;
            }

            foreach (var line in lines)
            {
                var key = (resend.ResendOfShipmentId!.Value, line.OrderLineId);
                if (usableByLine.TryGetValue(key, out var usable))
                {
                    usableByLine[key] = usable - line.Quantity;
                }
            }
        }

        return usableByLine;
    }
}
