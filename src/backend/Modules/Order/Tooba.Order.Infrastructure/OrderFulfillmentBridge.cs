using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

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
        return Map(checkout, sellerOrder, lines);
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
        return Map(checkout, sellerOrder, lines);
    }

    private static OrderFulfillmentHandoffSnapshot Map(
        CheckoutGroup checkout,
        SellerOrder sellerOrder,
        IReadOnlyList<OrderLine> lines) =>
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
            lines.Select(x => new OrderFulfillmentLineSnapshot(x.LineId, x.Quantity, x.ReservationId)).ToArray());
}
