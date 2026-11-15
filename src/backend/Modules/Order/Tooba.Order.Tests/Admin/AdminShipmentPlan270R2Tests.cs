using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Scheduling;
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

    /// <summary>The ONE store tenant context the connected capture is authored under.</summary>
    private const string Tenant = "store-alpha";

    /// <summary>The single captured destination node of the connected capture (identity only, never a rate input).</summary>
    private static readonly Guid City = Guid.Parse("01a1d270-1000-7000-8000-00000000000f");

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

    // ---------------------------------------------------------------- TB-270-R3 repairs

    [Fact]
    public async Task Preview_addresses_the_CAPTURED_area_group_and_rate_row_not_the_order_shipping_group()
    {
        var fixture = Fixture.Resend();
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.NotEmpty(preview.Value!.Options);
        var request = Assert.IsType<ShippingAttemptSchedulingLookupRequest>(fixture.LastLookupRequest);
        Assert.Equal(AreaGroup, request.GroupId);
        Assert.Equal(RateRow, request.RateRowId);
        Assert.NotEqual(Group, request.GroupId);
        // The Order-side group identity is still what the plan reports: the two namespaces stay distinct.
        Assert.Equal(Group, preview.Value.ShippingGroupId);
    }

    [Fact]
    public async Task A_capture_that_answers_for_the_order_shipping_group_is_refused_as_a_binding_mismatch()
    {
        var fixture = Fixture.Resend(capturedUsesShippingGroup: true);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.Equal("order.shipment_plan.binding_mismatch", preview.Value!.BlockerCode);
        Assert.Empty(preview.Value.Options);
    }

    [Fact]
    public async Task Confirmation_without_the_reviewed_fingerprint_requires_a_fresh_review()
    {
        var fixture = Fixture.Resend();
        var option = await fixture.FirstOptionAsync();

        var refused = await fixture.Orchestrator.PlanShipmentDispatchAsync(
            Checkout,
            Actor,
            Request(fixture.ResendShipmentId, option, "tb270r3-key-no-fingerprint"),
            CancellationToken.None);

        Assert.False(refused.IsSuccess);
        Assert.Equal("order.shipment_plan.review_required", Code(refused));
        Assert.Empty(fixture.Store.Rows);
    }

    [Fact]
    public async Task A_changed_reviewed_context_is_refused_instead_of_being_silently_replanned()
    {
        var fixture = Fixture.Resend();
        var options = await fixture.OptionsAsync();
        Assert.True(options.Count >= 2, "the fixture needs at least two feasible options");
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);
        var reviewed = preview.Value!.ReviewFingerprint;
        Assert.False(string.IsNullOrWhiteSpace(reviewed));

        var stale = await fixture.Orchestrator.PlanShipmentDispatchAsync(
            Checkout,
            Actor,
            Request(fixture.ResendShipmentId, options[1], "tb270r3-key-stale", "v1:0000000000000000"),
            CancellationToken.None);

        Assert.False(stale.IsSuccess);
        Assert.Equal("order.shipment_plan.review_context_changed", Code(stale));
        Assert.Empty(fixture.Store.Rows);

        // The SAME reviewed context still confirms normally.
        var confirmed = await fixture.PlanAsync(options[1], "tb270r3-key-echoed");
        Assert.True(confirmed.IsSuccess, Code(confirmed));
        Assert.Single(fixture.Store.Rows);
    }

    [Fact]
    public async Task A_replacement_readiness_fact_for_another_parcel_never_authorizes_this_one()
    {
        var replacement = Fixture.Replacement(DamagedResolutionStatus.Reserved, identityMismatch: true);
        var blocked = await replacement.PreviewAsync(replacement.ResendShipmentId);

        Assert.True(blocked.IsSuccess, Code(blocked));
        Assert.Equal("order.shipment_plan.replacement_hold_missing", blocked.Value!.BlockerCode);
        Assert.Empty(blocked.Value.Options);
        Assert.True(replacement.ReadinessCalls >= 1, "the owning readiness read is consulted");
    }

    [Fact]
    public async Task A_ready_replacement_reads_its_hold_from_the_owning_fact()
    {
        var fixture = Fixture.Replacement(DamagedResolutionStatus.Reserved);
        var option = await fixture.FirstOptionAsync();
        Assert.True((await fixture.PlanAsync(option, "tb270r3-key-hold")).IsSuccess);

        var row = Assert.Single(fixture.Store.Rows);
        Assert.Equal(OrderShipmentReadinessSources.ReplacementHold, row.ReadinessSource);
        Assert.Equal(Hold, row.ReplacementReservationId);
        Assert.True(fixture.ReadinessCalls >= 1);
    }

    // ------------------------------------------------- connected captured seam (Repair A)

    /// <summary>
    /// TB-270-R3 Repair A: ONE connected application fact. The REAL R1 Shipping adapter reads a REAL captured
    /// revision — canonical encoder, real EF store, real owner-scope resolver — instead of a fake that would answer
    /// any group id it is handed. The captured ids are read back from the payload, never assumed.
    /// </summary>
    private sealed record ConnectedCapture(
        IShippingAttemptSchedulingLookup Lookup,
        Guid SnapshotId,
        Guid DraftId,
        long SourceRevision,
        int FormatVersion,
        Guid AreaGroupId,
        Guid RateRowId,
        int MinimumDeliveryDays,
        int MaximumDeliveryDays);

    private static ConnectedCapture BuildConnectedCapture()
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore(Tenant),
            "store_courier",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [new ShippingDestinationNode(ShippingDestinationNodeKind.City, City)])],
            Now.AddHours(-3));

        var groupId = draft.RegionGroups.Single().GroupId;
        draft.ReplaceGroupRateRows(
            groupId,
            [new(null, null, null, null, null, null, null, 50_000.00m, 2_000.00m, 1, 2)],
            "IRT",
            Now.AddHours(-3).AddMinutes(1));

        // The customer receiving week, the carrier acceptance week and the warehouse processing week are three
        // SEPARATE stored calendars; nothing here is a default and nothing is collapsed into one week.
        draft.ApplyWeeklySchedule(
            draft.PreflightWeeklySchedule(
                "Asia/Tehran",
                [
                    new(1, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
                    new(2, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
                    new(3, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
                    new(4, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
                    new(5, false, null, null),
                    new(6, true, new TimeOnly(9, 0), new TimeOnly(14, 0)),
                    new(7, false, null, null),
                ],
                60),
            Now.AddHours(-3).AddMinutes(2));

        draft.ApplyCarrierSchedule(
            draft.PreflightCarrierSchedule(
                "Asia/Tehran",
                [
                    new(1, true, new TimeOnly(8, 0), new TimeOnly(20, 0)),
                    new(2, true, new TimeOnly(8, 0), new TimeOnly(20, 0)),
                    new(3, true, new TimeOnly(8, 0), new TimeOnly(20, 0)),
                    new(4, true, new TimeOnly(8, 0), new TimeOnly(20, 0)),
                    new(5, true, new TimeOnly(8, 0), new TimeOnly(20, 0)),
                    new(6, true, new TimeOnly(8, 0), new TimeOnly(16, 0)),
                    new(7, false, null, null),
                ]),
            Now.AddHours(-3).AddMinutes(3));

        draft.ApplyProcessingSchedule(
            draft.PreflightProcessingSchedule(
                "Asia/Tehran",
                [
                    new(1, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                    new(2, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                    new(3, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                    new(4, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                    new(5, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                    new(6, true, new TimeOnly(8, 0), new TimeOnly(14, 0)),
                    new(7, false, null, null),
                ]),
            Now.AddHours(-3).AddMinutes(4));

        draft.ApplyMethodSettings(
            draft.PreflightMethodSettings(
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual,
                120,
                60,
                ShippingAuthoringPreparationProcessingMode.WorkingWeek),
            Now.AddHours(-3).AddMinutes(5));

        draft.ApplyTypeAssignment(draft.PreflightTypeAssignment(null), Now.AddHours(-3).AddMinutes(6));

        // The ONE canonical capture path: the row is written by the real encoder, not by hand-written JSON.
        var snapshot = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(-2), Snapshot);
        var payload = snapshot.ReadPayload();
        var capturedGroup = payload.Groups.Single();
        var capturedRow = capturedGroup.RateRows.Single();
        Assert.Equal(ShippingAuthoringRevisionSnapshot.ProcessingHoursFormatVersion, snapshot.FormatVersion);
        Assert.NotNull(payload.MethodSettings);
        Assert.NotNull(payload.CarrierAcceptance);
        Assert.NotNull(payload.ProcessingHours);

        var context = new ShippingDbContext(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb270r3-connected-{Guid.NewGuid():N}")
            .Options);
        context.AuthoringRevisionSnapshots.Add(snapshot);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var lookup = new ShippingAttemptSchedulingLookup(
            new ShippingAuthoringDraftDirectory(context),
            new FixedEdition(),
            new FixedTenant(Tenant),
            NullLogger<ShippingAttemptSchedulingLookup>.Instance);

        return new ConnectedCapture(
            lookup,
            snapshot.SnapshotId,
            snapshot.DraftId,
            snapshot.SourceRevision,
            snapshot.FormatVersion,
            capturedGroup.GroupId,
            capturedRow.RateRowId,
            capturedRow.MinDeliveryDays,
            capturedRow.MaxDeliveryDays);
    }

    // ------------------------------------------------- connected captured seam (Repair A)

    [Fact]
    public async Task The_real_captured_seam_resolves_the_area_group_and_the_created_parcel_gets_its_own_plan()
    {
        var fixture = Fixture.Connected();
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        var value = preview.Value!;
        Assert.Null(value.BlockerCode);
        Assert.NotEmpty(value.Options);

        // The accepted evidence keeps BOTH identities distinct: the checkout group and the captured area group.
        Assert.Equal(Group, value.ShippingGroupId);
        Assert.NotNull(value.AreaGroupId);
        Assert.NotEqual(Group, value.AreaGroupId);
        Assert.NotNull(value.RateRowId);
        Assert.NotNull(value.SnapshotId);
        Assert.Null(value.ConfirmedPlan);

        var option = value.Options[0];
        var planned = await fixture.PlanAsync(option, "tb270r3-connected-key");
        Assert.True(planned.IsSuccess, Code(planned));

        // The immutable row stores the SAME captured binding that the seam resolved, not the checkout group.
        var row = Assert.Single(fixture.Store.Rows);
        Assert.Equal(value.AreaGroupId, row.AreaGroupId);
        Assert.Equal(value.RateRowId, row.RateRowId);
        Assert.Equal(value.SnapshotId, row.SnapshotId);
        Assert.Equal(value.DraftId, row.DraftId);
        Assert.Equal(value.SourceRevision, row.SourceRevision);
        Assert.Equal(value.FormatVersion, row.FormatVersion);
        Assert.Equal(Group, row.ShippingGroupId);
        Assert.Equal(120, row.PreparationMinutes);
        Assert.Equal(60, row.HandoffMinutes);
        Assert.True(row.PreparationStartUtc <= row.TransferDepartureUtc);
        Assert.True(row.TransferDepartureUtc <= row.HandoffDeadlineUtc);

        // Exact replay of the same reviewed selection answers the stored plan and never appends a second row.
        var replay = await fixture.PlanAsync(option, "tb270r3-connected-key");
        Assert.True(replay.IsSuccess, Code(replay));
        Assert.Single(fixture.Store.Rows);
    }

    [Fact]
    public async Task The_order_side_group_identity_cannot_address_the_captured_read()
    {
        // The SAME accepted evidence, but its captured area group replaced by the Order checkout shipping-group id:
        // that id is a different namespace, so the real seam answers "no such group" and NO calendar is invented.
        var fixture = Fixture.Connected(areaGroupOverride: Group);
        var preview = await fixture.PreviewAsync(fixture.ResendShipmentId);

        Assert.True(preview.IsSuccess, Code(preview));
        Assert.Equal(ShippingAttemptSchedulingReasonCodes.GroupUnavailable, preview.Value!.BlockerCode);
        Assert.Empty(preview.Value.Options);
        Assert.Empty(fixture.Store.Rows);
    }

    // ---------------------------------------------------------------- helpers

    private static AdminOrderOperationRequest Request(
        Guid shipmentId,
        AdminShipmentDispatchPlanOption option,
        string key,
        string? reviewFingerprint = null) =>
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
            DispatchPlanReceivingClosesAt: option.ReceivingClosesAt,
            DispatchPlanReviewFingerprint: reviewFingerprint);

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

    /// <summary>TB-270-R3: the trusted SingleStore edition context the connected seam is resolved under.</summary>
    private sealed class FixedEdition : ICurrentEdition
    {
        public EditionContext? Current => new(ToobaEdition.SingleStore, "tb270r3");
    }

    /// <summary>TB-270-R3: the trusted active tenant of the connected capture.</summary>
    private sealed class FixedTenant(string tenantId) : ICurrentTenant
    {
        public TenantContext? Current => new(
            new TenantId(tenantId),
            TenantStatus.Active,
            new ConnectionReference("tb270r3"),
            null,
            null,
            null,
            "localhost",
            null);
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

        /// <summary>The EXACT request the orchestrator sent to the captured-inputs seam (Repair A evidence).</summary>
        public ShippingAttemptSchedulingLookupRequest? LastLookupRequest => Schedule.Lookup.LastRequest;

        /// <summary>How many times the owning replacement-readiness read was consulted (Repair C evidence).</summary>
        public int ReadinessCalls => Schedule.Readiness.Calls;

        public static Fixture Resend(
            decimal parcelQuantity = 1m,
            decimal usable = 1m,
            decimal damaged = 0m,
            bool receipt = true,
            bool dispatched = false,
            bool allowManage = true,
            bool paid = true,
            IReadOnlyList<string>? missingInputs = null,
            bool capturedRefusal = false,
            bool capturedUsesShippingGroup = false,
            bool readinessIdentityMismatch = false) =>
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
                capturedRefusal,
                capturedUsesShippingGroup,
                readinessIdentityMismatch);

        public static Fixture Replacement(DamagedResolutionStatus status, bool identityMismatch = false) =>
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
                capturedRefusal: false,
                readinessIdentityMismatch: identityMismatch);

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
            bool capturedRefusal,
            bool capturedUsesShippingGroup = false,
            bool readinessIdentityMismatch = false,
            AcceptedShipmentGroupBinding? bindingOverride = null,
            IShippingAttemptSchedulingLookup? activeLookup = null)
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

            var bindings = new BindingFake(bindingOverride ?? new AcceptedShipmentGroupBinding(
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

            var lookup = new LookupFake(
                capturedRefusal,
                BuildInputs(missingInputs, capturedUsesShippingGroup ? Group : null));
            var store = new InMemoryPlanStore();
            var access = new AccessFake(allowManage);
            var fulfillment = new FulfillmentFake(unit);
            var checkouts = new CheckoutReader(paid);
            var readiness = new ReplacementReadinessFake(
                unit,
                replacement is { } readyStatus
                    && readyStatus is DamagedResolutionStatus.Reserved or DamagedResolutionStatus.Fulfilled,
                readinessIdentityMismatch);

            var schedule = new ScheduleFixture(
                checkouts,
                fulfillment,
                bindings,
                lookup,
                store,
                access,
                readiness,
                activeLookup);
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

        public async Task<Result<object>> PlanAsync(AdminShipmentDispatchPlanOption option, string key)
        {
            // The reviewed context comes from the OWNING preview, exactly like the client echoes it back.
            var preview = await PreviewAsync(ResendShipmentId);
            var fingerprint = preview.IsSuccess ? preview.Value!.ReviewFingerprint : null;
            return await Orchestrator.PlanShipmentDispatchAsync(
                Checkout, Actor, Request(ResendShipmentId, option, key, fingerprint), CancellationToken.None);
        }

        /// <summary>
        /// TB-270-R3 Repair A: the accepted evidence of the SAME group, whose captured identity comes from a REAL
        /// capture. The Order-side <c>ShippingGroupId</c> stays the checkout group; the captured area group is a
        /// DIFFERENT identity namespace that only the real seam can resolve.
        /// </summary>
        public static Fixture Connected(Guid? areaGroupOverride = null)
        {
            var capture = BuildConnectedCapture();
            var binding = new AcceptedShipmentGroupBinding(
                Checkout,
                SellerOrder,
                SellerParty,
                Group,
                "store_courier",
                "پیشتاز",
                "store_courier",
                null,
                ShippingGroupOwnerKind.Store,
                Tenant,
                null,
                capture.SnapshotId,
                capture.DraftId,
                capture.SourceRevision,
                capture.FormatVersion,
                9,
                areaGroupOverride ?? capture.AreaGroupId,
                capture.RateRowId,
                new DateOnly(2026, 11, 18),
                new TimeOnly(9, 0).Ticks,
                new TimeOnly(10, 0).Ticks,
                "Asia/Tehran",
                capture.MinimumDeliveryDays,
                capture.MaximumDeliveryDays);

            return Build(
                replacement: null,
                parcelQuantity: 1m,
                usable: 1m,
                damaged: 0m,
                receipt: true,
                dispatched: false,
                allowManage: true,
                paid: true,
                missingInputs: null,
                capturedRefusal: false,
                bindingOverride: binding,
                activeLookup: capture.Lookup);
        }
    }

    private sealed record ScheduleFixture(
        CheckoutReader Checkouts,
        FulfillmentFake Fulfillment,
        BindingFake Bindings,
        LookupFake Lookup,
        InMemoryPlanStore Store,
        AccessFake Access,
        ReplacementReadinessFake Readiness,
        IShippingAttemptSchedulingLookup? ActiveLookup = null)
    {
        /// <summary>
        /// TB-270-R3: `lookupOverride` replaces the controlled fake with the REAL R1 Shipping adapter for the one
        /// connected seam case; every other port stays the same, so only the capture read changes.
        /// </summary>
        public AdminOrderOperationsOrchestrator CreateOrchestrator(
            IClock clock,
            IShippingAttemptSchedulingLookup? lookupOverride = null) =>
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
                lookupOverride ?? ActiveLookup ?? Lookup,
                clock,
                Readiness);
    }

    private static ShippingAttemptSchedulingInputs BuildInputs(
        IReadOnlyList<string>? missingInputs,
        Guid? capturedGroupId = null) =>
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
            capturedGroupId ?? AreaGroup,
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

    /// <summary>
    /// TB-270-R3 Repair A: the seam is addressed by the CAPTURED area group and its rate row, never by the Order
    /// checkout shipping-group id. The fake refuses any other group identity, so a regression fails loudly.
    /// </summary>
    private sealed class LookupFake(bool refuse, ShippingAttemptSchedulingInputs inputs)
        : IShippingAttemptSchedulingLookup
    {
        public ShippingAttemptSchedulingLookupRequest? LastRequest { get; private set; }

        public Task<ShippingAttemptSchedulingResult> LookupAsync(
            ShippingAttemptSchedulingLookupRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.GroupId != AreaGroup || request.RateRowId != RateRow)
            {
                return Task.FromResult(ShippingAttemptSchedulingResult.Refused(
                    ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable,
                    ShippingAttemptSchedulingReasonCodes.CapturedRevisionUnavailable));
            }

            return Task.FromResult(
                refuse
                    ? ShippingAttemptSchedulingResult.Refused(
                        ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable,
                        ShippingAttemptSchedulingReasonCodes.CapturedRevisionUnavailable)
                    : new ShippingAttemptSchedulingResult(
                        ShippingAttemptSchedulingVerdict.CapturedInputs,
                        ShippingAttemptSchedulingReasonCodes.CapturedInputs,
                        inputs));
        }
    }

    /// <summary>
    /// TB-270-R3 Repair C: the owning readiness fact. The test controls readiness; the orchestrator still re-checks
    /// the returned identity against the parcel's recorded chain, so a mismatched fact is refused.
    /// </summary>
    private sealed class ReplacementReadinessFake(FulfillmentSnapshot unit, bool ready, bool identityMismatch = false)
        : IFulfillmentReplacementReadinessReader
    {
        public int Calls { get; private set; }

        public Task<FulfillmentReplacementReadinessFact> GetAsync(
            Guid fulfillmentId,
            Guid shipmentId,
            CancellationToken cancellationToken)
        {
            Calls++;
            var parcel = unit.Shipments.Single(x => x.ShipmentId == shipmentId);
            if (!ready)
            {
                return Task.FromResult(new FulfillmentReplacementReadinessFact(
                    fulfillmentId,
                    shipmentId,
                    false,
                    FulfillmentReplacementReadinessReasonCodes.DecisionNotReady));
            }

            // A fact about a DIFFERENT parcel must never authorize this one.
            var reportedShipment = identityMismatch ? SourceShipment : shipmentId;
            return Task.FromResult(new FulfillmentReplacementReadinessFact(
                fulfillmentId,
                reportedShipment,
                true,
                FulfillmentReplacementReadinessReasonCodes.Ready,
                parcel.ResendOfShipmentId,
                parcel.DamagedResolutionId,
                parcel.ReplacementReservationId,
                Line,
                parcel.Items.Sum(x => x.Quantity),
                ReceivedAt));
        }
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
