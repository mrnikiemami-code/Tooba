using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
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
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Contracts.Payments;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// درز سفارش برای پرداخت. DbContext پرداخت اینجا باز نمی‌شود و مبلغ از کلاینت خوانده نمی‌شود.
/// تصویر Paid فقط از مصرف رویداد پایدار نوشته می‌شود، نه از تراکنش همزمان Payment.
/// </summary>
public sealed class OrderPaymentBridge : IPayableCheckoutReader, IOrderPaymentProjection, IOrderPaymentProjectionPort
{
    private readonly OrderDbContext _db;
    private readonly IOrderInventoryLifecyclePort _inventoryLifecycle;
    private readonly IReservationCycleDirectory? _cycles;
    private readonly IReservationCyclePolicyResolver? _cyclePolicy;

    /// <summary>
    /// پل را به schema order و قرارداد Inventory وصل می‌کند.
    /// </summary>
    public OrderPaymentBridge(
        OrderDbContext db,
        IOrderInventoryLifecyclePort inventoryLifecycle,
        IReservationCycleDirectory? cycles = null,
        IReservationCyclePolicyResolver? cyclePolicy = null)
    {
        _db = db;
        _inventoryLifecycle = inventoryLifecycle;
        _cycles = cycles;
        _cyclePolicy = cyclePolicy;
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
            if (_cycles is not null)
            {
                await CommitPaidCycleAsync(group, cancellationToken);
            }
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

            var effectiveId = await _inventoryLifecycle.PromoteOrReacquireForManualPaymentReviewAsync(
                reservationId,
                reviewExpiresAt,
                $"manual-review-{line.LineId:N}",
                $"manual-review-{line.LineId:N}-{reviewExpiresAt.UtcTicks}",
                cancellationToken);
            if (effectiveId != reservationId)
            {
                line.ReplaceReservation(effectiveId);
            }
        }

        if (_cycles is not null)
        {
            var now = DateTimeOffset.UtcNow;
            var reservationIds = group.SellerOrders.SelectMany(x => x.Lines)
                .Where(x => x.ReservationId is not null)
                .Select(x => x.ReservationId!.Value)
                .Distinct()
                .ToArray();
            var active = await _cycles.GetActiveAsync(checkoutId, cancellationToken);
            if (active is not null)
            {
                await _cycles.TransitionManualReviewAsync(checkoutId, reviewExpiresAt, now, cancellationToken);
            }
            else
            {
                var policy = _cyclePolicy is null
                    ? new ReservationCyclePolicySnapshot(120, 120, 3, "platform")
                    : await _cyclePolicy.ResolveAsync(
                        group.SellerOrders.SelectMany(x => x.Lines)
                            .Select(x => new ReservationCyclePolicyLine(x.OfferId, x.CategoryIdSnapshot))
                            .ToArray(),
                        cancellationToken);
                await _cycles.StartAsync(
                    checkoutId,
                    ReservationCycleReason.ManualReview,
                    now,
                    reviewExpiresAt,
                    policy,
                    reservationIds,
                    actor: "manual-review",
                    correlationId: $"cycle-review:{checkoutId:N}",
                    paymentAttemptId: null,
                    cancellationToken);
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

            await _inventoryLifecycle.ReleaseIfHeldAsync(reservationId, cancellationToken);
        }

        if (_cycles is not null)
        {
            await _cycles.CloseActiveAsync(
                checkoutId,
                ReservationCycleStatus.ReleasedByPolicy,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }
    }

    private async Task CommitPaidCycleAsync(CheckoutGroup group, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await _cycles!.GetActiveAsync(group.CheckoutId, cancellationToken);
        if (active is null)
        {
            var policy = _cyclePolicy is null
                ? new ReservationCyclePolicySnapshot(120, 120, 3, "platform")
                : await _cyclePolicy.ResolveAsync(
                    group.SellerOrders.SelectMany(x => x.Lines)
                        .Select(x => new ReservationCyclePolicyLine(x.OfferId, x.CategoryIdSnapshot))
                        .ToArray(),
                    cancellationToken);
            var reservationIds = group.SellerOrders.SelectMany(x => x.Lines)
                .Where(x => x.ReservationId is not null)
                .Select(x => x.ReservationId!.Value)
                .Distinct()
                .ToArray();
            if (reservationIds.Length == 0)
            {
                return;
            }

            await _cycles.StartAsync(
                group.CheckoutId,
                ReservationCycleReason.LatePaymentRecovery,
                now,
                now.AddMinutes(Math.Max(1, policy.RetryHoldMinutes)),
                policy,
                reservationIds,
                actor: "late-captured-payment",
                correlationId: $"cycle-late:{group.CheckoutId:N}",
                paymentAttemptId: null,
                cancellationToken);
        }

        await _cycles.CloseActiveAsync(
            group.CheckoutId,
            ReservationCycleStatus.CommittedPaid,
            now,
            cancellationToken);
    }

    private async Task EnsurePaidDurableOrKeepPaidAsync(
        CheckoutGroup group,
        CancellationToken cancellationToken)
    {
        var lines = group.SellerOrders
            .Where(x => x.Status != SellerOrderStatus.Cancelled)
            .SelectMany(order => order.Lines)
            .Select(line => new OrderInventoryPaidSupplyLine(
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

        var result = await _inventoryLifecycle.EnsurePaidDurableSupplyAsync(
            new OrderInventoryPaidSupplyRequest(
                group.CheckoutId,
                Reason: "late-captured-payment",
                CorrelationId: $"paid:{group.CheckoutId:N}",
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
