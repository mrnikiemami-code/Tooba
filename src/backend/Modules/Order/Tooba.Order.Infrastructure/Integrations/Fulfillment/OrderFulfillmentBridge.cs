using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Fulfillment;

/// <summary>
/// snapshot handoff سفارش برای Fulfillment بدون cross-DbContext.
/// </summary>
public sealed class OrderFulfillmentBridge : IOrderFulfillmentReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل handoff را به schema order وصل می‌کند.</summary>
    public OrderFulfillmentBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var sellerOrder = await _db.SellerOrders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken);
        if (sellerOrder is null)
        {
            return null;
        }

        var checkout = await _db.Checkouts.AsNoTracking()
            .SingleAsync(x => x.CheckoutId == sellerOrder.CheckoutId, cancellationToken);
        var lines = await _db.Lines.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId)
            .ToListAsync(cancellationToken);
        var groups = await LoadGroupsAsync(sellerOrderId, cancellationToken);
        return Map(checkout, sellerOrder, lines, groups);
    }

    /// <inheritdoc />
    public async Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var checkout = await _db.Checkouts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (checkout is null || checkout.PlacedByUserId != actorUserId)
        {
            return null;
        }

        var sellerOrder = await _db.SellerOrders.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderBy(x => x.SellerOrderId)
            .FirstOrDefaultAsync(cancellationToken);
        if (sellerOrder is null)
        {
            return null;
        }

        var lines = await _db.Lines.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrder.SellerOrderId)
            .ToListAsync(cancellationToken);
        var groups = await LoadGroupsAsync(sellerOrder.SellerOrderId, cancellationToken);
        return Map(checkout, sellerOrder, lines, groups);
    }

    private async Task<IReadOnlyList<OrderFulfillmentShippingGroupSnapshot>> LoadGroupsAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var groups = await _db.OrderShippingGroups.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId)
            .OrderBy(x => x.ShippingGroupId)
            .ToListAsync(cancellationToken);
        if (groups.Count == 0)
        {
            return [];
        }

        var groupIds = groups.Select(x => x.ShippingGroupId).ToList();
        var memberships = await _db.OrderShippingGroupLines.AsNoTracking()
            .Where(x => groupIds.Contains(x.ShippingGroupId))
            .ToListAsync(cancellationToken);

        return groups
            .Select(group => new OrderFulfillmentShippingGroupSnapshot(
                group.ShippingGroupId,
                group.SellerOrderId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.MethodLabel,
                group.OriginContext,
                group.MinimumDeliveryDate,
                group.RequestedDeliveryDate,
                group.RequestedDeliveryTimeWindow,
                group.LeadDays,
                memberships
                    .Where(x => x.ShippingGroupId == group.ShippingGroupId)
                    .Select(x => x.OrderLineId)
                    .ToArray()))
            .ToArray();
    }

    private static OrderFulfillmentHandoffSnapshot Map(
        CheckoutGroup checkout,
        SellerOrder sellerOrder,
        IReadOnlyList<OrderLine> lines,
        IReadOnlyList<OrderFulfillmentShippingGroupSnapshot> groups) =>
        new(
            sellerOrder.SellerOrderId,
            checkout.CheckoutId,
            sellerOrder.SellerPartyId,
            checkout.PlacedByUserId,
            sellerOrder.Status == SellerOrderStatus.Paid,
            checkout.RecipientName,
            checkout.ContactMobile,
            checkout.ProvinceName,
            checkout.CityName,
            checkout.PostalAddress,
            checkout.PostalCode,
            checkout.ShippingMethodCode,
            checkout.ShippingMethodLabel,
            lines.Select(x => new OrderFulfillmentLineSnapshot(x.LineId, x.Quantity, x.ReservationId)).ToArray(),
            groups.Count == 0 ? null : groups);
}
