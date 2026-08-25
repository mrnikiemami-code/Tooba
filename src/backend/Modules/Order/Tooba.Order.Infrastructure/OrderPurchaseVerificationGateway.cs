using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>اثبات خرید فقط با schema سفارش و ورودی شناسهٔ گونه‌های Catalog انجام می‌شود.</summary>
public sealed class OrderPurchaseVerificationGateway : IOrderPurchaseVerificationGateway
{
    private readonly OrderDbContext _db;

    /// <summary>درگاه را به DbContext مالک Order وصل می‌کند.</summary>
    public OrderPurchaseVerificationGateway(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<OrderPurchaseVerification> VerifyPaidPurchaseAsync(
        Guid actorUserId,
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || catalogVariantIds.Count == 0)
        {
            return OrderPurchaseVerification.NotVerified;
        }

        var orderId = await (
            from checkout in _db.Checkouts.AsNoTracking()
            join order in _db.SellerOrders.AsNoTracking() on checkout.CheckoutId equals order.CheckoutId
            join line in _db.Lines.AsNoTracking() on order.SellerOrderId equals line.SellerOrderId
            where checkout.PlacedByUserId == actorUserId
                  && order.Status == SellerOrderStatus.Paid
                  && catalogVariantIds.Contains(line.CatalogVariantId)
            orderby order.SellerOrderId
            select (Guid?)order.SellerOrderId).FirstOrDefaultAsync(cancellationToken);

        return orderId is null
            ? OrderPurchaseVerification.NotVerified
            : new OrderPurchaseVerification(true, orderId);
    }
}
