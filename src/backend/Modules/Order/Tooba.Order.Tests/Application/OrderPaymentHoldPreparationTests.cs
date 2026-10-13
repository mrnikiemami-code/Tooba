using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Storefront;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Payment.Contracts.Hold;
using Xunit;

namespace Tooba.Order.Tests.Application;

/// <summary>
/// Focused Order-side correctness for the payment-hold preparation seam, using fakes only.
/// Nothing here is PostgreSQL end-to-end proof: row locks, all-or-none commit and monotonic
/// <c>max(existing, requested)</c> are proven in the Inventory module (017).
/// </summary>
public sealed class OrderPaymentHoldPreparationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2031-05-06T07:08:09Z");
    private static readonly Guid CheckoutId = Guid.Parse("11111111-1111-7111-8111-111111111111");
    private static readonly Guid CartId = Guid.Parse("22222222-2222-7222-8222-222222222222");
    private static readonly Guid GuestActor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000009");
    private static readonly Guid UserId = Guid.Parse("33333333-3333-7333-8333-333333333333");
    private static readonly Guid Reservation1 = Guid.Parse("44444444-4444-7444-8444-444444444441");
    private static readonly Guid Reservation2 = Guid.Parse("44444444-4444-7444-8444-444444444442");

    [Fact]
    public async Task Prepared_uses_only_persisted_line_reservations_in_a_deterministic_order()
    {
        var harness = Harness.Authenticated(
            lines: [Reservation2, Reservation1],
            cycleSecondsRemaining: 600);
        harness.Policy.HoursByProvider["gateway-x"] = 7;

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.Prepared, result.Outcome);
        Assert.True(result.Prepared);
        Assert.True(result.HoldExtensionCommitted);
        Assert.Equal(2, result.ReservationCount);
        Assert.Equal(Now.AddHours(7), result.RequestedDeadlineUtc);
        Assert.Equal(Now.AddHours(7), result.GuaranteedUntilUtc);
        Assert.Equal(1, result.CycleNumber);
        Assert.Empty(result.UnavailableReservationIds);

        // The caller never supplied ids: the set came from the persisted lines, deduplicated and ordered.
        Assert.Equal([Reservation1, Reservation2], harness.Inventory.LastReservationIds);
        Assert.Equal([Reservation1, Reservation2], harness.Inventory.LastReservationIds!.OrderBy(x => x));
        Assert.Equal(1, harness.Inventory.Calls);
        Assert.Equal(["gateway-x"], harness.Policy.RequestedProviderCodes);
    }

    [Fact]
    public async Task Unowned_or_foreign_checkout_is_rejected_before_any_reservation_work()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600, owned: false);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.AccessDenied, result.Outcome);
        Assert.False(result.Prepared);
        Assert.False(result.HoldExtensionCommitted);
        Assert.Equal(0, harness.Inventory.Calls);
        Assert.Equal(0, harness.Checkouts.Reads);
        Assert.Equal(0, harness.Cycles.Reads);
        Assert.Equal(0, harness.Policy.Calls);
    }

    [Fact]
    public async Task Cart_mismatch_fails_closed_before_any_reservation_work()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);
        harness.Access.RejectCartMismatch = true;

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x", cartId: Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.AccessDenied, result.Outcome);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task A_line_without_a_reservation_rejects_the_whole_set()
    {
        var harness = Harness.Authenticated(lines: [Reservation1, null], cycleSecondsRemaining: 600);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete, result.Outcome);
        Assert.False(result.HoldExtensionCommitted);
        Assert.Equal(0, harness.Inventory.Calls);
        Assert.Equal(0, harness.Cycles.Reads);
    }

    [Fact]
    public async Task An_empty_guid_reservation_rejects_the_whole_set()
    {
        var harness = Harness.Authenticated(lines: [Guid.Empty], cycleSecondsRemaining: 600);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete, result.Outcome);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task An_order_without_lines_rejects_the_whole_set()
    {
        var harness = Harness.Authenticated(lines: [], cycleSecondsRemaining: 600);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete, result.Outcome);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task One_reservation_bound_to_two_lines_is_ambiguous_and_rejected()
    {
        // No foreign id was smuggled in, but sharing a single hold across lines is not a valid set.
        var harness = Harness.Authenticated(lines: [Reservation1, Reservation1], cycleSecondsRemaining: 600);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete, result.Outcome);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task Expired_cycle_is_rejected_before_the_hold_extension()
    {
        var harness = Harness.Authenticated(lines: [Reservation1, Reservation2], cycleSecondsRemaining: 0);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.CycleNotPayable, result.Outcome);
        Assert.False(result.HoldExtensionCommitted);
        Assert.Equal(0, harness.Inventory.Calls);
        Assert.Equal(0, harness.Policy.Calls);
        Assert.Equal(1, harness.Cycles.Reads);
    }

    [Fact]
    public async Task A_closed_cycle_is_rejected_before_the_hold_extension()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);
        harness.Cycles.Projections.Clear();
        harness.Cycles.Projections.Enqueue(Cycle(ReservationCycleStatus.Expired, 0));

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.CycleNotPayable, result.Outcome);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task A_missing_cycle_projection_is_rejected()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);
        harness.Cycles.Projections.Clear();
        harness.Cycles.Projections.Enqueue(null);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.CycleNotPayable, result.Outcome);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task Mixed_reservations_are_rejected_all_or_none_by_inventory()
    {
        var harness = Harness.Authenticated(lines: [Reservation1, Reservation2], cycleSecondsRemaining: 600);
        harness.Inventory.Result = InventoryPaymentHoldExtensionResult.NotHeld(2, [Reservation2]);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.HoldUnavailable, result.Outcome);
        Assert.False(result.Prepared);
        Assert.False(result.HoldExtensionCommitted);
        Assert.Equal([Reservation2], result.UnavailableReservationIds);
        // Everything was asked for in one call: no per-line partial extension exists.
        Assert.Equal(1, harness.Inventory.Calls);
        Assert.Equal([Reservation1, Reservation2], harness.Inventory.LastReservationIds);
    }

    [Fact]
    public async Task An_invalid_deadline_from_inventory_is_reported_not_hidden()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);
        harness.Inventory.Result = InventoryPaymentHoldExtensionResult.DeadlineInvalid(1);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.DeadlineRejected, result.Outcome);
        Assert.False(result.HoldExtensionCommitted);
    }

    [Fact]
    public async Task An_ambient_scope_refusal_from_inventory_is_reported_as_such()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);
        harness.Inventory.Result = InventoryPaymentHoldExtensionResult.AmbientTransactionUnsupported(1);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.AmbientTransactionUnsupported, result.Outcome);
        Assert.False(result.HoldExtensionCommitted);
    }

    [Fact]
    public async Task A_cycle_that_closes_after_the_extension_is_never_reported_as_prepared()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);
        harness.Cycles.Projections.Clear();
        harness.Cycles.Projections.Enqueue(Cycle(ReservationCycleStatus.Active, 600));
        harness.Cycles.Projections.Enqueue(Cycle(ReservationCycleStatus.Expired, 0));

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.CycleClosedAfterExtension, result.Outcome);
        Assert.False(result.Prepared);
        // The durable extension really was committed; that is reported honestly and separately.
        Assert.True(result.HoldExtensionCommitted);
        Assert.Equal(Now.AddHours(7), result.RequestedDeadlineUtc);
        Assert.Equal(2, harness.Cycles.Reads);
    }

    [Fact]
    public async Task Method_specific_policy_is_used_verbatim_and_never_hard_coded()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);
        harness.Policy.HoursByProvider["manual"] = 1;
        harness.Policy.HoursByProvider["wallet"] = 3;
        harness.Policy.HoursByProvider["gateway-x"] = 9;

        var manual = await harness.Service.PrepareAsync(harness.Request("manual"), CancellationToken.None);
        var wallet = await harness.Service.PrepareAsync(harness.Request("wallet"), CancellationToken.None);
        var gateway = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(Now.AddHours(1), manual.RequestedDeadlineUtc);
        Assert.Equal(Now.AddHours(3), wallet.RequestedDeadlineUtc);
        Assert.Equal(Now.AddHours(9), gateway.RequestedDeadlineUtc);
        Assert.Equal(["manual", "wallet", "gateway-x"], harness.Policy.RequestedProviderCodes);
    }

    [Fact]
    public async Task An_absent_policy_source_blocks_instead_of_inventing_a_ttl()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600, withPolicy: false);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.PolicyUnavailable, result.Outcome);
        Assert.Null(result.RequestedDeadlineUtc);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task A_blank_selected_method_blocks_instead_of_guessing_one()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);

        var blank = await harness.Service.PrepareAsync(harness.Request("  "), CancellationToken.None);
        var empty = await harness.Service.PrepareAsync(harness.Request(string.Empty), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.PolicyUnavailable, blank.Outcome);
        Assert.Equal(OrderPaymentHoldPreparationOutcome.PolicyUnavailable, empty.Outcome);
        Assert.Empty(harness.Policy.RequestedProviderCodes);
        Assert.Equal(0, harness.Inventory.Calls);
    }

    [Fact]
    public async Task Replay_is_idempotent_and_never_shortens_the_deadline()
    {
        var harness = Harness.Authenticated(lines: [Reservation1, Reservation2], cycleSecondsRemaining: 600);
        harness.Policy.HoursByProvider["gateway-x"] = 7;

        var first = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);
        var second = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);
        Assert.Equal(first.RequestedDeadlineUtc, second.RequestedDeadlineUtc);

        // A later retry may only ask for the same or a later absolute deadline.
        harness.Clock.UtcNow = Now.AddMinutes(30);
        var retry = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.True(retry.RequestedDeadlineUtc >= first.RequestedDeadlineUtc);
        Assert.Equal(3, harness.Inventory.Calls);
        Assert.True(harness.Inventory.Monotonic);
        Assert.Equal(OrderPaymentHoldPreparationOutcome.Prepared, retry.Outcome);
    }

    [Fact]
    public async Task A_guest_owner_is_accepted_through_the_existing_access_rule()
    {
        var harness = Harness.Guest(lines: [Reservation1], cycleSecondsRemaining: 600);

        var result = await harness.Service.PrepareAsync(harness.Request("gateway-x"), CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.Prepared, result.Outcome);
        Assert.Null(harness.Access.LastAuthenticatedUserId);
        Assert.Equal(GuestActor, harness.Checkouts.LastActorId);
    }

    [Fact]
    public async Task An_unknown_checkout_id_fails_closed()
    {
        var harness = Harness.Authenticated(lines: [Reservation1], cycleSecondsRemaining: 600);

        var result = await harness.Service.PrepareAsync(
            new OrderPaymentHoldPreparationRequest(Guid.Empty, CartId, null, UserId, "gateway-x"),
            CancellationToken.None);

        Assert.Equal(OrderPaymentHoldPreparationOutcome.AccessDenied, result.Outcome);
        Assert.Equal(0, harness.Access.Calls);
    }

    private static ReservationCycleProjection Cycle(ReservationCycleStatus status, int secondsRemaining) =>
        new(
            CheckoutId,
            CurrentCycleNumber: 1,
            CurrentStatus: status,
            StartedAt: Now,
            ExpiresAt: Now.AddTicks(TimeSpan.FromSeconds(secondsRemaining).Ticks),
            ServerTime: Now,
            SecondsRemaining: secondsRemaining,
            TotalCyclesCreated: 1,
            EffectiveMaxCycles: 3,
            RetryCountRemaining: 2,
            History: [],
            SupplyStatus: null);

    private sealed class Harness
    {
        private Harness(
            ICheckoutPaymentAccessReader access,
            ICheckoutDirectory checkouts,
            IReservationCycleDirectory cycles,
            IInventoryPaymentHoldExtensionPort inventory,
            ICommerceHoldPolicySource? policy,
            TestClock clock,
            bool guest)
        {
            Access = (AccessFake)access;
            Checkouts = (CheckoutFake)checkouts;
            Cycles = (CyclesFake)cycles;
            Inventory = (InventoryFake)inventory;
            Policy = (PolicyFake?)policy;
            Clock = clock;
            IsGuest = guest;
            Service = new OrderPaymentHoldPreparationService(
                access,
                new ActorFake(),
                checkouts,
                cycles,
                inventory,
                clock,
                policy);
        }

        public bool IsGuest { get; }

        public AccessFake Access { get; }

        public CheckoutFake Checkouts { get; }

        public CyclesFake Cycles { get; }

        public InventoryFake Inventory { get; }

        public PolicyFake? Policy { get; }

        public TestClock Clock { get; }

        public OrderPaymentHoldPreparationService Service { get; }

        public static Harness Authenticated(
            Guid?[] lines,
            int cycleSecondsRemaining,
            bool owned = true,
            bool withPolicy = true) =>
            Build(lines, cycleSecondsRemaining, owned, withPolicy, guest: false);

        public static Harness Guest(Guid?[] lines, int cycleSecondsRemaining) =>
            Build(lines, cycleSecondsRemaining, owned: true, withPolicy: true, guest: true);

        public OrderPaymentHoldPreparationRequest Request(string providerCode, Guid? cartId = null) =>
            new(
                CheckoutId,
                cartId ?? CartId,
                GuestSecret: "guest-secret",
                AuthenticatedUserId: IsGuest ? null : UserId,
                providerCode);

        private static Harness Build(
            Guid?[] lines,
            int cycleSecondsRemaining,
            bool owned,
            bool withPolicy,
            bool guest)
        {
            var snapshots = lines
                .Select((reservationId, index) => Line(reservationId, index))
                .ToArray();
            var placedBy = guest ? GuestActor : UserId;
            var checkout = Snapshot(snapshots, placedBy);
            var cycles = new CyclesFake(Cycle(ReservationCycleStatus.Active, cycleSecondsRemaining));
            return new Harness(
                new AccessFake(checkout, owned),
                new CheckoutFake(checkout),
                cycles,
                new InventoryFake(),
                withPolicy ? new PolicyFake() : null,
                new TestClock(Now),
                guest);
        }

        private static OrderLineSnapshot Line(Guid? reservationId, int index) =>
            new(
                LineId: Guid.Parse($"55555555-5555-7555-8555-5555555555{index:D2}"),
                OfferId: Guid.Parse($"66666666-6666-7666-8666-6666666666{index:D2}"),
                CatalogVariantId: Guid.Parse($"77777777-7777-7777-8777-7777777777{index:D2}"),
                SellerPartyId: Guid.Parse("88888888-8888-7888-8888-888888888888"),
                Quantity: 1m,
                UnitPriceSnapshot: 1000m,
                LineTotalSnapshot: 1000m,
                Currency: "IRR",
                TaxExclusive: true,
                PriceId: Guid.Parse("99999999-9999-7999-8999-999999999999"),
                ReservationId: reservationId,
                TaxOutcomeSnapshot: "Applied",
                TaxRateSnapshot: 0m,
                TaxAmountSnapshot: 0m,
                TaxInclusiveSnapshot: 1000m,
                TaxRuleIdSnapshot: null,
                DiscountAmountSnapshot: 0m,
                PromotionIdSnapshot: null,
                PromotionNameSnapshot: null,
                PromotionCodeSnapshot: null,
                DiscountKindSnapshot: null,
                PreDiscountTaxExclusiveSnapshot: 1000m,
                PostDiscountTaxExclusiveSnapshot: 1000m,
                PromotionAppliedAtSnapshot: null);

        private static CheckoutSnapshot Snapshot(IReadOnlyList<OrderLineSnapshot> lines, Guid placedByUserId) =>
            new(
                CheckoutId,
                CartId,
                OrderMode.OnlinePurchase,
                BuyerPartyId: Guid.Parse("12121212-1212-7212-8212-121212121212"),
                PlacedByUserId: placedByUserId,
                Market: "IR",
                Currency: "IRR",
                Channel: SalesChannel.Marketplace,
                SubmittedAt: Now.AddMinutes(-5),
                SellerOrders:
                [
                    new SellerOrderSnapshot(
                        Guid.Parse("13131313-1313-7313-8313-131313131313"),
                        "TB-TEST-1",
                        Guid.Parse("88888888-8888-7888-8888-888888888888"),
                        SellerOrderStatus.PendingPayment,
                        SubtotalSnapshot: 1000m * lines.Count,
                        TaxSnapshot: 0m,
                        DiscountSnapshot: 0m,
                        GrandTotalSnapshot: 1000m * lines.Count,
                        Currency: "IRR",
                        Lines: lines),
                ]);
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private sealed class ActorFake : IOrderStorefrontActor
    {
        public Guid GuestActorId => GuestActor;

        public bool IsAuthenticated => true;

        public Guid? AuthenticatedUserId => UserId;

        public Guid ResolvePlacementActor(bool usingSavedAddress) => throw new NotSupportedException();

        public Guid? TryResolveListActor() => UserId;

        public Tooba.Cart.Contracts.CartAccess BuildCartAccess(string? guestSecret) => throw new NotSupportedException();
    }

    private sealed class AccessFake(CheckoutSnapshot checkout, bool owned) : ICheckoutPaymentAccessReader
    {
        public int Calls { get; private set; }

        public bool RejectCartMismatch { get; set; }

        public Guid? LastAuthenticatedUserId { get; private set; }

        public Task<CheckoutPaymentAccessSnapshot?> GetForMutationAsync(
            Guid checkoutId,
            Guid? cartId,
            string? guestSecret,
            Guid? authenticatedUserId,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastAuthenticatedUserId = authenticatedUserId;
            var accepted = owned
                && checkoutId == checkout.CheckoutId
                && !RejectCartMismatch
                && (cartId is null || cartId == checkout.CartId);
            return Task.FromResult(accepted
                ? new CheckoutPaymentAccessSnapshot(checkout.CheckoutId, 1000m, "IRR", "TB-TEST-1")
                : null);
        }

        public Task<CheckoutPaymentAccessSnapshot?> GetOwnedForPaymentResultAsync(
            Guid checkoutId,
            string? guestSecret,
            Guid? authenticatedUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutPaymentAccessSnapshot?> GetOwnedAsync(
            Guid checkoutId,
            Guid? cartId,
            string? guestSecret,
            Guid? authenticatedUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class CheckoutFake(CheckoutSnapshot checkout) : ICheckoutDirectory
    {
        public int Reads { get; private set; }

        public Guid? LastActorId { get; private set; }

        public Task<CheckoutSnapshot?> GetCheckoutAsync(
            Guid checkoutId,
            OrderAccess access,
            CancellationToken cancellationToken)
        {
            Reads++;
            LastActorId = access.PlacedByUserId;
            var matches = checkoutId == checkout.CheckoutId
                && (access.PlacedByUserId == checkout.PlacedByUserId
                    || access.BuyerPartyId == checkout.BuyerPartyId);
            return Task.FromResult(matches ? checkout : null);
        }

        public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(
            string orderNumber,
            OrderAccess access,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RestoreCancelledCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
            Guid checkoutId,
            Guid viewerUserId,
            int take,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
            Guid checkoutId,
            Guid actorUserId,
            string body,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
            Guid checkoutId,
            Guid noteId,
            Guid actorUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordAdminViewAsync(
            Guid checkoutId,
            Guid viewerUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class CyclesFake(ReservationCycleProjection? first) : IReservationCycleDirectory
    {
        public Queue<ReservationCycleProjection?> Projections { get; } = new([first]);

        public int Reads { get; private set; }

        public Task<ReservationCycleProjection> GetProjectionAsync(
            Guid checkoutId,
            DateTimeOffset serverNow,
            string? supplyStatus,
            CancellationToken cancellationToken)
        {
            Reads++;
            var next = Projections.Count > 0 ? Projections.Dequeue() : null;
            if (Projections.Count == 0 && next is not null)
            {
                // A steady state keeps answering with the last observation.
                Projections.Enqueue(next);
            }

            return Task.FromResult(next!);
        }

        public global::Tooba.Order.Domain.Reservation.ReservationCycle PrepareStart(
            Guid checkoutId,
            ReservationCycleReason reason,
            DateTimeOffset startedAt,
            DateTimeOffset expiresAt,
            ReservationCyclePolicySnapshot policy,
            IReadOnlyList<Guid> reservationIds,
            string? actor,
            string? correlationId,
            Guid? paymentAttemptId) => throw new NotSupportedException();

        public Task<ReservationCycleSnapshot> StartAsync(
            Guid checkoutId,
            ReservationCycleReason reason,
            DateTimeOffset startedAt,
            DateTimeOffset expiresAt,
            ReservationCyclePolicySnapshot policy,
            IReadOnlyList<Guid> reservationIds,
            string? actor,
            string? correlationId,
            Guid? paymentAttemptId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CorrelatePaymentAttemptAsync(Guid checkoutId, Guid? paymentAttemptId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task TransitionManualReviewAsync(
            Guid checkoutId,
            DateTimeOffset reviewExpiresAt,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CloseExpiredDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CloseActiveAsync(
            Guid checkoutId,
            ReservationCycleStatus status,
            DateTimeOffset endedAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordReacquireRequestedAsync(Guid checkoutId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RecordReacquireFailedAsync(
            Guid checkoutId,
            DateTimeOffset now,
            string detail,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordRetryLimitReachedAsync(Guid checkoutId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReservationCycleSnapshot?> GetActiveAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> CountCreatedAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, ReservationCycleProjection>> GetProjectionsAsync(
            IReadOnlyList<Guid> checkoutIds,
            DateTimeOffset serverNow,
            IReadOnlyDictionary<Guid, string?>? supplyByCheckout,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ReservationCycleEventSnapshot>> ListEventsAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class InventoryFake : IInventoryPaymentHoldExtensionPort
    {
        private DateTimeOffset? _lastDeadline;

        public int Calls { get; private set; }

        public bool Monotonic { get; private set; } = true;

        public IReadOnlyList<Guid>? LastReservationIds { get; private set; }

        public InventoryPaymentHoldExtensionResult? Result { get; set; }

        public Task<InventoryPaymentHoldExtensionResult> ExtendHeldForPaymentAsync(
            IReadOnlyCollection<Guid> reservationIds,
            DateTimeOffset payWindowDeadlineUtc,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastReservationIds = reservationIds.ToArray();
            if (_lastDeadline is { } previous && payWindowDeadlineUtc < previous)
            {
                Monotonic = false;
            }

            _lastDeadline = payWindowDeadlineUtc;
            return Task.FromResult(Result ?? InventoryPaymentHoldExtensionResult.Extended(reservationIds.Count, payWindowDeadlineUtc));
        }
    }

    private sealed class PolicyFake : ICommerceHoldPolicySource
    {
        public Dictionary<string, int> HoursByProvider { get; } = new(StringComparer.Ordinal);

        public List<string> RequestedProviderCodes { get; } = [];

        public int Calls => RequestedProviderCodes.Count;

        public DateTimeOffset ResolveUnpaidTimeoutAt(string providerCode, DateTimeOffset utcNow)
        {
            RequestedProviderCodes.Add(providerCode);
            var hours = HoursByProvider.TryGetValue(providerCode, out var configured) ? configured : 7;
            return utcNow.AddHours(hours);
        }

        public int ResolveOnlineHoldHours(string? providerCode) => throw new NotSupportedException();

        public int ResolveManualInitialHoldHours(string? providerCode) => throw new NotSupportedException();

        public int ResolveManualReviewHoldHours(string? providerCode) => throw new NotSupportedException();

        public DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow) => throw new NotSupportedException();

        public DateTimeOffset ResolveManualReviewExpiresAt(DateTimeOffset utcNow) => throw new NotSupportedException();
    }
}
