using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native صف کار ارسال و تحویل Admin با batch map و بدون N+1.</summary>
public sealed class AdminFulfillmentWorkQueueQueryEngine
{
    private readonly FulfillmentDbContext _db;
    private readonly PartyDbContext _parties;
    private readonly OrderDbContext _orders;

    /// <summary>موتور صف کار را به DbContextها وصل می‌کند.</summary>
    public AdminFulfillmentWorkQueueQueryEngine(
        FulfillmentDbContext db,
        PartyDbContext parties,
        OrderDbContext orders)
    {
        _db = db;
        _parties = parties;
        _orders = orders;
    }

    /// <summary>صفحه‌بندی و فیلتر صف کار را اجرا می‌کند.</summary>
    public async Task<GridPageResponse<AdminFulfillmentWorkQueueRow>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<FulfillmentUnit> q = _db.Fulfillments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var sellerIds = await _parties.Parties.AsNoTracking()
                .Where(p => p.DisplayName.ToLower().Contains(term))
                .Select(p => p.PartyId)
                .Take(200)
                .ToListAsync(cancellationToken);
            var matchingOrderIds = await _orders.SellerOrders.AsNoTracking()
                .Where(o => o.OrderNumber.ToLower().Contains(term))
                .Select(o => o.SellerOrderId)
                .Take(200)
                .ToListAsync(cancellationToken);
            q = q.Where(x =>
                x.RecipientName.ToLower().Contains(term)
                || x.FulfillmentId.ToString().ToLower().Contains(term)
                || x.CheckoutId.ToString().ToLower().Contains(term)
                || x.CityName.ToLower().Contains(term)
                || x.ShippingMethodLabel.ToLower().Contains(term)
                || sellerIds.Contains(x.SellerPartyId)
                || matchingOrderIds.Contains(x.SellerOrderId));
        }

        foreach (var filter in request.Filters)
        {
            q = await ApplyFilterAsync(q, filter, cancellationToken);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.FulfillmentId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("updatedAt", "desc");
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
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.RecipientName, filter);
            case "fulfillmentId":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.FulfillmentId.ToString(), filter);
            case "checkoutId":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.CheckoutId.ToString(), filter);
            case "cityName":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.CityName, filter);
            case "orderReference":
            {
                var orders = _orders.SellerOrders.AsNoTracking().AsQueryable();
                orders = AdminEfGridQuery.ApplyTextFilter(orders, x => x.OrderNumber, filter);
                var sellerOrderIds = await orders.Select(x => x.SellerOrderId).Take(500).ToListAsync(cancellationToken);
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
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.ShippingMethodLabel, filter);
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
                var nameQ = _parties.Parties.AsNoTracking().AsQueryable();
                nameQ = AdminEfGridQuery.ApplyTextFilter(nameQ, x => x.DisplayName, filter);
                var partyIds = await nameQ.Select(x => x.PartyId).Take(500).ToListAsync(cancellationToken);
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
                joined = AdminEfGridQuery.ApplyIntFilter(joined, x => x.Count, filter);
                return joined.Select(x => x.Unit);
            }
            case "status":
                return AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
            case "queueFilter":
                return await ApplyQueueFilterAsync(source, filter, cancellationToken);
            case "createdAt":
                return AdminEfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter);
            case "updatedAt":
                return AdminEfGridQuery.ApplyDateFilter(source, x => x.UpdatedAt, filter);
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
            return key switch
            {
                AdminFulfillmentQueueFilters.ReadyToProcess =>
                    source.Where(x => x.Status == FulfillmentStatus.ReadyToFulfill),
                AdminFulfillmentQueueFilters.ReadyToPack =>
                    source.Where(x => x.Status == FulfillmentStatus.Processing),
                AdminFulfillmentQueueFilters.ReadyToShip =>
                    source.Where(x => x.Status == FulfillmentStatus.Packed),
                AdminFulfillmentQueueFilters.InTransit =>
                    source.Where(x => x.Status == FulfillmentStatus.Dispatched || x.Status == FulfillmentStatus.InTransit),
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
            return source.Where(x =>
                x.Status == FulfillmentStatus.ReadyToFulfill
                || x.Status == FulfillmentStatus.Failed
                || x.Status == FulfillmentStatus.Processing
                || x.Status == FulfillmentStatus.Packed
                || missingTrackingIds.Contains(x.FulfillmentId));
        }

        return source;
    }

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

    private IQueryable<FulfillmentUnit> Order(IQueryable<FulfillmentUnit> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        if (sort.Field == "shipmentCount")
        {
            var counts = _db.Shipments.AsNoTracking()
                .GroupBy(x => x.FulfillmentId)
                .Select(g => new { FulfillmentId = g.Key, Count = g.Count() });
            var joined = from u in source
                         join c in counts on u.FulfillmentId equals c.FulfillmentId into cj
                         from c in cj.DefaultIfEmpty()
                         select new { Unit = u, Count = c != null ? c.Count : 0 };
            var ordered = asc
                ? joined.OrderBy(x => x.Count).ThenBy(x => x.Unit.FulfillmentId)
                : joined.OrderByDescending(x => x.Count).ThenBy(x => x.Unit.FulfillmentId);
            return ordered.Select(x => x.Unit);
        }

        return sort.Field switch
        {
            "fulfillmentId" => asc
                ? source.OrderBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.FulfillmentId),
            "checkoutId" => asc
                ? source.OrderBy(x => x.CheckoutId).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.CheckoutId).ThenBy(x => x.FulfillmentId),
            "orderReference" => OrderByOrderNumber(source, asc),
            "cityName" => asc
                ? source.OrderBy(x => x.CityName).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.CityName).ThenBy(x => x.FulfillmentId),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.FulfillmentId),
            "shippingMethodCode" or "shippingMethodLabel" => asc
                ? source.OrderBy(x => x.ShippingMethodLabel).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.ShippingMethodLabel).ThenBy(x => x.FulfillmentId),
            "sellerPartyId" or "sellerDisplayName" => asc
                ? source.OrderBy(x => x.SellerPartyId).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.SellerPartyId).ThenBy(x => x.FulfillmentId),
            "recipientName" => asc
                ? source.OrderBy(x => x.RecipientName).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.RecipientName).ThenBy(x => x.FulfillmentId),
            "createdAt" => asc
                ? source.OrderBy(x => x.CreatedAt).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.FulfillmentId),
            "updatedAt" => asc
                ? source.OrderBy(x => x.UpdatedAt).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.FulfillmentId),
            _ => asc
                ? source.OrderBy(x => x.UpdatedAt).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.FulfillmentId),
        };
    }

    private IQueryable<FulfillmentUnit> OrderByOrderNumber(IQueryable<FulfillmentUnit> source, bool asc)
    {
        var numbers = _orders.SellerOrders.AsNoTracking()
            .Select(x => new { x.SellerOrderId, x.OrderNumber });
        var joined = from u in source
                     join n in numbers on u.SellerOrderId equals n.SellerOrderId into nj
                     from n in nj.DefaultIfEmpty()
                     select new { Unit = u, OrderNumber = n != null ? n.OrderNumber : "" };
        var ordered = asc
            ? joined.OrderBy(x => x.OrderNumber).ThenBy(x => x.Unit.FulfillmentId)
            : joined.OrderByDescending(x => x.OrderNumber).ThenBy(x => x.Unit.FulfillmentId);
        return ordered.Select(x => x.Unit);
    }

    private async Task<IReadOnlyList<AdminFulfillmentWorkQueueRow>> MapPageAsync(
        List<FulfillmentUnit> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.FulfillmentId).ToList();
        var sellerIds = rows.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerOrderIds = rows.Select(x => x.SellerOrderId).Distinct().ToList();

        var items = await _db.Items.AsNoTracking()
            .Where(x => ids.Contains(x.FulfillmentId))
            .ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.AsNoTracking()
            .Where(x => ids.Contains(x.FulfillmentId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var shipmentIds = shipments.Select(x => x.ShipmentId).ToList();
        var shipmentItems = shipmentIds.Count == 0
            ? []
            : await _db.ShipmentItems.AsNoTracking()
                .Where(x => shipmentIds.Contains(x.ShipmentId))
                .ToListAsync(cancellationToken);
        var sellerNames = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName })
            .ToListAsync(cancellationToken);
        var orderNumbers = await _orders.SellerOrders.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .Select(x => new { x.SellerOrderId, x.OrderNumber })
            .ToListAsync(cancellationToken);

        var itemsBy = items.GroupBy(x => x.FulfillmentId).ToDictionary(g => g.Key, g => g.ToList());
        var shipmentsBy = shipments.GroupBy(x => x.FulfillmentId).ToDictionary(g => g.Key, g => g.ToList());
        var shipmentItemsBy = shipmentItems.GroupBy(x => x.ShipmentId).ToDictionary(g => g.Key, g => g.ToList());
        var sellerNameBy = sellerNames.ToDictionary(x => x.PartyId, x => x.DisplayName);
        var orderNumberBy = orderNumbers.ToDictionary(x => x.SellerOrderId, x => x.OrderNumber);

        return rows.Select(unit =>
        {
            itemsBy.TryGetValue(unit.FulfillmentId, out var itemList);
            shipmentsBy.TryGetValue(unit.FulfillmentId, out var shipmentList);
            itemList ??= [];
            shipmentList ??= [];
            var shipmentSnapshots = shipmentList.Select(shipment =>
            {
                shipmentItemsBy.TryGetValue(shipment.ShipmentId, out var lines);
                lines ??= [];
                return new ShipmentSnapshot(
                    shipment.ShipmentId,
                    shipment.Status,
                    shipment.CarrierDisplayName,
                    shipment.TrackingReference,
                    shipment.DispatchedAt,
                    shipment.DeliveredAt,
                    lines.Select(x => new ShipmentLineSnapshot(x.OrderLineId, x.Quantity)).ToArray(),
                    shipment.CreatedAt,
                    shipment.ShippingMethodCode,
                    shipment.ShippingMethodLabel);
            }).ToArray();
            var itemSnapshots = itemList.Select(x => new FulfillmentItemSnapshot(
                x.FulfillmentItemId,
                x.OrderLineId,
                x.QuantityOrdered,
                x.QuantityShipped,
                x.ReservationId,
                x.QuantityPacked,
                x.QuantityProcessing)).ToArray();
            var snapshot = new FulfillmentSnapshot(
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
                itemSnapshots,
                shipmentSnapshots,
                unit.CreatedAt,
                unit.UpdatedAt);
            var tracking = shipmentSnapshots
                .Select(s => s.TrackingReference)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Cast<string>()
                .ToArray();
            var primaryShipment = shipmentSnapshots
                .OrderByDescending(s => s.Status == ShipmentStatus.Created)
                .ThenByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            var methodLabel = ShippingMethodRegistry.ResolveLabel(
                unit.ShippingMethodCode,
                string.IsNullOrWhiteSpace(unit.ShippingMethodLabel) ? null : unit.ShippingMethodLabel);
            orderNumberBy.TryGetValue(unit.SellerOrderId, out var orderNumber);
            var orderReference = !string.IsNullOrWhiteSpace(orderNumber)
                ? orderNumber!
                : string.Empty;
            sellerNameBy.TryGetValue(unit.SellerPartyId, out var sellerName);
            return new AdminFulfillmentWorkQueueRow(
                unit.FulfillmentId,
                unit.SellerOrderId,
                unit.CheckoutId,
                unit.SellerPartyId,
                string.IsNullOrWhiteSpace(sellerName) ? "فروشنده" : sellerName,
                orderReference,
                unit.Status.ToString(),
                unit.RecipientName,
                unit.CityName,
                unit.ShippingMethodCode,
                methodLabel,
                itemSnapshots.Length,
                itemSnapshots.Sum(x => x.QuantityOrdered),
                itemSnapshots.Sum(x => x.QuantityShipped),
                shipmentSnapshots.Length,
                primaryShipment?.ShipmentId.ToString(),
                tracking.Length == 0 ? "" : string.Join(" · ", tracking),
                unit.CreatedAt,
                unit.UpdatedAt,
                AdminFulfillmentQueueFilters.ProjectActionCodes(snapshot));
        }).ToList();
    }
}
