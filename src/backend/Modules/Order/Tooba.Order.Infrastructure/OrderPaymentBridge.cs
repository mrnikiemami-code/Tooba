using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
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

        foreach (var order in group.SellerOrders)
        {
            if (!_db.Entry(order).Collection(x => x.Lines).IsLoaded)
            {
                await _db.Entry(order).Collection(x => x.Lines).LoadAsync(cancellationToken);
            }
        }

        foreach (var order in group.SellerOrders.Where(x => sellerOrderIds.Contains(x.SellerOrderId)))
        {
            if (group.Mode != OrderMode.OnlinePurchase)
            {
                throw new InvalidOperationException("درخواست رزرو با موفقیت درگاه Paid نمی‌شود.");
            }

            order.RecordVerifiedPayment();
        }

        try
        {
            await EnsurePaidDurableOrKeepPaidAsync(group, cancellationToken);
        }
        catch (Exception)
        {
            // Late captured money stays Paid; SupplyStatus remains Unavailable.
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

    /// <inheritdoc />
    public async Task PromoteReservationsForManualPaymentReviewAsync(
        Guid checkoutId,
        DateTimeOffset reviewExpiresAt,
        CancellationToken cancellationToken)
    {
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("checkout برای ارتقای رزرو پیدا نشد.");

        foreach (var line in group.SellerOrders.SelectMany(x => x.Lines))
        {
            if (line.ReservationId is not { } reservationId)
            {
                continue;
            }

            var existing = await _inventory.FindReservationAsync(reservationId, cancellationToken);
            if (existing is { Status: StockReservationStatus.Held })
            {
                await _inventory.PromoteReservationForManualPaymentReviewAsync(
                    reservationId,
                    reviewExpiresAt,
                    cancellationToken);
                continue;
            }

            // Released/Consumed: never resurrect; authoritative reacquire under review TTL.
            if (existing is null)
            {
                throw new InvalidOperationException("inventory.reservation.not_found");
            }

            try
            {
                var receipt = await _inventory.ReserveAsync(
                    existing.StockItemId,
                    existing.Quantity,
                    $"manual-review-{line.LineId:N}",
                    $"manual-review-{line.LineId:N}-{reviewExpiresAt.UtcTicks}",
                    reviewExpiresAt,
                    cancellationToken);
                line.ReplaceReservation(receipt.ReservationId);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("inventory.manual_review.unavailable");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReleaseReservationsAfterManualRejectAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("checkout برای آزادسازی رزرو پیدا نشد.");

        foreach (var line in group.SellerOrders.SelectMany(x => x.Lines))
        {
            if (line.ReservationId is not { } reservationId)
            {
                continue;
            }

            var existing = await _inventory.FindReservationAsync(reservationId, cancellationToken);
            if (existing is not { Status: StockReservationStatus.Held })
            {
                continue;
            }

            await _inventory.ReleaseAsync(reservationId, cancellationToken);
        }
    }

    private async Task EnsurePaidDurableOrKeepPaidAsync(
        CheckoutGroup group,
        CancellationToken cancellationToken)
    {
        var lines = group.SellerOrders
            .Where(x => x.Status != SellerOrderStatus.Cancelled)
            .SelectMany(order => order.Lines)
            .Select(line => new OrderSupplyLineInput(
                line.LineId,
                line.OfferId,
                line.ReservationId,
                line.Quantity,
                line.UnitDisplaySnapshot,
                line.UnitCodeSnapshot))
            .ToArray();
        if (lines.Length == 0)
        {
            return;
        }

        var result = await _inventory.EnsureOrderSupplyAsync(
            new EnsureOrderSupplyRequest(
                group.CheckoutId,
                OrderSupplyMode.EnsurePaidDurable,
                AllowReacquire: true,
                Reason: "late-captured-payment",
                CorrelationId: $"paid:{group.CheckoutId:N}",
                ReviewExpiresAt: null,
                lines),
            cancellationToken);
        foreach (var pair in result.NewBindingsByOrderLineId)
        {
            var orderLine = group.SellerOrders.SelectMany(o => o.Lines).Single(x => x.LineId == pair.Key);
            if (orderLine.ReservationId != pair.Value)
            {
                orderLine.ReplaceReservation(pair.Value);
            }
        }
    }

}
