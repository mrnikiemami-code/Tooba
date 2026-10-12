using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Infrastructure.Checkout.Abuse;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.ReservationCycle;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-CHECKOUT-OPEN-UNPAID-DISTINCT-001 — سقف «سفارش باز» روی checkout شمرده می‌شود، نه ردیف سفارش فروشنده.
/// این تست از درز واقعی عبور می‌کند: <see cref="CheckoutAbuseGate"/> روی <see cref="OrderDbContext"/>
/// و <see cref="ReservationCycleDirectory"/> واقعی؛ هیچ شمارشی stub نشده است.
/// </summary>
public sealed class CheckoutAbuseOpenUnpaidDistinctTests
{
    private static readonly Guid StoreId = Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-09-20T10:00:00Z");

    [Fact]
    public async Task Multi_seller_checkout_consumes_exactly_one_open_unpaid_slot()
    {
        var customer = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        await using var db = CreateOrderDb();
        db.Checkouts.Add(CheckoutWith(
            checkoutId,
            customer,
            UnpaidSellerOrder(checkoutId),
            UnpaidSellerOrder(checkoutId),
            UnpaidSellerOrder(checkoutId)));
        await db.SaveChangesAsync();

        // سه سفارش فروشنده در یک checkout = یک سهم. سقف ۲ باید عبور کند؛
        // شمارش ردیفی اینجا ۳ می‌داد و مشتری بی‌دلیل مسدود می‌شد.
        await CreateGate(db).EnsureCanStartInitialReservationAsync(
            customer,
            Settings(maxOpenUnpaid: 2),
            CancellationToken.None);

        // و همان یک سهم دقیقاً مصرف شده است: با سقف ۱، شمارش گزارش‌شده باید ۱ باشد نه ۳.
        var blocked = await Assert.ThrowsAsync<CheckoutAbuseLimitException>(() =>
            CreateGate(db).EnsureCanStartInitialReservationAsync(
                customer,
                Settings(maxOpenUnpaid: 1),
                CancellationToken.None));
        Assert.Equal("checkout.open_unpaid_limit_reached", blocked.ErrorCode);
        Assert.Equal("open_unpaid", blocked.Kind);
        Assert.Equal(1, blocked.CurrentCount);
    }

    [Fact]
    public async Task Distinct_checkouts_each_consume_their_own_slot()
    {
        var customer = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await using var db = CreateOrderDb();
        db.Checkouts.Add(CheckoutWith(first, customer, UnpaidSellerOrder(first)));
        db.Checkouts.Add(CheckoutWith(second, customer, UnpaidSellerOrder(second)));
        await db.SaveChangesAsync();

        await CreateGate(db).EnsureCanStartInitialReservationAsync(
            customer,
            Settings(maxOpenUnpaid: 3),
            CancellationToken.None);

        var blocked = await Assert.ThrowsAsync<CheckoutAbuseLimitException>(() =>
            CreateGate(db).EnsureCanStartInitialReservationAsync(
                customer,
                Settings(maxOpenUnpaid: 2),
                CancellationToken.None));
        Assert.Equal(2, blocked.CurrentCount);
        Assert.Equal(2, blocked.MaxCount);
    }

