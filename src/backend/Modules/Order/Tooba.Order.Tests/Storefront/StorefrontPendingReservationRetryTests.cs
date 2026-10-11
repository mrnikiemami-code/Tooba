using System.Collections.Concurrent;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.PendingPayment.Commands.RetryPendingCheckoutReservation;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Validation;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Enums;
using Tooba.Payment.Contracts.Storefront;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// «بررسی موجودی و پرداخت مجدد» (LOCK-SF-078/083/085/087): عملیات checkout-scoped است،
/// از سیاست رزرو سفارش استفاده می‌کند، paymentId جعل نمی‌کند و هیچ‌گاه بازگشت خاموش ندارد.
/// </summary>
public sealed class StorefrontPendingReservationRetryTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");

    [Fact]
    public async Task Expired_hold_retry_stays_on_the_same_checkout_and_never_invents_a_payment()
    {
        var group = OrderTestData.Checkout();
        var harness = Harness.Create(group, group.PlacedByUserId);

        var value = await harness.Service.RetryExpiredReservationAsync(
            group.CheckoutId,
            group.CartId,
            guestSecret: null,
            CancellationToken.None);

        // همان checkout و همان هویت سفارش؛ چرخهٔ جدید توسط coordinator ساخته شده است.
        Assert.Equal([group.CheckoutId], harness.Coordinator.RequestedCheckoutIds);
        Assert.Equal(group.CheckoutId, ReadGuid(value, "checkoutId"));
        Assert.Equal(2, ReadInt(value, "cycleNumber"));
        Assert.Equal(900, ReadInt(value, "secondsRemaining"));

        // فقط خواندن وضعیت پرداخت؛ هیچ ردیف پرداختی ساخته یا بسته نمی‌شود.
        Assert.Equal(1, harness.Payments.OperationalReads);
        Assert.False(harness.Payments.TouchedForWrite);
    }

    [Fact]
    public async Task Unavailable_supply_surfaces_the_canonical_unpaid_supply_code()
    {
        var group = OrderTestData.Checkout();
        var harness = Harness.Create(group, group.PlacedByUserId);
        harness.Coordinator.Supply = new EnsureOrderSupplyResult(
            OrderSupplyOutcome.Unavailable,
            OrderSupplyStatusKind.Unavailable,
            [],
            new Dictionary<Guid, Guid>());

        var exception = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.Service.RetryExpiredReservationAsync(group.CheckoutId, group.CartId, null, CancellationToken.None));

        // کد پایدار متعارف این سناریو (همان کد پنل مشتری/پرداخت)، بدون بازنگاشت به checkout.inventory.unavailable.
        Assert.Equal(StorefrontOrderErrors.PaymentUnpaidSupplyUnavailable, exception.Code);
        Assert.NotEqual(StorefrontOrderErrors.CheckoutInventoryUnavailable, exception.Code);
    }

    [Fact]
    public async Task Retry_limit_reached_keeps_the_canonical_reservation_code_end_to_end()
    {
        var group = OrderTestData.Checkout();
        var harness = Harness.Create(group, group.PlacedByUserId);
        harness.Coordinator.RejectWithRetryLimit = true;

        var result = await new RetryPendingCheckoutReservationHandler(harness.Service).Handle(
            new RetryPendingCheckoutReservationCommand(group.CheckoutId, group.CartId, null),
            CancellationToken.None);

        // هرگز بازگشت خاموش/موفق بدون عملیات: خطای صریح و قابل ترجمه برمی‌گردد.
        Assert.True(result.IsFailure);
        Assert.Equal(ReservationCycleErrors.RetryLimitReached, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Cancelled_checkout_is_not_retryable_and_never_touches_the_cycle_engine()
    {
        var group = OrderTestData.Checkout(status: SellerOrderStatus.Cancelled);
        var harness = Harness.Create(group, group.PlacedByUserId);

        var exception = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.Service.RetryExpiredReservationAsync(group.CheckoutId, group.CartId, null, CancellationToken.None));

        Assert.Equal(StorefrontOrderErrors.PendingRetryNotAllowed, exception.Code);
        Assert.Empty(harness.Coordinator.RequestedCheckoutIds);
    }

    [Fact]
    public async Task Succeeded_payment_blocks_inventory_retry_even_when_ui_is_stale()
    {
        var group = OrderTestData.Checkout();
        var harness = Harness.Create(group, group.PlacedByUserId, paymentStatus: "Succeeded");

        var exception = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.Service.RetryExpiredReservationAsync(group.CheckoutId, group.CartId, null, CancellationToken.None));

        Assert.Equal(StorefrontOrderErrors.PendingRetryNotAllowed, exception.Code);
        Assert.Empty(harness.Coordinator.RequestedCheckoutIds);
    }

    [Fact]
    public async Task Foreign_actor_cannot_retry_someone_elses_checkout()
    {
        var group = OrderTestData.Checkout();
        var harness = Harness.Create(group, actorId: Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            harness.Service.RetryExpiredReservationAsync(group.CheckoutId, group.CartId, null, CancellationToken.None));

        Assert.Equal(StorefrontOrderErrors.CheckoutAccessDenied, exception.Code);
        Assert.Empty(harness.Coordinator.RequestedCheckoutIds);
    }

    [Fact]
    public async Task Guest_proof_must_match_the_checkout_cart_and_its_secret()
    {
        var group = OrderTestData.Checkout();
        var wrongCart = Harness.Create(group, actorId: null);
        var unverifiableProof = Harness.Create(group, actorId: null, cartOwned: false);

        var mismatched = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            wrongCart.Service.RetryExpiredReservationAsync(group.CheckoutId, Guid.NewGuid(), "secret", CancellationToken.None));
        var unverified = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            unverifiableProof.Service.RetryExpiredReservationAsync(group.CheckoutId, group.CartId, "wrong-secret", CancellationToken.None));

        Assert.Equal(StorefrontOrderErrors.CheckoutAccessDenied, mismatched.Code);
        Assert.Equal(StorefrontOrderErrors.CheckoutAccessDenied, unverified.Code);
        Assert.Empty(wrongCart.Coordinator.RequestedCheckoutIds);
        Assert.Empty(unverifiableProof.Coordinator.RequestedCheckoutIds);
    }

    [Fact]
    public async Task Duplicate_and_concurrent_clicks_always_target_the_same_checkout_identity()
    {
        var group = OrderTestData.Checkout();
        var harness = Harness.Create(group, group.PlacedByUserId);

        var values = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            harness.Service.RetryExpiredReservationAsync(group.CheckoutId, group.CartId, null, CancellationToken.None)));

        Assert.Equal(6, harness.Coordinator.RequestedCheckoutIds.Count);
        Assert.Single(harness.Coordinator.RequestedCheckoutIds.Distinct());
        Assert.All(values, value => Assert.Equal(group.CheckoutId, ReadGuid(value, "checkoutId")));
        Assert.All(values, value => Assert.Equal(2, ReadInt(value, "cycleNumber")));
    }

    [Fact]
    public async Task Command_transport_requires_checkout_and_cart_primitives()
    {
        var result = await new RetryPendingCheckoutReservationCommandValidator().ValidateAsync(
            new RetryPendingCheckoutReservationCommand(Guid.Empty, Guid.Empty, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorCode == OrderValidationCodes.CheckoutIdRequired);
        Assert.Contains(result.Errors, x => x.ErrorCode == OrderValidationCodes.CartIdRequired);
    }

    private static Guid ReadGuid(object value, string property) =>
        (Guid)value.GetType().GetProperty(property)!.GetValue(value)!;

    private static int ReadInt(object value, string property) =>
        Convert.ToInt32(value.GetType().GetProperty(property)!.GetValue(value), null);

    /// <summary>سیم‌کشی تست: فقط پورت‌های مسیر retry واقعی‌اند؛ بقیه null هستند تا اگر لمس شوند، تست افشا کند.</summary>
    private sealed class Harness
    {
        public required PaymentsFake Payments { get; init; }
        public required CycleDirectoryStub Cycles { get; init; }
        public required CoordinatorFake Coordinator { get; init; }
        public required StorefrontPendingPaymentService Service { get; init; }

        public static Harness Create(
            CheckoutGroup group,
            Guid? actorId,
            string? paymentStatus = null,
            bool cartOwned = true)
        {
            var payments = new PaymentsFake(paymentStatus);
            var cycles = new CycleDirectoryStub(group.CheckoutId);
            var coordinator = new CoordinatorFake();
            var service = new StorefrontPendingPaymentService(
                new PendingStore(group),
                payments,
                catalog: null!,
                new CartsFake(cartOwned),
                cycles,
                coordinator,
                checkout: null!,
                fulfillment: null!,
                settlement: null!,
                new Actor(actorId),
                new FixedClock(Now));
            return new Harness
            {
                Payments = payments,
                Cycles = cycles,
                Coordinator = coordinator,
                Service = service,
            };
        }
    }

    private sealed class PendingStore(CheckoutGroup group) : IStorefrontPendingCheckoutStore
    {
        public Task<CheckoutGroup?> GetWithSellerOrdersAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult<CheckoutGroup?>(group.CheckoutId == checkoutId ? group : null);

        public Task<IReadOnlyList<CheckoutGroup>> ListOwnedByUserAsync(Guid userId, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CheckoutGroup>> ListByCheckoutIdsAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task HidePendingCardAsync(Guid checkoutId, Guid? ownerUserId, Guid? guestCartId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<HashSet<Guid>> LoadHiddenCheckoutIdsAsync(
            IReadOnlyList<Guid> checkoutIds,
            Guid? ownerUserId,
            IReadOnlyList<Guid> guestCartIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class PaymentsFake(string? status) : IPendingPaymentReader
    {
        public int OperationalReads { get; private set; }

        public bool TouchedForWrite { get; private set; }

        public Task<IReadOnlyList<PendingPaymentLatestSnapshot>> GetLatestByCheckoutIdsAsync(
            IReadOnlyCollection<Guid> checkoutIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PendingPaymentOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
        {
            OperationalReads++;
            return Task.FromResult(status is null ? null : Payment(checkoutId, status));
        }

        public Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken)
        {
            TouchedForWrite = true;
            throw new NotSupportedException();
        }

        private static PendingPaymentOperationalSnapshot Payment(Guid checkoutId, string status) =>
            new(Guid.NewGuid(), checkoutId, status, 250000m, "IRR", "sandbox", null, null,
                Now, Now, null, null, false, false, false, false, false, false, null, null, null);
    }

    private sealed class CartsFake(bool owned) : ICartPresentationGateway
    {
        public Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CartPage?> TryGetForOwnershipAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken) =>
            Task.FromResult(owned ? new CartPage(cartId, 1, "IR", "IRR", "Marketplace", 0m, [], [], guestSecret) : null);
    }

    private sealed class Actor(Guid? actorId) : IOrderStorefrontActor
    {
        public Guid GuestActorId => Guid.Empty;

        public bool IsAuthenticated => actorId is not null;

        public Guid? AuthenticatedUserId => actorId;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => throw new NotSupportedException();

        public Guid? TryResolveListActor() => actorId;

        public CartAccess BuildCartAccess(string? guestSecret) => throw new NotSupportedException();
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class CoordinatorFake : IReservationCycleCoordinator
    {
        private readonly ConcurrentQueue<Guid> _requested = new();

        public IReadOnlyList<Guid> RequestedCheckoutIds => _requested.ToArray();

        public bool RejectWithRetryLimit { get; set; }

        public EnsureOrderSupplyResult Supply { get; set; } = new(
            OrderSupplyOutcome.Reacquired,
            OrderSupplyStatusKind.Reserved,
            [],
            new Dictionary<Guid, Guid>());

        public Task<ReservationCyclePolicySnapshot> ResolveForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult(new ReservationCyclePolicySnapshot(15, 15, 3, "platform"));

        public Task<EnsureOrderSupplyResult> EnsureRetryAfterExpiryAsync(Guid checkoutId, CancellationToken cancellationToken)
        {
            _requested.Enqueue(checkoutId);
            return RejectWithRetryLimit
                ? throw new ContractOperationException(ReservationCycleErrors.RetryLimitReached)
                : Task.FromResult(Supply);
        }
    }

    /// <summary>همهٔ mutationهای چرخه ممنوع‌اند: retry فقط باید از coordinator بگذرد.</summary>
    private sealed class CycleDirectoryStub(Guid checkoutId) : IReservationCycleDirectory
    {
        public Task<IReadOnlyDictionary<Guid, ReservationCycleProjection>> GetProjectionsAsync(
            IReadOnlyList<Guid> checkoutIds,
            DateTimeOffset serverNow,
            IReadOnlyDictionary<Guid, string?>? supplyByCheckout,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, ReservationCycleProjection>>(
                new Dictionary<Guid, ReservationCycleProjection>
                {
                    [checkoutId] = new(
                        checkoutId,
                        2,
                        ReservationCycleStatus.Active,
                        serverNow,
                        serverNow.AddMinutes(15),
                        serverNow,
                        900,
                        2,
                        3,
                        1,
                        [],
                        "Reserved"),
                });

        public ReservationCycle PrepareStart(Guid checkoutId, ReservationCycleReason reason, DateTimeOffset startedAt, DateTimeOffset expiresAt, ReservationCyclePolicySnapshot policy, IReadOnlyList<Guid> reservationIds, string? actor, string? correlationId, Guid? paymentAttemptId) => throw new NotSupportedException();

        public Task<ReservationCycleSnapshot> StartAsync(Guid checkoutId, ReservationCycleReason reason, DateTimeOffset startedAt, DateTimeOffset expiresAt, ReservationCyclePolicySnapshot policy, IReadOnlyList<Guid> reservationIds, string? actor, string? correlationId, Guid? paymentAttemptId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CorrelatePaymentAttemptAsync(Guid checkoutId, Guid? paymentAttemptId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task TransitionManualReviewAsync(Guid checkoutId, DateTimeOffset reviewExpiresAt, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CloseExpiredDueAsync(DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CloseActiveAsync(Guid checkoutId, ReservationCycleStatus status, DateTimeOffset endedAt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordReacquireRequestedAsync(Guid checkoutId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordReacquireFailedAsync(Guid checkoutId, DateTimeOffset now, string detail, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordRetryLimitReachedAsync(Guid checkoutId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ReservationCycleSnapshot?> GetActiveAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CountCreatedAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ReservationCycleProjection> GetProjectionAsync(Guid checkoutId, DateTimeOffset serverNow, string? supplyStatus, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ReservationCycleEventSnapshot>> ListEventsAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
