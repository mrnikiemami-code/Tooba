using System.Text.Json;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;
using Xunit;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-270-R2 — the reviewed OWN dispatch plan of ONE existing <c>Created</c> resend/replacement parcel.
/// </summary>
/// <remarks>
/// <para>
/// These cases run the REAL public orchestrator boundary with controlled ports (checkout/access/fulfillment facts,
/// accepted binding, captured scheduling inputs, plan store). The calendar and the deadline triple come from the ONE
/// existing <see cref="ShippingDispatchSchedulingAuthority"/> — nothing is recomputed locally — and the immutable row
/// is built by the real <see cref="OrderShipmentDispatchPlan"/> aggregate.
/// </para>
/// <para>
/// The store is in-memory here, so these cases prove command/intent semantics only. Relational durability and the
/// <c>shipment_id</c> unique arbitration are proven in <c>AdminShipmentDispatchPlan270R2RelationalTests</c>.
/// </para>
/// </remarks>
public sealed class AdminShipmentPlan270R2Tests
{
    private static readonly Guid Checkout = Guid.Parse("01a1d270-1000-7000-8000-000000000001");
    private static readonly Guid SellerOrder = Guid.Parse("01a1d270-1000-7000-8000-000000000002");
    private static readonly Guid SellerParty = Guid.Parse("01a1d270-1000-7000-8000-000000000003");
    private static readonly Guid Line = Guid.Parse("01a1d270-1000-7000-8000-000000000004");
    private static readonly Guid Fulfillment = Guid.Parse("01a1d270-1000-7000-8000-000000000005");
    private static readonly Guid SourceShipment = Guid.Parse("01a1d270-1000-7000-8000-000000000006");
    private static readonly Guid Group = Guid.Parse("01a1d270-1000-7000-8000-000000000007");
    private static readonly Guid Actor = Guid.Parse("01a1d270-1000-7000-8000-000000000008");
    private static readonly Guid Snapshot = Guid.Parse("01a1d270-1000-7000-8000-000000000009");
    private static readonly Guid Draft = Guid.Parse("01a1d270-1000-7000-8000-00000000000a");
    private static readonly Guid AreaGroup = Guid.Parse("01a1d270-1000-7000-8000-00000000000b");
    private static readonly Guid RateRow = Guid.Parse("01a1d270-1000-7000-8000-00000000000c");
    private static readonly Guid Decision = Guid.Parse("01a1d270-1000-7000-8000-00000000000d");
    private static readonly Guid Hold = Guid.Parse("01a1d270-1000-7000-8000-00000000000e");

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReceivedAt = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------- the parcel becomes scheduled

    [Fact]
    public async Task Preview_of_a_live_created_resend_offers_feasible_customer_dates_without_writing()
    {
        var fixture = Fixture.Resend();
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        var value = preview.Value!;
        Assert.Equal(fixture.ResendShipmentId, value.ShipmentId);
        Assert.Equal(OrderShipmentAttemptKinds.Resend, value.AttemptKind);
        Assert.Equal(SourceShipment, value.SourceShipmentId);
        Assert.Equal(OrderShipmentReadinessSources.ReturnReceipt, value.ReadinessSource);
        Assert.Equal(Group, value.ShippingGroupId);
        Assert.Equal(Snapshot, value.SnapshotId);
        Assert.Equal(RateRow, value.RateRowId);
        Assert.Null(value.ConfirmedPlan);
        Assert.Null(value.BlockerCode);
        Assert.NotEmpty(value.Options);

        foreach (var option in value.Options)
        {
            // The triple keeps its distinct meaning and is never before the review anchor.
            Assert.True(option.PreparationStartUtc >= Now);
            Assert.True(option.PreparationStartUtc <= option.TransferDepartureUtc);
            Assert.True(option.TransferDepartureUtc <= option.HandoffDeadlineUtc);
            Assert.True(option.HandoffDate <= option.ReceivingDate);
            Assert.Equal("Asia/Tehran", option.ReceivingTimeZone);
            Assert.Equal("Asia/Tehran", option.HandoffTimeZone);
        }

        // A preview is a read: no plan row exists afterwards.
        Assert.Empty(fixture.Store.Rows);
    }

