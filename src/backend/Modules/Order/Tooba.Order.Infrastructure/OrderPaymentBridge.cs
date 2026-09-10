using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// درز سفارش برای پرداخت. DbContext پرداخت اینجا باز نمی‌شود و مبلغ از کلاینت خوانده نمی‌شود.
/// تصویر Paid فقط از مصرف رویداد پایدار نوشته می‌شود، نه از تراکنش همزمان Payment.
/// </summary>
public sealed class OrderPaymentBridge : IPayableCheckoutReader, IOrderPaymentProjection
{
    private readonly OrderDbContext _db;
    private readonly IInventoryDirectory _inventory;

    /// <summary>
    /// پل را به schema order و قرارداد Inventory وصل می‌کند.
    /// </summary>
    public OrderPaymentBridge(OrderDbContext db, IInventoryDirectory inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    /// <inheritdoc />
    public async Task<PayableCheckoutSnapshot?> GetPayableAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        if (!group.CanBeViewedBy(buyerPartyId, actorUserId))
        {
            throw new InvalidOperationException("دسترسی به تصویر قابل‌پرداخت بدون هویت مجاز رد شد.");
        }

        return new PayableCheckoutSnapshot(
            group.CheckoutId,
            group.Mode == OrderMode.OnlinePurchase ? OrderPaymentMode.OnlinePurchase : OrderPaymentMode.RequestToReserve,
            group.Currency,
            group.SellerOrders.Select(order => new PayableSellerOrderSnapshot(
                order.SellerOrderId,
                order.GrandTotalSnapshot,
                order.Currency,
                order.Status == SellerOrderStatus.PendingPayment)).ToArray(),
            group.ShippingAmount);
    }

    /// <inheritdoc />
    public async Task ApplyVerifiedSuccessAsync(
        Guid checkoutId,
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        _ = paymentId;
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("checkout برای تصویر پرداخت پیدا نشد.");

        foreach (var order in group.SellerOrders.Where(x => sellerOrderIds.Contains(x.SellerOrderId)))
        {
            if (group.Mode != OrderMode.OnlinePurchase)
            {
                throw new InvalidOperationException("درخواست رزرو با موفقیت درگاه Paid نمی‌شود.");
            }

            order.RecordVerifiedPayment();
            foreach (var line in order.Lines)
            {
                if (line.ReservationId is not { } reservationId)
                {
                    continue;
                }

                await _inventory.CommitReservationForPaidOrderAsync(reservationId, cancellationToken);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevertVerifiedSuccessAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("checkout برای تصویر پرداخت پیدا نشد.");

        foreach (var order in group.SellerOrders.Where(x => sellerOrderIds.Contains(x.SellerOrderId)))
        {
            order.RevertVerifiedPayment();
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