    [Fact]
    public async Task Retry_exhausted_checkout_is_excluded_while_a_retryable_one_still_counts()
    {
        var customer = Guid.NewGuid();
        var dead = Guid.NewGuid();
        var live = Guid.NewGuid();
        await using var db = CreateOrderDb();
        db.Checkouts.Add(CheckoutWith(dead, customer, UnpaidSellerOrder(dead)));
        db.Checkouts.Add(CheckoutWith(live, customer, UnpaidSellerOrder(live)));
        await db.SaveChangesAsync();

        var cycles = new ReservationCycleDirectory(db);
        var policy = new ReservationCyclePolicySnapshot(10, 5, 3, "platform");

        // checkout مرده: سه چرخهٔ مصرف‌شده و بسته → سقف رزرو مجدد تمام است.
        for (var i = 0; i < 3; i++)
        {
            var startedAt = T0.AddMinutes(i * 10);
            await cycles.StartAsync(
                dead,
                i == 0 ? ReservationCycleReason.InitialPayment : ReservationCycleReason.RetryAfterExpiry,
                startedAt,
                startedAt.AddMinutes(10),
                policy,
                [Guid.NewGuid()],
                "commit",
                null,
                null,
                CancellationToken.None);
            await cycles.CloseActiveAsync(dead, ReservationCycleStatus.Expired, startedAt.AddMinutes(10), CancellationToken.None);
        }

        // checkout زنده: چرخهٔ فعال با مهلت باقی‌مانده → هنوز قابل پرداخت و دارای سهم.
        await cycles.StartAsync(
            live,
            ReservationCycleReason.InitialPayment,
            T0,
            T0.AddMinutes(10),
            policy,
            [Guid.NewGuid()],
            "commit",
            null,
            null,
            CancellationToken.None);

        var gate = CreateGate(db, T0.AddMinutes(1));

        // فقط checkout زنده سهم می‌گیرد: ۱ از ۲ → عبور می‌کند.
        await gate.EnsureCanStartInitialReservationAsync(customer, Settings(maxOpenUnpaid: 2), CancellationToken.None);

        // و دقیقاً یک سهم: سقف ۱ مسدود می‌کند با شمارش ۱ (نه ۲ که checkout مرده را هم می‌شمرد).
        var blocked = await Assert.ThrowsAsync<CheckoutAbuseLimitException>(() =>
            gate.EnsureCanStartInitialReservationAsync(customer, Settings(maxOpenUnpaid: 1), CancellationToken.None));
        Assert.Equal(1, blocked.CurrentCount);
    }

    [Fact]
    public async Task Non_open_statuses_do_not_consume_a_slot()
    {
        var customer = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        await using var db = CreateOrderDb();
        var cancelled = UnpaidSellerOrder(checkoutId);
        cancelled.Cancel();
        db.Checkouts.Add(CheckoutWith(checkoutId, customer, cancelled));
        await db.SaveChangesAsync();

        // وضعیت پایانی غیرقابل‌پرداخت سهم نمی‌گیرد؛ سقف ۱ نباید مسدود کند.
        await CreateGate(db).EnsureCanStartInitialReservationAsync(
            customer,
            Settings(maxOpenUnpaid: 1),
            CancellationToken.None);
    }

    private static OrderDbContext CreateOrderDb() =>
        new(new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase($"checkout-abuse-distinct-{Guid.NewGuid():N}")
            .Options);

    private static CheckoutAbuseGate CreateGate(OrderDbContext db, DateTimeOffset? now = null) =>
        new(db, new SettingsReader(), new FixedClock(now ?? T0), new ReservationCycleDirectory(db));

    private static CheckoutAbuseSettingsSnapshot Settings(int maxOpenUnpaid) =>
        new(StoreId, maxOpenUnpaid, 30, 3);

    private static CheckoutGroup CheckoutWith(
        Guid checkoutId,
        Guid customerId,
        params SellerOrder[] sellerOrders) =>
        CheckoutGroup.Submit(
            checkoutId,
            $"idem-{checkoutId:N}",
            Guid.NewGuid(),
            OrderMode.OnlinePurchase,
            buyerPartyId: null,
            placedByUserId: customerId,
            market: "IR",
            currency: "IRR",
            channel: SalesChannel.Marketplace,
            sellerOrders: sellerOrders,
            now: T0);

    /// <summary>سفارش فروشنده در وضعیت باز و پرداخت‌نشده (PendingPayment) روی همان checkout.</summary>
    private static SellerOrder UnpaidSellerOrder(Guid checkoutId)
    {
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            offerId: Guid.NewGuid(),
            catalogVariantId: Guid.NewGuid(),
            sellerPartyId: sellerPartyId,
            quantity: 1m,
            unitPrice: 1000m,
            currency: "IRR",
            taxExclusive: true,
            priceId: Guid.NewGuid(),
            reservationId: null,
            taxOutcome: "Taxable",
            taxRate: 0.09m,
            taxAmount: 0m,
            taxInclusive: 1000m,
            taxRuleId: null,
            unitCodeSnapshot: "kg",
            quantityDecimalPlacesSnapshot: 2);
        return SellerOrder.Open(
            checkoutId,
            sellerPartyId,
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
    }

    /// <summary>مرز Catalog برای تست؛ تنظیمات واقعی از snapshot صریح تست می‌آید.</summary>
    private sealed class SettingsReader : IStoreCheckoutAbuseSettingsReader
    {
        public Task<StoreCheckoutAbuseSettingsSnapshot> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new StoreCheckoutAbuseSettingsSnapshot(StoreId, 2, 30, 3));
    }
}