    [Fact]
    public async Task Confirm_writes_one_immutable_plan_and_an_exact_replay_answers_the_same_row()
    {
        var fixture = Fixture.Resend();
        var option = await fixture.FirstOptionAsync();

        var first = await fixture.PlanAsync(option, "tb270r2-key-1");
        Assert.True(first.IsSuccess, Code(first));
        var body = Json(first.Value!);
        Assert.False(body.GetProperty("replayed").GetBoolean());
        var planId = body.GetProperty("planId").GetGuid();

        var stored = Assert.Single(fixture.Store.Rows);
        Assert.Equal(fixture.ResendShipmentId, stored.ShipmentId);
        Assert.Equal(OrderShipmentAttemptKinds.Resend, stored.AttemptKind);
        Assert.Equal(SourceShipment, stored.SourceShipmentId);
        Assert.Equal(Group, stored.ShippingGroupId);
        Assert.Equal(option.ReceivingDate, stored.CustomerReceivingDate);
        Assert.Equal(option.PreparationStartUtc, stored.PreparationStartUtc);
        Assert.Equal(option.TransferDepartureUtc, stored.TransferDepartureUtc);
        Assert.Equal(option.HandoffDeadlineUtc, stored.HandoffDeadlineUtc);
        Assert.Equal(120, stored.PreparationMinutes);
        Assert.Equal(60, stored.HandoffMinutes);
        Assert.Equal(2, stored.TransportWorkingDays);
        Assert.Equal(ShippingDispatchSchedulingAuthority.WindowAwarePolicyVersion, stored.PolicyVersion);
        Assert.Equal(Actor, stored.ConfirmedByUserId);
        Assert.Equal("tb270r2-key-1", stored.OccurrenceKey);
        // The source receipt is the readiness proof, not the browser clock and not the order instant.
        Assert.Equal(OrderShipmentReadinessSources.ReturnReceipt, stored.ReadinessSource);
        Assert.Equal(ReceivedAt, stored.ReadinessAtUtc);

        var replay = await fixture.PlanAsync(option, "tb270r2-key-1");
        Assert.True(replay.IsSuccess, Code(replay));
        var replayBody = Json(replay.Value!);
        Assert.True(replayBody.GetProperty("replayed").GetBoolean());
        Assert.Equal(planId, replayBody.GetProperty("planId").GetGuid());
        Assert.Single(fixture.Store.Rows);
    }

    [Fact]
    public async Task The_confirmed_plan_is_read_from_storage_after_time_elapsed_and_is_never_recalculated()
    {
        var fixture = Fixture.Resend();
        var option = await fixture.FirstOptionAsync();
        Assert.True((await fixture.PlanAsync(option, "tb270r2-key-elapsed")).IsSuccess);
        var stored = Assert.Single(fixture.Store.Rows);

        // A NEW context, a year later: the committed promise is history. It is answered exactly, not recomputed, and
        // the elapsed promise is never clamped to a new date.
        var later = fixture.WithClock(new FixedClock(stored.HandoffDeadlineUtc.AddDays(365)));
        var replay = await later.PlanAsync(option, "tb270r2-key-elapsed");

        Assert.True(replay.IsSuccess, Code(replay));
        var body = Json(replay.Value!);
        Assert.True(body.GetProperty("replayed").GetBoolean());
        Assert.Equal(stored.PlanId, body.GetProperty("planId").GetGuid());
        Assert.Single(fixture.Store.Rows);

        var preview = await later.PreviewAsync(fixture.ResendShipmentId);
        Assert.True(preview.IsSuccess, Code(preview));
        Assert.NotNull(preview.Value!.ConfirmedPlan);
        Assert.Equal(stored.PlanId, preview.Value.ConfirmedPlan!.PlanId);
        Assert.Equal(stored.CustomerReceivingDate, preview.Value.ConfirmedPlan.ReceivingDate);
    }

    // ---------------------------------------------------------------- intent, replay and expiry

