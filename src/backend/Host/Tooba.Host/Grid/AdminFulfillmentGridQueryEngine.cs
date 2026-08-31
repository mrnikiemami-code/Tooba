using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native گرید fulfillment Admin با batch map آیتم/shipment.</summary>
internal sealed class AdminFulfillmentGridQueryEngine
{
    private readonly FulfillmentDbContext _db;

    public AdminFulfillmentGridQueryEngine(FulfillmentDbContext db) => _db = db;

    public async Task<GridPageResponse<FulfillmentSnapshot>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<FulfillmentUnit> q = _db.Fulfillments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            q = q.Where(x =>
                x.RecipientName.ToLower().Contains(term)
                || x.FulfillmentId.ToString().ToLower().Contains(term)
                || x.CheckoutId.ToString().ToLower().Contains(term)
                || x.CityName.ToLower().Contains(term));
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.FulfillmentId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("recipientName", "asc");
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
            var ids = await ApplyFilter(_db.Fulfillments.AsNoTracking(), filter)
                .Select(x => x.FulfillmentId)
                .ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private IQueryable<FulfillmentUnit> ApplyFilter(IQueryable<FulfillmentUnit> source, GridFilterRequest filter)
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
            default:
                return source;
        }
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
            "cityName" => asc
                ? source.OrderBy(x => x.CityName).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.CityName).ThenBy(x => x.FulfillmentId),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.FulfillmentId),
            _ => asc
                ? source.OrderBy(x => x.RecipientName).ThenBy(x => x.FulfillmentId)
                : source.OrderByDescending(x => x.RecipientName).ThenBy(x => x.FulfillmentId),
        };
    }

    private async Task<IReadOnlyList<FulfillmentSnapshot>> MapPageAsync(
        List<FulfillmentUnit> rows,
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
        var shipmentItems = shipmentIds.Count == 0
            ? []
            : await _db.ShipmentItems.AsNoTracking()
                .Where(x => shipmentIds.Contains(x.ShipmentId))
                .ToListAsync(cancellationToken);
        var itemsBy = items.GroupBy(x => x.FulfillmentId).ToDictionary(g => g.Key, g => g.ToList());
        var shipmentsBy = shipments.GroupBy(x => x.FulfillmentId).ToDictionary(g => g.Key, g => g.ToList());
        var shipmentItemsBy = shipmentItems.GroupBy(x => x.ShipmentId).ToDictionary(g => g.Key, g => g.ToList());

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
                    lines.Select(x => new ShipmentLineSnapshot(x.OrderLineId, x.Quantity)).ToArray());
            }).ToArray();
            return new FulfillmentSnapshot(
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
                    x.ReservationId)).ToArray(),
                shipmentSnapshots);
        }).ToList();
    }
}
