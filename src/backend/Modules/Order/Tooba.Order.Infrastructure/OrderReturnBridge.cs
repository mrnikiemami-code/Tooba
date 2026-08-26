using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// snapshot سفارش برای Returns بدون cross-DbContext.
/// </summary>
public sealed class OrderReturnBridge : IOrderReturnReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل return را به schema order وصل می‌کند.</summary>
    public OrderReturnBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
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

    private static OrderReturnContextSnapshot Map(
        CheckoutGroup checkout,
        SellerOrder sellerOrder,
        IReadOnlyList<OrderLine> lines) =>
        new(
            sellerOrder.SellerOrderId,
            checkout.CheckoutId,
            sellerOrder.SellerPartyId,
            checkout.PlacedByUserId,
            sellerOrder.Status == SellerOrderStatus.Paid,
            sellerOrder.Currency,
            lines.Select(x => new OrderReturnLineSnapshot(
                x.LineId,
                x.Quantity,
                x.UnitPriceSnapshot,
                x.Currency,
                x.ReservationId)).ToArray());
}