    [Fact]
    public async Task Same_key_with_a_changed_selection_is_a_conflict_with_zero_extra_write()
    {
        var fixture = Fixture.Resend();
        var options = await fixture.OptionsAsync();
        Assert.True(options.Count >= 2, "the fixture needs at least two feasible options");
        Assert.True((await fixture.PlanAsync(options[0], "tb270r2-key-conflict")).IsSuccess);

        var conflict = await fixture.PlanAsync(options[1], "tb270r2-key-conflict");

        Assert.False(conflict.IsSuccess);
        Assert.Equal("order.shipment_plan.conflict", Code(conflict));
        var stored = Assert.Single(fixture.Store.Rows);
        Assert.Equal(options[0].ReceivingDate, stored.CustomerReceivingDate);
        Assert.Equal(TicksOf(options[0].ReceivingOpensAt), stored.CustomerReceivingOpenTicks);
    }

    [Fact]
    public async Task A_different_key_never_overwrites_the_one_confirmed_plan()
    {
        var fixture = Fixture.Resend();
        var options = await fixture.OptionsAsync();
        Assert.True(options.Count >= 2, "the fixture needs at least two feasible options");
        Assert.True((await fixture.PlanAsync(options[0], "tb270r2-key-owner")).IsSuccess);
        var winner = Assert.Single(fixture.Store.Rows);

        var second = await fixture.PlanAsync(options[1], "tb270r2-key-intruder");

        Assert.False(second.IsSuccess);
        Assert.Equal("order.shipment_plan.conflict", Code(second));
        var stored = Assert.Single(fixture.Store.Rows);
        Assert.Equal(winner.PlanId, stored.PlanId);
        Assert.Equal("tb270r2-key-owner", stored.OccurrenceKey);
    }

    [Fact]
    public async Task A_selection_outside_the_fresh_calendar_is_refused_as_expired()
    {
        var fixture = Fixture.Resend();
        var expired = new AdminShipmentDispatchPlanOption(
            new DateOnly(2030, 1, 1),
            "00:00",
            "00:30",
            "Asia/Tehran",
            new DateOnly(2029, 12, 30),
            "08:00",
            "20:00",
            "Asia/Tehran",
            new DateTimeOffset(2029, 12, 29, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2029, 12, 30, 3, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2029, 12, 30, 16, 30, 0, TimeSpan.Zero));

        var refused = await fixture.PlanAsync(expired, "tb270r2-key-expired");

        Assert.False(refused.IsSuccess);
        Assert.Equal("order.shipment_plan.selection_expired", Code(refused));
        Assert.Empty(fixture.Store.Rows);
    }

    [Fact]
    public async Task A_request_without_a_reviewed_selection_is_refused()
    {
        var fixture = Fixture.Resend();
        var request = new AdminOrderOperationRequest(
            "plan_shipment_dispatch", SellerOrder, Fulfillment, fixture.ResendShipmentId, null, null, null, null,
            "tb270r2-key-no-selection", null);

        var refused = await fixture.Orchestrator.PlanShipmentDispatchAsync(
            Checkout, Actor, request, CancellationToken.None);

        Assert.False(refused.IsSuccess);
        Assert.Equal("order.shipment_plan.selection_required", Code(refused));
        Assert.Empty(fixture.Store.Rows);
    }

    // ---------------------------------------------------------------- truthful blockers (no invented date)

    [Fact]
    public async Task A_stock_pending_replacement_hold_is_an_honest_blocker_and_offers_no_date()
    {
        var fixture = Fixture.Replacement(DamagedResolutionStatus.Recorded);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.Empty(preview.Value!.Options);
        Assert.Equal("order.shipment_plan.replacement_hold_missing", preview.Value.BlockerCode);
        Assert.False(string.IsNullOrWhiteSpace(preview.Value.BlockerFa));

        var refused = await fixture.PlanAsync(DummyOption(), "tb270r2-key-hold");
        Assert.False(refused.IsSuccess);
        Assert.Equal("order.shipment_plan.replacement_hold_missing", Code(refused));
        Assert.Empty(fixture.Store.Rows);
    }

