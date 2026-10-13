using System.Text.RegularExpressions;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Domain.Reservation;
using Xunit;

namespace Tooba.Order.Tests.Application;

/// <summary>
/// Focused Order-side proof for the read-only fresh-payment payability projection (fakes only).
/// Not PostgreSQL end-to-end proof: the reservation row state is read through the Inventory-owned
/// projection, whose own locking/atomicity guarantees live in the Inventory module.
/// </summary>
public sealed class OrderPaymentFreshPayabilityTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2031-05-06T07:08:09Z");
    private static readonly Guid CheckoutId = Guid.Parse("11111111-1111-7111-8111-111111111111");
    private static readonly Guid Reservation1 = Guid.Parse("44444444-4444-7444-8444-444444444441");
    private static readonly Guid Reservation2 = Guid.Parse("44444444-4444-7444-8444-444444444442");

    [Fact]
    public async Task Active_cycle_with_all_holds_held_and_unexpired_is_payable()
    {
        var harness = Harness.Build([Reservation1, Reservation2], ReservationCycleStatus.Active, secondsRemaining: 600);
        harness.Inventory.Holds[Reservation1] = Held(Reservation1, Now.AddMinutes(30));
        harness.Inventory.Holds[Reservation2] = Held(Reservation2, ExpiresAt: null);

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.True(result.Payable);
        Assert.Equal(OrderFreshPaymentPayability.Payable, result.Outcome);
        Assert.Equal(2, result.ReservationCount);
        Assert.Empty(result.UnavailableReservationIds);
        Assert.Equal(2, harness.Inventory.Reads);
    }

    [Fact]
    public async Task Wall_clock_expired_cycle_is_not_payable()
    {
        var harness = Harness.Build([Reservation1], ReservationCycleStatus.Active, secondsRemaining: 0);
        harness.Inventory.Holds[Reservation1] = Held(Reservation1, Now.AddMinutes(30));

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.False(result.Payable);
        Assert.Equal(OrderFreshPaymentPayability.CycleNotPayable, result.Outcome);
        Assert.Equal(0, harness.Inventory.Reads);
    }

    [Fact]
    public async Task Closed_cycle_is_not_payable()
    {
        var harness = Harness.Build([Reservation1], ReservationCycleStatus.ReleasedByPolicy, secondsRemaining: 600);

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.Equal(OrderFreshPaymentPayability.CycleNotPayable, result.Outcome);
        Assert.Equal(0, harness.Inventory.Reads);
    }

    [Fact]
    public async Task Configured_renewal_quota_exhausted_is_reported_distinctly()
    {
        var harness = Harness.Build(
            [Reservation1],
            ReservationCycleStatus.Expired,
            secondsRemaining: 0,
            retryCountRemaining: 0,
            effectiveMaxCycles: 3,
            totalCyclesCreated: 3);

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.False(result.Payable);
        Assert.Equal(OrderFreshPaymentPayability.RetryExhausted, result.Outcome);
        Assert.Equal(0, result.RetryCountRemaining);
        Assert.Equal(3, result.EffectiveMaxCycles);
    }

    [Fact]
    public async Task Released_hold_under_an_active_cycle_is_rejected()
    {
        var harness = Harness.Build([Reservation1, Reservation2], ReservationCycleStatus.Active, secondsRemaining: 600);
        harness.Inventory.Holds[Reservation1] = Held(Reservation1, Now.AddMinutes(30));
        harness.Inventory.Holds[Reservation2] = new OrderInventoryReservationView(
            Reservation2,
            Guid.NewGuid(),
            1m,
            "Released",
            Now.AddMinutes(30));

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.False(result.Payable);
        Assert.Equal(OrderFreshPaymentPayability.HoldUnavailable, result.Outcome);
        Assert.Equal([Reservation2], result.UnavailableReservationIds);
    }

    [Fact]
    public async Task Expired_hold_is_rejected_without_inventing_a_ttl()
    {
        var harness = Harness.Build([Reservation1], ReservationCycleStatus.Active, secondsRemaining: 600);
        harness.Inventory.Holds[Reservation1] = Held(Reservation1, Now);

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.Equal(OrderFreshPaymentPayability.HoldUnavailable, result.Outcome);
        Assert.Equal([Reservation1], result.UnavailableReservationIds);
    }

    [Fact]
    public async Task Missing_reservation_row_is_rejected()
    {
        var harness = Harness.Build([Reservation1], ReservationCycleStatus.Active, secondsRemaining: 600);

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.Equal(OrderFreshPaymentPayability.HoldUnavailable, result.Outcome);
        Assert.Equal([Reservation1], result.UnavailableReservationIds);
    }

    [Fact]
    public async Task Absent_cycle_authority_is_never_payable()
    {
        var harness = Harness.Build([Reservation1], ReservationCycleStatus.Active, secondsRemaining: 600, withCycles: false);
        harness.Inventory.Holds[Reservation1] = Held(Reservation1, Now.AddMinutes(30));

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.False(result.Payable);
        Assert.Equal(OrderFreshPaymentPayability.AuthorityUnavailable, result.Outcome);
        Assert.Equal(0, harness.Cycles.Reads);
        Assert.Equal(0, harness.Inventory.Reads);
    }

    [Fact]
    public async Task Order_without_lines_cannot_be_proven()
    {
        var harness = Harness.Build([], ReservationCycleStatus.Active, secondsRemaining: 600);

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.Equal(OrderFreshPaymentPayability.ReservationSetIncomplete, result.Outcome);
        Assert.Equal(0, harness.Cycles.Reads);
    }

    [Fact]
    public async Task Line_without_a_reservation_cannot_be_proven()
    {
        var harness = Harness.Build([Reservation1, null], ReservationCycleStatus.Active, secondsRemaining: 600);

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.Equal(OrderFreshPaymentPayability.ReservationSetIncomplete, result.Outcome);
        Assert.Equal(0, harness.Inventory.Reads);
    }

    [Fact]
    public async Task Duplicate_reservation_id_across_lines_is_ambiguity_not_a_second_hold()
    {
        var harness = Harness.Build([Reservation1, Reservation1], ReservationCycleStatus.Active, secondsRemaining: 600);
        harness.Inventory.Holds[Reservation1] = Held(Reservation1, Now.AddMinutes(30));

        var result = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);

        Assert.False(result.Payable);
        Assert.Equal(OrderFreshPaymentPayability.ReservationSetIncomplete, result.Outcome);
        Assert.Equal(0, harness.Inventory.Reads);
    }

    [Fact]
    public async Task Unknown_checkout_is_unavailable_and_empty_id_never_reads()
    {
        var harness = Harness.Build([Reservation1], ReservationCycleStatus.Active, secondsRemaining: 600, setExists: false);

        var missing = await harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, CancellationToken.None);
        var empty = await harness.Service.GetFreshPaymentPayabilityAsync(Guid.Empty, CancellationToken.None);

        Assert.Equal(OrderFreshPaymentPayability.CheckoutUnavailable, missing.Outcome);
        Assert.Equal(OrderFreshPaymentPayability.CheckoutUnavailable, empty.Outcome);
        Assert.Equal(1, harness.Reservations.Reads);
        Assert.False(missing.Payable);
        Assert.False(empty.Payable);
    }

    [Fact]
    public async Task Cancellation_is_propagated_and_never_reported_as_payable()
    {
        var harness = Harness.Build([Reservation1], ReservationCycleStatus.Active, secondsRemaining: 600);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Service.GetFreshPaymentPayabilityAsync(CheckoutId, cancelled.Token));
    }

    [Fact]
    public void Projection_source_has_no_ttl_arithmetic_no_write_and_no_payment_call()
    {
        var text = File.ReadAllText(Path.Combine(
            Repo(),
            "src/backend/Modules/Order/Tooba.Order.Application/Checkout/Process/OrderPaymentFreshPayabilityService.cs"));

        Assert.DoesNotMatch(@"AddHours|AddMinutes|AddDays|TimeSpan\.From", text);
        Assert.DoesNotContain("SaveChanges", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IPaymentInitiationPort", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IPaymentDirectory", text, StringComparison.Ordinal);
        Assert.Contains("ReservationCyclePayabilityPolicy.IsActive", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Existing_payability_projection_stays_readable_and_unchanged()
    {
        var text = File.ReadAllText(Path.Combine(
            Repo(),
            "src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/OrderPaymentBridge.cs"));

        Assert.DoesNotContain("ReservationCyclePayabilityPolicy", text, StringComparison.Ordinal);
        Assert.DoesNotContain("payment.reservation.not_payable", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IsActive", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Fresh_payability_port_is_declared_and_registered_once()
    {
        var contracts = File.ReadAllText(Path.Combine(
            Repo(),
            "src/backend/Modules/Order/Tooba.Order.Contracts/Payments/OrderPaymentFreshPayabilityContracts.cs"));
        var module = File.ReadAllText(Path.Combine(
            Repo(),
            "src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs"));

        Assert.Contains("public interface IOrderPaymentFreshPayabilityPort", contracts, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(module, @"AddScoped<\s*IOrderPaymentFreshPayabilityPort").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddScoped<\s*IOrderCheckoutReservationSetReader").Count);
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repository root not found");
    }

    private static OrderInventoryReservationView Held(Guid reservationId, DateTimeOffset? ExpiresAt) =>
        new(reservationId, Guid.NewGuid(), 1m, "Held", ExpiresAt);

    private sealed class Harness
    {
        private Harness(
            ReservationSetFake reservations,
            CyclesFake cycles,
            InventoryFake inventory,
            OrderPaymentFreshPayabilityService service)
        {
            Reservations = reservations;
            Cycles = cycles;
            Inventory = inventory;
            Service = service;
        }

        public ReservationSetFake Reservations { get; }

        public CyclesFake Cycles { get; }

        public InventoryFake Inventory { get; }

        public OrderPaymentFreshPayabilityService Service { get; }

        public static Harness Build(
            Guid?[] reservationIds,
            ReservationCycleStatus cycleStatus,
            int secondsRemaining,
            bool withCycles = true,
            bool setExists = true,
            int retryCountRemaining = 2,
            int effectiveMaxCycles = 3,
            int totalCyclesCreated = 1)
        {
            var reservations = new ReservationSetFake(reservationIds, setExists);
            var cycles = new CyclesFake(
                cycleStatus,
                secondsRemaining,
                retryCountRemaining,
                effectiveMaxCycles,
                totalCyclesCreated);
            var inventory = new InventoryFake();
            var service = new OrderPaymentFreshPayabilityService(
                reservations,
                withCycles ? cycles : null,
                inventory,
                new TestClock(Now));
            return new Harness(reservations, cycles, inventory, service);
        }
    }

    private sealed class ReservationSetFake(Guid?[] reservationIds, bool setExists) : IOrderCheckoutReservationSetReader
    {
        public int Reads { get; private set; }

        public Task<OrderCheckoutReservationSet?> GetReservationSetAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            return Task.FromResult(setExists && checkoutId == CheckoutId
                ? new OrderCheckoutReservationSet(CheckoutId, reservationIds)
                : null);
        }
    }

    private sealed class CyclesFake(
        ReservationCycleStatus status,
        int secondsRemaining,
        int retryCountRemaining,
        int effectiveMaxCycles,
        int totalCyclesCreated) : IReservationCycleDirectory
    {
        public int Reads { get; private set; }

        public Task<ReservationCycleProjection> GetProjectionAsync(
            Guid id,
            DateTimeOffset serverNow,
            string? supplyStatus,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            return Task.FromResult(new ReservationCycleProjection(
                id,
                CurrentCycleNumber: 1,
                CurrentStatus: status,
                StartedAt: Now,
                ExpiresAt: Now.AddTicks(TimeSpan.FromSeconds(secondsRemaining).Ticks),
                ServerTime: serverNow,
                SecondsRemaining: secondsRemaining,
                TotalCyclesCreated: totalCyclesCreated,
                EffectiveMaxCycles: effectiveMaxCycles,
                RetryCountRemaining: retryCountRemaining,
                History: [],
                SupplyStatus: null));
        }

        public global::Tooba.Order.Domain.Reservation.ReservationCycle PrepareStart(
            Guid checkoutIdValue,
            ReservationCycleReason reason,
            DateTimeOffset startedAt,
            DateTimeOffset expiresAt,
            ReservationCyclePolicySnapshot policy,
            IReadOnlyList<Guid> reservationIds,
            string? actor,
            string? correlationId,
            Guid? paymentAttemptId) => throw new NotSupportedException();

        public Task<ReservationCycleSnapshot> StartAsync(
            Guid checkoutIdValue,
            ReservationCycleReason reason,
            DateTimeOffset startedAt,
            DateTimeOffset expiresAt,
            ReservationCyclePolicySnapshot policy,
            IReadOnlyList<Guid> reservationIds,
            string? actor,
            string? correlationId,
            Guid? paymentAttemptId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CorrelatePaymentAttemptAsync(Guid checkoutIdValue, Guid? paymentAttemptId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task TransitionManualReviewAsync(
            Guid checkoutIdValue,
            DateTimeOffset reviewExpiresAt,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CloseExpiredDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CloseActiveAsync(
            Guid checkoutIdValue,
            ReservationCycleStatus status,
            DateTimeOffset endedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordReacquireRequestedAsync(Guid checkoutIdValue, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RecordReacquireFailedAsync(
            Guid checkoutIdValue,
            DateTimeOffset now,
            string detail,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordRetryLimitReachedAsync(Guid checkoutIdValue, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReservationCycleSnapshot?> GetActiveAsync(Guid checkoutIdValue, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> CountCreatedAsync(Guid checkoutIdValue, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, ReservationCycleProjection>> GetProjectionsAsync(
            IReadOnlyList<Guid> checkoutIds,
            DateTimeOffset serverNow,
            IReadOnlyDictionary<Guid, string?>? supplyByCheckout,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ReservationCycleEventSnapshot>> ListEventsAsync(
            Guid checkoutIdValue,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class InventoryFake : IOrderInventoryLifecyclePort
    {
        public Dictionary<Guid, OrderInventoryReservationView> Holds { get; } = [];

        public int Reads { get; private set; }

        public Task<OrderInventoryReservationView?> FindReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            return Task.FromResult(Holds.TryGetValue(reservationId, out var view) ? view : null);
        }

        public Task ReleaseHeldReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task TryReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Guid> ReacquireDurableHoldFromPreviousAsync(
            Guid previousReservationId,
            string externalReference,
            string idempotencyKey,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Guid> PromoteOrReacquireForManualPaymentReviewAsync(
            Guid reservationId,
            DateTimeOffset reviewExpiresAt,
            string externalReference,
            string idempotencyKey,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ReleaseIfHeldAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OrderInventoryPaidSupplyResult> EnsurePaidDurableSupplyAsync(
            OrderInventoryPaidSupplyRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<OrderInventorySupplyResult> EnsureUnpaidRetryHoldAsync(
            OrderInventoryUnpaidRetryRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, OrderInventorySupplyStatusSnapshot>> GetSupplyStatusesAsync(
            IReadOnlyDictionary<Guid, IReadOnlyList<OrderInventorySupplyLine>> linesByCheckoutId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<OrderInventoryReservationView> ReserveAsync(
            Guid stockItemId,
            decimal quantity,
            string? externalReference,
            string? idempotencyKey,
            DateTimeOffset? expiresAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OrderInventorySupplyStatusDetail> GetSupplyStatusDetailAsync(
            Guid checkoutIdValue,
            IReadOnlyList<OrderInventorySupplyLine> lines,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<OrderInventoryEnsureDetailResult> EnsureSupplyDetailAsync(
            OrderInventoryEnsureDetailRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }
}