    [Fact]
    public async Task A_reserved_replacement_hold_schedules_its_own_parcel()
    {
        var fixture = Fixture.Replacement(DamagedResolutionStatus.Reserved);
        var option = await fixture.FirstOptionAsync();

        var stored = await fixture.PlanAsync(option, "tb270r2-key-replacement");
        Assert.True(stored.IsSuccess, Code(stored));

        var row = Assert.Single(fixture.Store.Rows);
        Assert.Equal(OrderShipmentAttemptKinds.Replacement, row.AttemptKind);
        Assert.Equal(OrderShipmentReadinessSources.ReplacementHold, row.ReadinessSource);
        Assert.Equal(Decision, row.DamagedResolutionId);
        Assert.Equal(Hold, row.ReplacementReservationId);
        // The replacement is funded by its OWN purpose-bound hold, never by the damaged units.
        Assert.Equal(SourceShipment, row.SourceShipmentId);
    }

    [Fact]
    public async Task Only_the_sources_usable_received_quantity_funds_the_resend()
    {
        // The parcel carries 2 units but the source receipt is 1 usable + 1 damaged.
        var fixture = Fixture.Resend(parcelQuantity: 2m, usable: 1m, damaged: 1m);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.Equal("order.shipment_plan.source_quantity_mismatch", preview.Value!.BlockerCode);
        Assert.Empty(preview.Value.Options);
        Assert.Empty(fixture.Store.Rows);
    }

    [Fact]
    public async Task A_parcel_without_a_usable_origin_receipt_is_not_plannable()
    {
        var fixture = Fixture.Resend(receipt: false);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.Equal("order.shipment_plan.source_not_ready", preview.Value!.BlockerCode);
        Assert.Empty(preview.Value.Options);
    }

    [Fact]
    public async Task An_incomplete_historical_capture_is_a_typed_blocker_and_never_a_guessed_week()
    {
        var fixture = Fixture.Resend(missingInputs: [ShippingAttemptSchedulingInputCodes.CarrierWeek]);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.Equal("order.shipment_plan.captured_inputs_incomplete", preview.Value!.BlockerCode);
        Assert.Empty(preview.Value.Options);
        Assert.False(string.IsNullOrWhiteSpace(preview.Value.BlockerFa));
    }

    [Fact]
    public async Task A_refused_captured_read_keeps_its_own_stable_reason()
    {
        var fixture = Fixture.Resend(capturedRefusal: true);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.Equal(
            ShippingAttemptSchedulingReasonCodes.CapturedRevisionUnavailable,
            preview.Value!.BlockerCode);
        Assert.Empty(preview.Value.Options);
    }

    // ---------------------------------------------------------------- eligibility and boundary

    [Fact]
    public async Task The_first_dispatch_parcel_of_the_order_is_not_plannable_here()
    {
        var fixture = Fixture.Resend();
        var preview = await fixture.PreviewAsync(SourceShipment);

        Assert.False(preview.IsSuccess);
        Assert.Equal("order.shipment_plan.first_dispatch_not_plannable", Code(preview));
    }

    [Fact]
    public async Task A_dispatch_history_parcel_never_gains_a_planning_action_again()
    {
        var fixture = Fixture.Resend(dispatched: true);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.False(preview.IsSuccess);
        Assert.Equal("order.shipment_plan.not_created", Code(preview));
    }

    [Fact]
    public async Task An_actor_without_the_physical_operation_permission_is_denied_even_on_replay()
    {
        var fixture = Fixture.Resend(allowManage: false);

        var denied = await fixture.PlanAsync(DummyOption(), "tb270r2-key-denied");
        Assert.False(denied.IsSuccess);
        Assert.Equal("order.operation.denied", Code(denied));

        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);
        Assert.False(preview.IsSuccess);
        Assert.Equal("order.operation.denied", Code(preview));
        Assert.Empty(fixture.Store.Rows);
    }

    [Fact]
    public async Task An_unpaid_seller_order_cannot_be_scheduled()
    {
        var fixture = Fixture.Resend(paid: false);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.False(preview.IsSuccess);
        Assert.Equal("order.shipment_plan.payment_required", Code(preview));
    }

    // ---------------------------------------------------------------- helpers

    private static AdminOrderOperationRequest Request(
        Guid shipmentId,
        AdminShipmentDispatchPlanOption option,
        string key) =>
        new(
            "plan_shipment_dispatch",
            SellerOrder,
            Fulfillment,
            shipmentId,
            null,
            null,
            null,
            null,
            key,
            null,
            DispatchPlanReceivingDate: option.ReceivingDate.ToString("yyyy-MM-dd"),
            DispatchPlanReceivingOpensAt: option.ReceivingOpensAt,
            DispatchPlanReceivingClosesAt: option.ReceivingClosesAt);

    private static AdminShipmentDispatchPlanOption DummyOption() =>
        new(
            new DateOnly(2026, 11, 18),
            "09:00",
            "10:00",
            "Asia/Tehran",
            new DateOnly(2026, 11, 17),
            "08:00",
            "20:00",
            "Asia/Tehran",
            new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 17, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 17, 16, 30, 0, TimeSpan.Zero));

    private static long TicksOf(string value) =>
        TimeOnly.ParseExact(value, "HH\\:mm", System.Globalization.CultureInfo.InvariantCulture).Ticks;

    /// <summary>Success-safe code accessor: <c>FirstError</c> must never be read on a successful result.</summary>
    private static string Code<T>(Result<T> result) =>
        result.IsSuccess ? "success" : result.FirstError?.Code ?? "unknown";

    private static JsonElement Json(object value) =>
        JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class Fixture
    {
        private Fixture(
            InMemoryPlanStore store,
            AdminOrderOperationsOrchestrator orchestrator,
            Guid resendShipmentId,
            ScheduleFixture schedule)
        {
            Store = store;
            Orchestrator = orchestrator;
            ResendShipmentId = resendShipmentId;
            Schedule = schedule;
        }

        public InMemoryPlanStore Store { get; }

        public AdminOrderOperationsOrchestrator Orchestrator { get; }

        public Guid ResendShipmentId { get; }

        private ScheduleFixture Schedule { get; }

        public static Fixture Resend(
            decimal parcelQuantity = 1m,
            decimal usable = 1m,
            decimal damaged = 0m,
            bool receipt = true,
            bool dispatched = false,
            bool allowManage = true,
            bool paid = true,
            IReadOnlyList<string>? missingInputs = null,
            bool capturedRefusal = false) =>
            Build(
                replacement: null,
                parcelQuantity,
                usable,
                damaged,
                receipt,
                dispatched,
                allowManage,
                paid,
                missingInputs,
                capturedRefusal);

        public static Fixture Replacement(DamagedResolutionStatus status) =>
            Build(
                status,
                1m,
                1m,
                0m,
                receipt: true,
                dispatched: false,
                allowManage: true,
                paid: true,
                missingInputs: null,
                capturedRefusal: false);

        private static Fixture Build(
            DamagedResolutionStatus? replacement,
            decimal parcelQuantity,
            decimal usable,
            decimal damaged,
            bool receipt,
            bool dispatched,
            bool allowManage,
            bool paid,
            IReadOnlyList<string>? missingInputs,
            bool capturedRefusal)
        {
            var resendShipmentId = Guid.Parse("01a1d270-2000-7000-8000-000000000001");

            var receiptSnapshot = receipt
                ? new ReturnReceiptSnapshot(
                    Guid.NewGuid(),
                    ReceivedAt,
                    Actor,
                    "TB270R2",
                    [
                        new ReturnReceiptLineSnapshot(Line, usable, ReturnReceiptCondition.Usable),
                        new ReturnReceiptLineSnapshot(Line, damaged, ReturnReceiptCondition.Damaged),
                    ])
                : null;

            var decisions = replacement is { } status
                ? new List<DamagedResolutionSnapshot>
                {
                    new(
                        Decision,
                        SourceShipment,
                        Guid.NewGuid(),
                        Line,
                        parcelQuantity,
                        DamagedResolutionOutcome.Replacement,
                        status,
                        Actor,
                        ReceivedAt,
                        ReceivedAt,
                        Hold,
                        resendShipmentId,
                        "TB270R2 damaged",
                        "tb270r2-decision"),
                }
                : null;

            var source = new ShipmentSnapshot(
                SourceShipment,
                ShipmentOperationStatus.Returned,
                "پیشتاز",
                "TRK-TB270R2-SOURCE",
                Now.AddDays(-4),
                null,
                [new ShipmentLineSnapshot(Line, parcelQuantity)],
                Now.AddDays(-6),
                "store_courier",
                "پیشتاز",
                ShippingGroupId: Group,
                NonDeliveryReason: "customer_unavailable",
                NonDeliveredAt: Now.AddDays(-3),
                ReturnStartedAt: Now.AddDays(-2),
                ReturnedAt: receipt ? ReceivedAt : null,
                ReturnReceipt: receiptSnapshot,
                DamagedResolutions: decisions);

            var decisionId = replacement is null ? (Guid?)null : Decision;
            var target = new ShipmentSnapshot(
                resendShipmentId,
                dispatched ? ShipmentOperationStatus.Dispatched : ShipmentOperationStatus.Created,
                "پیشتاز",
                dispatched ? "TRK-TB270R2-RESEND" : null,
                dispatched ? Now.AddDays(-1) : null,
                null,
                [new ShipmentLineSnapshot(Line, parcelQuantity)],
                Now.AddDays(-1),
                "store_courier",
                "پیشتاز",
                ShippingGroupId: Group,
                ResendOfShipmentId: SourceShipment,
                DamagedResolutionId: decisionId,
                ReplacementReservationId: replacement is null ? null : Hold,
                IsReplacement: replacement is not null);

            var unit = new FulfillmentSnapshot(
                Fulfillment,
                SellerOrder,
                Checkout,
                SellerParty,
                FulfillmentOperationStatus.Packed,
                "محمد امامی",
                "09121234567",
                "تهران",
                "تهران",
                "تهران خیابان آزمایش ۱",
                "1234567890",
                "store_courier",
                "پیشتاز",
                [new FulfillmentItemSnapshot(Guid.NewGuid(), Line, parcelQuantity, 0m, null)],
                [source, target],
                Now.AddDays(-7),
                Now);

            var bindings = new BindingFake(new AcceptedShipmentGroupBinding(
                Checkout,
                SellerOrder,
                SellerParty,
                Group,
                "store_courier",
                "پیشتاز",
                "store_courier",
                null,
                ShippingGroupOwnerKind.Seller,
                "store-alpha",
                SellerParty,
                Snapshot,
                Draft,
                7,
                4,
                9,
                AreaGroup,
                RateRow,
                new DateOnly(2026, 11, 18),
                new TimeOnly(9, 0).Ticks,
                new TimeOnly(10, 0).Ticks,
                "Asia/Tehran",
                1,
                2));

            var lookup = new LookupFake(capturedRefusal, BuildInputs(missingInputs));
            var store = new InMemoryPlanStore();
            var access = new AccessFake(allowManage);
            var fulfillment = new FulfillmentFake(unit);
            var checkouts = new CheckoutReader(paid);

            var schedule = new ScheduleFixture(checkouts, fulfillment, bindings, lookup, store, access);
            return new Fixture(store, schedule.CreateOrchestrator(new FixedClock(Now)), resendShipmentId, schedule);
        }

        public Fixture WithClock(IClock clock) =>
            new(Store, Schedule.CreateOrchestrator(clock), ResendShipmentId, Schedule);

        public Task<Result<AdminShipmentDispatchPlanPreview>> PreviewAsync(Guid shipmentId) =>
            Orchestrator.PreviewShipmentDispatchPlanAsync(Checkout, Actor, shipmentId, CancellationToken.None);

        public async Task<IReadOnlyList<AdminShipmentDispatchPlanOption>> OptionsAsync()
        {
            var preview = await PreviewAsync(ResendShipmentId);
            return preview.IsSuccess ? preview.Value!.Options : [];
        }

        public async Task<AdminShipmentDispatchPlanOption> FirstOptionAsync()
        {
            var preview = await PreviewAsync(ResendShipmentId);
            Assert.True(preview.IsSuccess, Code(preview));
            Assert.False(
                preview.Value!.Options.Count == 0,
                $"no feasible option (blocker={preview.Value.BlockerCode})");
            return preview.Value.Options[0];
        }

        public Task<Result<object>> PlanAsync(AdminShipmentDispatchPlanOption option, string key) =>
            Orchestrator.PlanShipmentDispatchAsync(
                Checkout, Actor, Request(ResendShipmentId, option, key), CancellationToken.None);
    }

    private sealed record ScheduleFixture(
        CheckoutReader Checkouts,
        FulfillmentFake Fulfillment,
        BindingFake Bindings,
        LookupFake Lookup,
        InMemoryPlanStore Store,
        AccessFake Access)
    {
        public AdminOrderOperationsOrchestrator CreateOrchestrator(IClock clock) =>
            new(
                Checkouts,
                Fulfillment,
                null!,
                null!,
                Access,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                Store,
                Bindings,
                Lookup,
                clock);
    }

    private static ShippingAttemptSchedulingInputs BuildInputs(IReadOnlyList<string>? missingInputs) =>
        new(
            ShippingCapturedQuoteOwnerKind.Seller,
            "store-alpha",
            SellerParty,
            Snapshot,
            Draft,
            7,
            4,
            "store_courier",
            null,
            Group,
            RateRow,
            1,
            2,
            120,
            60,
            ShippingAuthoringPreparationProcessingMode.WorkingWeek,
            new ShippingAttemptReceivingWeekView("Asia/Tehran", Days(9, 18), 60),
            new ShippingSchedulingWeekView("Asia/Tehran", Days(8, 20)),
            new ShippingSchedulingWeekView("Asia/Tehran", Days(8, 18)),
            missingInputs ?? []);

    private static IReadOnlyList<ShippingSchedulingDayView> Days(int opensAt, int closesAt) =>
        Enumerable.Range(1, 7)
            .Select(day => new ShippingSchedulingDayView(
                day,
                true,
                new TimeOnly(opensAt, 0),
                new TimeOnly(closesAt, 0)))
            .ToList();

    private sealed class CheckoutReader(bool paid) : IAdminOrderOperationsCheckoutReader
    {
        public Task<AdminOrderOpsCheckoutSnapshot?> GetAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<AdminOrderOpsCheckoutSnapshot?>(
                checkoutId == Checkout
                    ? new AdminOrderOpsCheckoutSnapshot(
                        Checkout,
                        Actor,
                        [new AdminOrderOpsSellerOrderSnapshot(
                            SellerOrder,
                            SellerParty,
                            paid ? SellerOrderStatus.Paid : SellerOrderStatus.PendingPayment,
                            null,
                            [])])
                    : null);
    }

    private sealed class AccessFake(bool allowManage) : IOrderAdminEffectiveAccessReader
    {
        public Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult(new OrderAdminEffectiveAccess(
                allowManage
                    ? [new OrderAdminPermissionGrant("fulfillment.manage", false)]
                    : [new OrderAdminPermissionGrant("order.view", false)]));
    }

    private sealed class BindingFake(AcceptedShipmentGroupBinding binding) : IOrderAcceptedShipmentBindingReader
    {
        public Task<AcceptedShipmentGroupBinding?> GetAsync(
            Guid checkoutId,
            Guid shippingGroupId,
            CancellationToken cancellationToken) =>
            Task.FromResult<AcceptedShipmentGroupBinding?>(
                checkoutId == binding.CheckoutId && shippingGroupId == binding.ShippingGroupId ? binding : null);
    }

    private sealed class LookupFake(bool refuse, ShippingAttemptSchedulingInputs inputs)
        : IShippingAttemptSchedulingLookup
    {
        public Task<ShippingAttemptSchedulingResult> LookupAsync(
            ShippingAttemptSchedulingLookupRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                refuse
                    ? ShippingAttemptSchedulingResult.Refused(
                        ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable,
                        ShippingAttemptSchedulingReasonCodes.CapturedRevisionUnavailable)
                    : new ShippingAttemptSchedulingResult(
                        ShippingAttemptSchedulingVerdict.CapturedInputs,
                        ShippingAttemptSchedulingReasonCodes.CapturedInputs,
                        inputs));
    }

    private sealed class InMemoryPlanStore : IOrderShipmentDispatchPlanStore
    {
        public List<OrderShipmentDispatchPlan> Rows { get; } = [];

        public Task<OrderShipmentDispatchPlan?> FindAsync(Guid shipmentId, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.SingleOrDefault(x => x.ShipmentId == shipmentId));

        public Task<IReadOnlyList<OrderShipmentDispatchPlan>> ListAsync(
            IReadOnlyList<Guid> shipmentIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderShipmentDispatchPlan>>(
                Rows.Where(x => shipmentIds.Contains(x.ShipmentId)).ToList());

        public Task<OrderShipmentDispatchPlanInsertOutcome> TryInsertAsync(
            OrderShipmentDispatchPlan plan,
            CancellationToken cancellationToken)
        {
            var existing = Rows.SingleOrDefault(x => x.ShipmentId == plan.ShipmentId);
            if (existing is not null)
            {
                return Task.FromResult(existing.HasSameIntent(plan)
                    ? OrderShipmentDispatchPlanInsertOutcome.DuplicateExact
                    : OrderShipmentDispatchPlanInsertOutcome.DuplicateConflict);
            }

            Rows.Add(plan);
            return Task.FromResult(OrderShipmentDispatchPlanInsertOutcome.Inserted);
        }
    }

    private sealed class FulfillmentFake(FulfillmentSnapshot unit) : IFulfillmentAdminOperations
    {
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FulfillmentSnapshot>>(unit.CheckoutId == checkoutId ? [unit] : []);

        public Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(
            IReadOnlyList<Guid> shipmentIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ActivePackageMembershipSnapshot>>([]);

        public Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ConsolidatedPackageSnapshot>>([]);

        public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentSnapshot?>(fulfillmentId == unit.FulfillmentId ? unit : null);

        public IReadOnlyList<ShippingMethodOption> ListEnabledShippingMethods() => [];

        public string ResolveShippingMethodLabel(string? code, string? fallbackDisplayName) =>
            fallbackDisplayName ?? code ?? string.Empty;

        public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> ProcessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> UnprocessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> PackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> UnpackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(Guid fulfillmentId, Guid actorUserId, string carrierDisplayName, IReadOnlyList<ShipmentLineCommand> items, string? shippingMethodCode, string? providerMetadataJson, CancellationToken cancellationToken, Guid? shippingGroupId = null, string? shippingDispatchKey = null) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> CancelShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> AssignTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> CorrectTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> DispatchShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> DeliverShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> RecordNonDeliveryAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string reason, string? note, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> StartReturnAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> RecordReturnReceiptAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, IReadOnlyList<ReturnReceiptLineCommand> items, string? note, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentShipmentCreateResult> CreateResendShipmentAsync(Guid fulfillmentId, Guid actorUserId, Guid originalShipmentId, IReadOnlyList<ShipmentLineCommand> items, CancellationToken cancellationToken, string? shippingDispatchKey = null) => throw new NotSupportedException();
        public Task<PostDispatchIntentResult> ReconcilePostDispatchIntentAsync(PostDispatchIntent intent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DamagedResolutionResult> ResolveDamagedGoodsAsync(Guid fulfillmentId, Guid actorUserId, Guid shipmentId, Guid orderLineId, decimal quantity, DamagedResolutionOutcome outcome, string? occurrenceKey, string? note, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DamagedResolutionIntentResult> ReconcileDamagedResolutionAsync(DamagedResolutionIntent intent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(Guid checkoutId, IReadOnlyList<Guid> shipmentIds, string? shippingMethodCode, string? trackingReference, string? note, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(Guid consolidatedPackageId, string trackingReference, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EnsureCreatedForPaidCheckoutAsync(Guid checkoutId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
