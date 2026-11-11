using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Adapters;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;
using Tooba.Persistence;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;
using Xunit;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-258-R2 (Gap A): a post-dispatch exception whose response was lost is reconciled through the PUBLIC
/// admin command against durable facts, even after the parcel moved on and the fresh projection stopped
/// offering that action.
///
/// The tests pin the four invariants of that seam:
/// 1. an EXACT recorded intent is answered from the owning facts and writes nothing;
/// 2. a changed intent is a typed conflict, never a second allocation;
/// 3. actor authority and checkout/fulfillment/parcel ownership are re-checked on EVERY request, so a
///    retained browser envelope can never authorize anything;
/// 4. an ABSENT record still goes through the ordinary projection gate, so a missing record can never
///    become a new allocation either.
/// </summary>
public sealed class AdminOrderPostDispatchReplayTests
{
    private static readonly Guid CheckoutId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActorId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid SellerOrderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SellerPartyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid FulfillmentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid OtherFulfillmentId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ShipmentId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid LineId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Exact_recorded_non_delivery_answers_from_durable_facts_without_the_projection_gate()
    {
        // The parcel is already Delivered, so the fresh projection offers NO non-delivery exit any more.
        var snapshot = FulfillmentSnapshotWith(ShipmentOperationStatus.Delivered, delivered: true);
        var fulfillment = new FulfillmentFake(
            snapshot,
            new PostDispatchIntentResult(PostDispatchIntentState.Recorded, snapshot));
        var ports = new Ports("order.handle");
        var orchestrator = Build(fulfillment, ports);

        var result = await orchestrator.RecordNonDeliveryAsync(
            CheckoutId,
            ActorId,
            NonDeliveryRequest("customer_unavailable", "مشتری در دسترس نبود"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(snapshot, result.Value);
        // Nothing is written, nothing is re-attributed and the fresh projection is never consulted.
        Assert.Equal(0, fulfillment.MutationCalls);
        Assert.Equal(0, ports.Returns.ProjectionCalls);
        // The recorded intent was asked for, and it is the EXACT reviewed one.
        var intent = Assert.Single(fulfillment.ReconcileCalls);
        Assert.Equal(PostDispatchIntentKind.RecordNonDelivery, intent.Kind);
        Assert.Equal(FulfillmentId, intent.FulfillmentId);
        Assert.Equal(ShipmentId, intent.ShipmentId);
        Assert.Equal("customer_unavailable", intent.Reason);
        Assert.Equal("مشتری در دسترس نبود", intent.Note);
    }

    [Fact]
    public async Task Exact_recorded_receipt_replays_the_same_mixed_condition_lines()
    {
        var snapshot = FulfillmentSnapshotWith(ShipmentOperationStatus.Returned, delivered: true, returned: true);
        var fulfillment = new FulfillmentFake(
            snapshot,
            new PostDispatchIntentResult(PostDispatchIntentState.Recorded, snapshot));
        var ports = new Ports("fulfillment.manage");
        var orchestrator = Build(fulfillment, ports);

        var result = await orchestrator.RecordReturnReceiptAsync(
            CheckoutId,
            ActorId,
            ReceiptRequest(
                new AdminReturnReceiptLineSelection(LineId, 1m, 0),
                new AdminReturnReceiptLineSelection(LineId, 1m, 1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, fulfillment.MutationCalls);
        // ONE line split across both physical conditions is carried as the canonical two-line receipt.
        var items = Assert.Single(fulfillment.ReconcileCalls).ReceiptItems!;
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.Condition == ReturnReceiptCondition.Usable && x.Quantity == 1m);
        Assert.Contains(items, x => x.Condition == ReturnReceiptCondition.Damaged && x.Quantity == 1m);
    }

    [Fact]
    public async Task A_changed_recorded_intent_is_a_typed_conflict_and_never_a_second_allocation()
    {
        var snapshot = FulfillmentSnapshotWith(ShipmentOperationStatus.Returned, delivered: true, returned: true);
        var fulfillment = new FulfillmentFake(
            snapshot,
            new PostDispatchIntentResult(
                PostDispatchIntentState.Conflict,
                ConflictCode: "fulfillment.return.receipt_conflict"));
        var ports = new Ports("order.handle");
        var orchestrator = Build(fulfillment, ports);

        var result = await orchestrator.RecordReturnReceiptAsync(
            CheckoutId,
            ActorId,
            ReceiptRequest(new AdminReturnReceiptLineSelection(LineId, 2m, 0)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("fulfillment.return.receipt_conflict", result.FirstError.Code);
        Assert.Equal(0, fulfillment.MutationCalls);
        Assert.Equal(0, ports.Returns.ProjectionCalls);
    }

    [Fact]
    public async Task An_actor_without_fulfillment_authority_cannot_replay_a_recorded_intent()
    {
        var snapshot = FulfillmentSnapshotWith(ShipmentOperationStatus.Dispatched, delivered: false);
        var fulfillment = new FulfillmentFake(
            snapshot,
            new PostDispatchIntentResult(PostDispatchIntentState.Recorded, snapshot));
        // An ops-family grant that is NOT order.handle/fulfillment.manage: the actor is genuinely denied.
        var ports = new Ports("order.cancel");
        var orchestrator = Build(fulfillment, ports);

        var result = await orchestrator.RecordNonDeliveryAsync(
            CheckoutId,
            ActorId,
            NonDeliveryRequest("other", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("order.operation.denied", result.FirstError.Code);
        // Authorization is decided BEFORE any evidence read.
        Assert.Empty(fulfillment.ReconcileCalls);
        Assert.Equal(0, fulfillment.MutationCalls);
    }

    [Fact]
    public async Task A_parcel_of_another_fulfillment_is_refused_even_when_a_record_exists()
    {
        var snapshot = FulfillmentSnapshotWith(ShipmentOperationStatus.Dispatched, delivered: false);
        var fulfillment = new FulfillmentFake(
            snapshot,
            new PostDispatchIntentResult(PostDispatchIntentState.Recorded, snapshot));
        var ports = new Ports("order.handle");
        var orchestrator = Build(fulfillment, ports);

        // The fulfillment exists for this checkout, but the parcel belongs to a different fulfillment unit.
        var result = await orchestrator.StartReturnAsync(
            CheckoutId,
            ActorId,
            new AdminOrderOperationRequest(
                "start_return",
                SellerOrderId,
                OtherFulfillmentId,
                ShipmentId,
                ReturnRequestId: null,
                CarrierDisplayName: null,
                TrackingReference: null,
                Reason: null,
                IdempotencyKey: null,
                ReturnItems: null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("order.operation.invalid", result.FirstError.Code);
        Assert.Empty(fulfillment.ReconcileCalls);
        Assert.Equal(0, fulfillment.MutationCalls);
    }

    [Fact]
    public async Task An_absent_record_still_goes_through_the_projection_gate_and_allocates_nothing()
    {
        // The parcel is Delivered, so the projection no longer offers non-delivery and nothing was recorded.
        var fulfillment = new FulfillmentFake(
            FulfillmentSnapshotWith(ShipmentOperationStatus.Delivered, delivered: true),
            new PostDispatchIntentResult(PostDispatchIntentState.NotRecorded));
        var ports = new Ports("order.handle");
        var orchestrator = Build(fulfillment, ports);

        var result = await orchestrator.RecordNonDeliveryAsync(
            CheckoutId,
            ActorId,
            NonDeliveryRequest("refused", "بررسی دوباره"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("order.operation.invalid", result.FirstError.Code);
        // The ordinary projection gate ran…
        Assert.True(ports.Returns.ProjectionCalls > 0);
        // …and the owning mutation was never reached: an absent record never becomes an allocation.
        Assert.Equal(0, fulfillment.MutationCalls);
        Assert.Single(fulfillment.ReconcileCalls);
    }

    [Fact]
    public async Task An_absent_record_for_a_still_actionable_parcel_runs_the_ordinary_command_once()
    {
        var fulfillment = new FulfillmentFake(
            FulfillmentSnapshotWith(ShipmentOperationStatus.Dispatched, delivered: false),
            new PostDispatchIntentResult(PostDispatchIntentState.NotRecorded));
        var ports = new Ports("order.handle");
        var orchestrator = Build(fulfillment, ports);

        var result = await orchestrator.RecordNonDeliveryAsync(
            CheckoutId,
            ActorId,
            NonDeliveryRequest("address_not_found", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(fulfillment.ReconcileCalls);
        Assert.Equal(1, fulfillment.MutationCalls);
        Assert.Equal(1, fulfillment.NonDeliveryCalls);
    }

    // ---------------------------------------------------------------------------------------------------------
    // TB-258-R2 integrated proof: the PUBLIC command against REAL owning storage.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The formerly blocking seam, end to end: the public admin command → the real Fulfillment admin adapter →
    /// the real <c>FulfillmentDirectory</c> → real rows in the ONE canonical database.
    ///
    /// <para>
    /// A lost response is replayed for a resend whose source capacity is already consumed and for a receipt whose
    /// parcel is already <c>Returned</c> — in both cases the fresh projected page no longer offers the action, so the
    /// old <c>RunProjectedCoreAsync</c> gate would have refused the retry. The exact recorded intent is answered from
    /// the owning facts with zero new rows, a changed one is a typed conflict, and a foreign checkout is refused.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Relational_public_command_replays_a_lost_resend_and_receipt_from_real_owning_storage()
    {
        var connection = Environment.GetEnvironmentVariable(RelationalEnvVar);
        if (string.IsNullOrWhiteSpace(connection))
        {
            return;
        }

        var target = new NpgsqlConnectionStringBuilder(connection);
        var fixture = await CreateDispatchedFixtureAsync(target);
        var ports = new Ports("order.handle");
        var orders = new CheckoutReaderFake(
            fixture.CheckoutId, fixture.SellerOrderId, fixture.SellerPartyId, fixture.LineAId);

        // ---- the ordinary lifecycle is recorded through the PUBLIC command, once -------------------------------
        Guid resendShipmentId;
        Guid receiptIdOnFirstCommit;
        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewAdapter(db), ports, orders);

            var nonDelivered = await orchestrator.RecordNonDeliveryAsync(
                fixture.CheckoutId,
                ActorId,
                NonDeliveryRequestFor(fixture, "customer_unavailable", "TB258R2 relational"),
                CancellationToken.None);
            Assert.True(nonDelivered.IsSuccess, nonDelivered.IsSuccess ? string.Empty : nonDelivered.FirstError?.Code);

            var returned = await orchestrator.StartReturnAsync(
                fixture.CheckoutId, ActorId, ReturnStartRequest(fixture), CancellationToken.None);
            Assert.True(returned.IsSuccess, returned.IsSuccess ? string.Empty : returned.FirstError?.Code);

            var receipt = await orchestrator.RecordReturnReceiptAsync(
                fixture.CheckoutId,
                ActorId,
                ReceiptRequestFor(fixture,
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 0),
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 1),
                    new AdminReturnReceiptLineSelection(fixture.LineBId, 1m, 0)),
                CancellationToken.None);
            Assert.True(receipt.IsSuccess, receipt.IsSuccess ? string.Empty : receipt.FirstError?.Code);
            receiptIdOnFirstCommit = await RawScalarGuidAsync(
                target,
                "select shipment_return_receipt_id from fulfillment.shipment_return_receipts where shipment_id = @id",
                ("id", fixture.ShipmentId))
                ?? throw new InvalidOperationException("the receipt was not persisted");

            // The fresh page offers exactly one resend for this returned parcel.
            var before = await orchestrator.ListAsync(fixture.CheckoutId, ActorId, CancellationToken.None);
            Assert.Contains(before.Value.Actions, a =>
                a.Code == "resend_shipment" && a.ShipmentId == fixture.ShipmentId);

            var resend = await orchestrator.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 1m, fixture.ResendKey),
                CancellationToken.None);
            Assert.True(resend.IsSuccess, resend.IsSuccess ? string.Empty : resend.FirstError?.Code);
            resendShipmentId = await RawScalarGuidAsync(
                target,
                "select shipment_id from fulfillment.shipments where resend_of_shipment_id = @id",
                ("id", fixture.ShipmentId))
                ?? throw new InvalidOperationException("the resend parcel was not persisted");

            // Capacity is consumed: the reloaded REAL projection no longer has a resendable unit on that line.
            var reloaded = await NewAdapter(db).GetAsync(fixture.FulfillmentId, CancellationToken.None);
            Assert.Equal(0m, reloaded!.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);
            Assert.Equal(
                resendShipmentId,
                reloaded.Shipments.Single(x => x.ShipmentId == resendShipmentId).ShipmentId);
        }

        var shipmentsAfterFirst = await CountShipmentsAsync(target, fixture.FulfillmentId);
        var receiptsAfterFirst = await CountReceiptsAsync(target, fixture.ShipmentId);
        Assert.Equal(2, shipmentsAfterFirst);
        Assert.Equal(1, receiptsAfterFirst);

        // ---- response lost: a NEW context/adapter sees the SAME durable state and replays verbatim -----------------
        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewAdapter(db), ports, orders);

            // The receipt is already committed and the parcel is Returned, so the projected action is gone.
            var replayedReceipt = await orchestrator.RecordReturnReceiptAsync(
                fixture.CheckoutId,
                ActorId,
                ReceiptRequestFor(fixture,
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 0),
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 1),
                    new AdminReturnReceiptLineSelection(fixture.LineBId, 1m, 0)),
                CancellationToken.None);
            Assert.True(replayedReceipt.IsSuccess);
            var replayedSnapshot = Assert.IsType<FulfillmentSnapshot>(replayedReceipt.Value);
            var replayReceipt = replayedSnapshot.Shipments
                .Single(x => x.ShipmentId == fixture.ShipmentId).ReturnReceipt!;
            Assert.Equal(receiptIdOnFirstCommit, replayReceipt.ReceivedReceiptId);
            Assert.Equal(2m, replayReceipt.UsableQuantity);
            Assert.Equal(receiptsAfterFirst, await CountReceiptsAsync(target, fixture.ShipmentId));

            // The resend occurrence is already committed: the same key/source/lines answer the same parcel.
            var replayedResend = await orchestrator.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 1m, fixture.ResendKey),
                CancellationToken.None);
            Assert.True(replayedResend.IsSuccess);
            var replayedResendSnapshot = Assert.IsType<FulfillmentSnapshot>(replayedResend.Value);
            Assert.Contains(replayedResendSnapshot.Shipments, x => x.ShipmentId == resendShipmentId);
            Assert.Equal(shipmentsAfterFirst, await CountShipmentsAsync(target, fixture.FulfillmentId));

            // The non-delivery and the return start are replayed from their own recorded facts, unchanged.
            var replayedNonDelivery = await orchestrator.RecordNonDeliveryAsync(
                fixture.CheckoutId,
                ActorId,
                NonDeliveryRequestFor(fixture, "customer_unavailable", "TB258R2 relational"),
                CancellationToken.None);
            Assert.True(replayedNonDelivery.IsSuccess);
            var replayedReturn = await orchestrator.StartReturnAsync(
                fixture.CheckoutId, ActorId, ReturnStartRequest(fixture), CancellationToken.None);
            Assert.True(replayedReturn.IsSuccess);

            // A CHANGED intent is a typed conflict with zero writes.
            var conflictedResend = await orchestrator.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 2m, fixture.ResendKey),
                CancellationToken.None);
            Assert.False(conflictedResend.IsSuccess);
            Assert.Equal("fulfillment.resend.intent_conflict", conflictedResend.FirstError.Code);

            var conflictedReceipt = await orchestrator.RecordReturnReceiptAsync(
                fixture.CheckoutId,
                ActorId,
                ReceiptRequestFor(fixture, new AdminReturnReceiptLineSelection(fixture.LineAId, 2m, 0)),
                CancellationToken.None);
            Assert.False(conflictedReceipt.IsSuccess);
            Assert.Equal("fulfillment.return.receipt_conflict", conflictedReceipt.FirstError.Code);

            // A FOREIGN checkout cannot borrow this parcel, even with a recorded intent.
            var foreign = await orchestrator.ResendShipmentAsync(
                fixture.ForeignCheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 1m, fixture.ResendKey),
                CancellationToken.None);
            Assert.False(foreign.IsSuccess);
            Assert.Equal("order.operation.invalid", foreign.FirstError.Code);
        }

        // An actor without the post-dispatch authority is denied before any evidence read.
        await using (var db = NewFulfillmentDb(target))
        {
            var denied = Build(NewAdapter(db), new Ports("order.cancel"), orders);
            var result = await denied.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 1m, fixture.ResendKey),
                CancellationToken.None);
            Assert.False(result.IsSuccess);
            Assert.Equal("order.operation.denied", result.FirstError.Code);
        }

        // Every replay/refusal above wrote nothing: the owning rows are exactly the first-request rows.
        Assert.Equal(shipmentsAfterFirst, await CountShipmentsAsync(target, fixture.FulfillmentId));
        Assert.Equal(receiptsAfterFirst, await CountReceiptsAsync(target, fixture.ShipmentId));

        RecordEvidence(
            $"G orchestration fulfillment={fixture.FulfillmentId} checkout={fixture.CheckoutId} "
            + $"source={fixture.ShipmentId} resend={resendShipmentId} receipt={receiptIdOnFirstCommit} "
            + "lifecycle=public-command replay=exact(sameParcel) changed=conflict foreign=refused "
            + $"rows=shipments{shipmentsAfterFirst}/receipts{receiptsAfterFirst}");
    }

    /// <summary>
    /// TB-262 on real owning storage through the PUBLIC command: the damaged decision of a physically returned parcel
    /// line is durable, its replacement is funded by its OWN new reservation, and an exact retry is answered even
    /// after the fresh projection no longer offers the action. A changed quantity/outcome is a typed conflict with
    /// zero writes, a foreign checkout is refused and an actor without post-dispatch authority is denied.
    /// </summary>
    [Fact]
    public async Task Relational_damaged_decision_replays_through_the_public_command_after_the_action_disappears()
    {
        var connection = Environment.GetEnvironmentVariable(RelationalEnvVar);
        if (string.IsNullOrWhiteSpace(connection))
        {
            return;
        }

        var target = new NpgsqlConnectionStringBuilder(connection);
        var inventory = new ReplacementInventoryGateway();
        var fixture = await CreateDispatchedFixtureAsync(
            target,
            inventory,
            lineAReservationId: Guid.NewGuid(),
            lineBReservationId: Guid.NewGuid());
        var ports = new Ports("order.handle");
        var orders = new CheckoutReaderFake(
            fixture.CheckoutId, fixture.SellerOrderId, fixture.SellerPartyId, fixture.LineAId);
        var keyA = $"tb262-damaged-a-{fixture.ShipmentId:N}";
        var keyB = $"tb262-damaged-b-{fixture.ShipmentId:N}";

        Guid replacementShipmentId;
        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewDamagedAdapter(db, inventory), ports, orders);

            Assert.True((await orchestrator.RecordNonDeliveryAsync(
                fixture.CheckoutId,
                ActorId,
                NonDeliveryRequestFor(fixture, "customer_unavailable", "TB262 relational"),
                CancellationToken.None)).IsSuccess);
            Assert.True((await orchestrator.StartReturnAsync(
                fixture.CheckoutId, ActorId, ReturnStartRequest(fixture), CancellationToken.None)).IsSuccess);
            // Line A comes back as 1 healthy + 1 damaged; line B as 1 damaged. Both parcels of physical truth belong
            // to the SAME returned source parcel.
            Assert.True((await orchestrator.RecordReturnReceiptAsync(
                fixture.CheckoutId,
                ActorId,
                ReceiptRequestFor(fixture,
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 0),
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 1),
                    new AdminReturnReceiptLineSelection(fixture.LineBId, 1m, 1)),
                CancellationToken.None)).IsSuccess);

            // The fresh projection offers the damaged decision for exactly this returned parcel.
            var before = await orchestrator.ListAsync(fixture.CheckoutId, ActorId, CancellationToken.None);
            Assert.Contains(before.Value.Actions, a =>
                a.Code == "resolve_damaged_goods" && a.ShipmentId == fixture.ShipmentId);

            var resolved = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", keyA),
                CancellationToken.None);
            Assert.True(resolved.IsSuccess, resolved.IsSuccess ? string.Empty : resolved.FirstError?.Code);
            var snapshot = Assert.IsType<FulfillmentSnapshot>(resolved.Value);
            var replacement = snapshot.Shipments.Single(x => x.IsReplacement);
            replacementShipmentId = replacement.ShipmentId;
            Assert.Equal(inventory.ReservationId, replacement.ReplacementReservationId);
            Assert.Equal(fixture.ShipmentId, replacement.ResendOfShipmentId);
            Assert.Equal(ShipmentOperationStatus.Created, replacement.Status);
            Assert.Equal(1m, replacement.Items.Single(x => x.OrderLineId == fixture.LineAId).Quantity);
            Assert.Equal(1, inventory.ReserveCalls);
            // TB-262-R1: the replacement hold is a NON-EXPIRING paid-order reservation from its FIRST durable commit
            // inside Inventory, so there is no separate "clear the TTL" step that could be lost in a crash.
            Assert.Equal(0, inventory.CommitCalls);
            // The damaged unit is committed; the healthy returned capacity of the same line is untouched.
            Assert.Equal(0m, snapshot.Items.Single(x => x.OrderLineId == fixture.LineAId).OutstandingDamagedQuantity);
            Assert.Equal(1m, snapshot.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);

            // A referral binds the unit without allocating anything and without pretending it was resolved.
            var referral = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineBId, 1m, "referral", keyB),
                CancellationToken.None);
            Assert.True(referral.IsSuccess, referral.IsSuccess ? string.Empty : referral.FirstError?.Code);
            var referralSnapshot = Assert.IsType<FulfillmentSnapshot>(referral.Value);
            var referralDecision = referralSnapshot.Shipments
                .Single(x => x.ShipmentId == fixture.ShipmentId).DamagedResolutions!
                .Single(x => x.OrderLineId == fixture.LineBId);
            Assert.Equal(DamagedResolutionOutcome.Referral, referralDecision.Outcome);
            Assert.Equal(DamagedResolutionStatus.Recorded, referralDecision.Status);
            Assert.Null(referralDecision.ReplacementReservationId);
            Assert.Null(referralDecision.ReplacementShipmentId);
            Assert.Equal(1, inventory.ReserveCalls);

            // Both damaged units are committed, so the fresh projection no longer offers the action at all.
            var after = await orchestrator.ListAsync(fixture.CheckoutId, ActorId, CancellationToken.None);
            Assert.DoesNotContain(after.Value.Actions, a => a.Code == "resolve_damaged_goods");
        }

        var shipmentsBefore = await CountShipmentsAsync(target, fixture.FulfillmentId);
        var decisionsBefore = await CountDamagedResolutionsAsync(target, fixture.FulfillmentId);
        Assert.Equal(2, shipmentsBefore);
        Assert.Equal(2, decisionsBefore);

        // ---- the response was lost: a NEW context replays the EXACT reviewed decision with zero writes ----------
        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewDamagedAdapter(db, inventory), ports, orders);

            var replayed = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", keyA),
                CancellationToken.None);
            Assert.True(replayed.IsSuccess, replayed.IsSuccess ? string.Empty : replayed.FirstError?.Code);
            var replayedSnapshot = Assert.IsType<FulfillmentSnapshot>(replayed.Value);
            Assert.Contains(replayedSnapshot.Shipments, x => x.ShipmentId == replacementShipmentId);
            // No second reservation and no second parcel: the recorded decision is answered, never re-executed.
            Assert.Equal(1, inventory.ReserveCalls);
            Assert.Equal(shipmentsBefore, await CountShipmentsAsync(target, fixture.FulfillmentId));

            // The same occurrence with a DIFFERENT quantity, and with a DIFFERENT outcome, are typed conflicts.
            var changedQuantity = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 2m, "replacement", keyA),
                CancellationToken.None);
            Assert.False(changedQuantity.IsSuccess);
            Assert.Equal("fulfillment.damage.resolution_intent_conflict", changedQuantity.FirstError.Code);

            var changedOutcome = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "referral", keyA),
                CancellationToken.None);
            Assert.False(changedOutcome.IsSuccess);
            Assert.Equal("fulfillment.damage.resolution_intent_conflict", changedOutcome.FirstError.Code);

            // A foreign checkout cannot borrow this returned parcel even with the recorded occurrence.
            var foreign = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.ForeignCheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", keyA),
                CancellationToken.None);
            Assert.False(foreign.IsSuccess);
            Assert.Equal("order.operation.invalid", foreign.FirstError.Code);
        }

        // An actor without the post-dispatch authority is denied before any evidence read.
        await using (var db = NewFulfillmentDb(target))
        {
            var denied = Build(NewDamagedAdapter(db, inventory), new Ports("order.cancel"), orders);
            var result = await denied.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", keyA),
                CancellationToken.None);
            Assert.False(result.IsSuccess);
            Assert.Equal("order.operation.denied", result.FirstError.Code);
        }

        // Nothing above wrote: exactly the two first-request decisions and the one replacement parcel exist.
        Assert.Equal(shipmentsBefore, await CountShipmentsAsync(target, fixture.FulfillmentId));
        Assert.Equal(decisionsBefore, await CountDamagedResolutionsAsync(target, fixture.FulfillmentId));
        Assert.Equal(1, inventory.ReserveCalls);
        Assert.Equal("Created", await RawShipmentStatusAsync(target, replacementShipmentId));

        RecordEvidence(
            $"H damaged fulfillment={fixture.FulfillmentId} checkout={fixture.CheckoutId} "
            + $"source={fixture.ShipmentId} replacement={replacementShipmentId} keyA={keyA} keyB={keyB} "
            + "replacementReservation=own referral=noAllocation replay=exact(zeroWrites) "
            + "changedQuantity=conflict changedOutcome=conflict foreign=refused denied=denied "
            + $"rows=shipments{shipmentsBefore}/decisions{decisionsBefore}",
            "tb262-orchestrator-relational.txt");
    }

    /// <summary>
    /// TB-262-R1 on real storage through the PUBLIC command: an INCOMPLETE damaged replacement (decision recorded,
    /// healthy stock unavailable, so no hold and no parcel) is a pending conversation, not a completed one. A retry
    /// must continue the SAME recorded decision instead of answering it as a snapshot, a pre-dispatch parcel
    /// cancellation must keep the order-bound hold (so a starved gateway still resumes with ZERO new reservation),
    /// and replaying the OLD cancelled parcel's cancellation must never detach the NEW live replacement attempt.
    /// </summary>
    [Fact]
    public async Task Relational_pending_damaged_replacement_resumes_after_a_cancelled_attempt_with_the_retained_hold()
    {
        var connection = Environment.GetEnvironmentVariable(RelationalEnvVar);
        if (string.IsNullOrWhiteSpace(connection))
        {
            return;
        }

        var target = new NpgsqlConnectionStringBuilder(connection);
        var fixture = await CreateDispatchedFixtureAsync(
            target,
            new ReplacementInventoryGateway(),
            lineAReservationId: Guid.NewGuid(),
            lineBReservationId: Guid.NewGuid());
        var ports = new Ports("order.handle");
        var orders = new CheckoutReaderFake(
            fixture.CheckoutId, fixture.SellerOrderId, fixture.SellerPartyId, fixture.LineAId);
        var key = $"TB262R1-resume-{Guid.NewGuid():N}";

        // ---- the origin receipt is recorded ONCE: line A returns 1 healthy + 1 damaged ---------------------------
        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewAdapter(db), ports, orders);

            Assert.True((await orchestrator.RecordNonDeliveryAsync(
                fixture.CheckoutId,
                ActorId,
                NonDeliveryRequestFor(fixture, "customer_unavailable", "TB262R1 resume"),
                CancellationToken.None)).IsSuccess);
            Assert.True((await orchestrator.StartReturnAsync(
                fixture.CheckoutId, ActorId, ReturnStartRequest(fixture), CancellationToken.None)).IsSuccess);
            Assert.True((await orchestrator.RecordReturnReceiptAsync(
                fixture.CheckoutId,
                ActorId,
                ReceiptRequestFor(fixture,
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 0),
                    new AdminReturnReceiptLineSelection(fixture.LineAId, 1m, 1),
                    new AdminReturnReceiptLineSelection(fixture.LineBId, 1m, 1)),
                CancellationToken.None)).IsSuccess);
        }

        // ---- A: healthy stock is gone, so the FIRST public attempt is an honest blocker with NO parcel ----------
        await using (var db = NewFulfillmentDb(target))
        {
            var starved = new StarvedReplacementInventoryGateway();
            var orchestrator = Build(NewDamagedAdapter(db, starved), ports, orders);

            var pending = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", key),
                CancellationToken.None);
            Assert.False(pending.IsSuccess);
            Assert.Equal("fulfillment.damage.replacement_unavailable", pending.FirstError.Code);
            Assert.Equal(1, starved.ReserveAttempts);
        }

        // The decision survived its own failure and no parcel was invented for it.
        Assert.Equal(1, await CountDamagedResolutionsAsync(target, fixture.FulfillmentId));
        Assert.Equal(1, await CountShipmentsAsync(target, fixture.FulfillmentId));

        // ---- A: a retry in a NEW context CONTINUES the pending decision instead of answering a snapshot --------
        await using (var db = NewFulfillmentDb(target))
        {
            var starved = new StarvedReplacementInventoryGateway();
            var orchestrator = Build(NewDamagedAdapter(db, starved), ports, orders);

            var stillPending = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", key),
                CancellationToken.None);
            Assert.False(stillPending.IsSuccess);
            Assert.Equal("fulfillment.damage.replacement_unavailable", stillPending.FirstError.Code);
            Assert.Equal(1, starved.ReserveAttempts);
        }

        Assert.Equal(1, await CountDamagedResolutionsAsync(target, fixture.FulfillmentId));
        Assert.Equal(1, await CountShipmentsAsync(target, fixture.FulfillmentId));

        // ---- the SAME recorded intent now finds healthy stock and completes with its own hold + parcel ----------
        Guid firstParcel;
        Guid retainedReservation;
        await using (var db = NewFulfillmentDb(target))
        {
            var inventory = new ReplacementInventoryGateway();
            var orchestrator = Build(NewDamagedAdapter(db, inventory), ports, orders);

            var resolved = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", key),
                CancellationToken.None);
            Assert.True(resolved.IsSuccess, resolved.IsSuccess ? string.Empty : resolved.FirstError?.Code);
            var snapshot = Assert.IsType<FulfillmentSnapshot>(resolved.Value);
            var decision = snapshot.Shipments
                .Single(x => x.ShipmentId == fixture.ShipmentId).DamagedResolutions!
                .Single(x => x.OrderLineId == fixture.LineAId);
            Assert.Equal(DamagedResolutionStatus.Fulfilled, decision.Status);
            Assert.Equal(inventory.ReservationId, decision.ReplacementReservationId);
            firstParcel = decision.ReplacementShipmentId!.Value;
            retainedReservation = decision.ReplacementReservationId!.Value;
            Assert.Equal(1, inventory.ReserveCalls);
            Assert.Equal(0, inventory.CommitCalls);
            Assert.Equal("Created", await RawShipmentStatusAsync(target, firstParcel));
        }

        // ---- cancelling the CREATED replacement parcel releases ONLY the parcel allocation -----------------------
        await using (var db = NewFulfillmentDb(target))
        {
            var inventory = new ReplacementInventoryGateway();
            var adapter = NewDamagedAdapter(db, inventory);

            await adapter.CancelShipmentAsync(
                fixture.FulfillmentId, firstParcel, ActorId, CancellationToken.None);
            // The ORDER is still active: a parcel cancellation is not an order cancellation, so NOTHING is released.
            Assert.Equal(0, inventory.ReleaseCalls);
        }

        Assert.Equal("Cancelled", await RawShipmentStatusAsync(target, firstParcel));
        Assert.Equal(1, await CountDamagedResolutionsAsync(target, fixture.FulfillmentId));

        // ---- the retry RESUMES on the RETAINED hold: a starved gateway proves no second reservation is made -----
        Guid secondParcel;
        await using (var db = NewFulfillmentDb(target))
        {
            var starved = new StarvedReplacementInventoryGateway();
            var orchestrator = Build(NewDamagedAdapter(db, starved), ports, orders);

            var resumed = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", key),
                CancellationToken.None);
            Assert.True(resumed.IsSuccess, resumed.IsSuccess ? string.Empty : resumed.FirstError?.Code);
            var snapshot = Assert.IsType<FulfillmentSnapshot>(resumed.Value);
            var decision = snapshot.Shipments
                .Single(x => x.ShipmentId == fixture.ShipmentId).DamagedResolutions!
                .Single(x => x.OrderLineId == fixture.LineAId);
            Assert.Equal(DamagedResolutionOutcome.Replacement, decision.Outcome);
            Assert.Equal(DamagedResolutionStatus.Fulfilled, decision.Status);
            // The SAME purpose-bound hold, a genuinely NEW parcel — never a second reservation for one allocation.
            Assert.Equal(retainedReservation, decision.ReplacementReservationId);
            Assert.NotNull(decision.ReplacementShipmentId);
            secondParcel = decision.ReplacementShipmentId!.Value;
            Assert.NotEqual(firstParcel, secondParcel);
            Assert.Equal(0, starved.ReserveAttempts);
            Assert.Equal(0, starved.ReleaseCalls);
            Assert.Equal(
                retainedReservation,
                snapshot.Shipments.Single(x => x.ShipmentId == secondParcel).ReplacementReservationId);
            Assert.Equal("Created", await RawShipmentStatusAsync(target, secondParcel));
        }

        // One decision, one retained allocation, and the source parcel plus TWO replacement attempts (one cancelled).
        Assert.Equal(1, await CountDamagedResolutionsAsync(target, fixture.FulfillmentId));
        Assert.Equal(3, await CountShipmentsAsync(target, fixture.FulfillmentId));

        // ---- replaying the OLD parcel's cancellation is a NO-OP for the NEW live attempt ------------------------
        await using (var db = NewFulfillmentDb(target))
        {
            var inventory = new ReplacementInventoryGateway();
            var adapter = NewDamagedAdapter(db, inventory);

            // The owning seam accepts the idempotent replay (the parcel is already Cancelled); the TB-262-R1
            // association guard is what keeps it from detaching the CURRENT live replacement attempt.
            await adapter.CancelShipmentAsync(
                fixture.FulfillmentId, firstParcel, ActorId, CancellationToken.None);
            Assert.Equal("Cancelled", await RawShipmentStatusAsync(target, firstParcel));
            // The stale cancellation never detached the current attempt and never advanced it.
            Assert.Equal("Created", await RawShipmentStatusAsync(target, secondParcel));
            Assert.Equal(0, inventory.ReleaseCalls);
        }

        // The public exact-intent replay still answers the SAME live replacement for the SAME decision.
        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewAdapter(db), ports, orders);
            var replayed = await orchestrator.ResolveDamagedGoodsAsync(
                fixture.CheckoutId,
                ActorId,
                DamagedRequest(fixture, fixture.LineAId, 1m, "replacement", key),
                CancellationToken.None);
            Assert.True(replayed.IsSuccess, replayed.IsSuccess ? string.Empty : replayed.FirstError?.Code);
            var snapshot = Assert.IsType<FulfillmentSnapshot>(replayed.Value);
            var decision = snapshot.Shipments
                .Single(x => x.ShipmentId == fixture.ShipmentId).DamagedResolutions!
                .Single(x => x.OrderLineId == fixture.LineAId);
            Assert.Equal(DamagedResolutionStatus.Fulfilled, decision.Status);
            Assert.Equal(secondParcel, decision.ReplacementShipmentId);
            Assert.Equal(retainedReservation, decision.ReplacementReservationId);
        }

        Assert.Equal(1, await CountDamagedResolutionsAsync(target, fixture.FulfillmentId));
        Assert.Equal(3, await CountShipmentsAsync(target, fixture.FulfillmentId));
        Assert.Equal("Created", await RawShipmentStatusAsync(target, secondParcel));

        RecordEvidence(
            $"I damagedPending fulfillment={fixture.FulfillmentId} checkout={fixture.CheckoutId} "
            + $"source={fixture.ShipmentId} key={key} firstAttempt=replacement_unavailable(zeroParcel) "
            + $"retry=pendingResume firstParcel={firstParcel} retainedReservation={retainedReservation} "
            + $"cancelledParcel=allocationOnly(noRelease) resumedParcel={secondParcel} secondReserve=0 "
            + "staleCancelReplay=noDetach rows=shipments3/decisions1 state=Created",
            "tb262r1-orchestrator-relational.txt");
    }

    /// <summary>
    /// TB-258-R3 on real storage through the PUBLIC command: a resend recorded and then cancelled pre-dispatch is
    /// history. The exact retained request resolves that cancelled occurrence with zero writes, while the SAME key
    /// with a different source parcel or a different quantity is a typed conflict — cancellation is never an
    /// exemption from exact identity validation.
    /// </summary>
    [Fact]
    public async Task Relational_cancelled_occurrence_answers_only_its_exact_intent_through_the_public_command()
    {
        var connection = Environment.GetEnvironmentVariable(RelationalEnvVar);
        if (string.IsNullOrWhiteSpace(connection))
        {
            return;
        }

        var target = new NpgsqlConnectionStringBuilder(connection);
        var fixture = await CreateDispatchedFixtureAsync(target);
        var ports = new Ports("order.handle");
        var orders = new CheckoutReaderFake(
            fixture.CheckoutId, fixture.SellerOrderId, fixture.SellerPartyId, fixture.LineAId);
        var key = $"tb258r3-cancelled-{fixture.ShipmentId:N}";

        Guid resendShipmentId;
        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewAdapter(db), ports, orders);

            Assert.True((await orchestrator.RecordNonDeliveryAsync(
                fixture.CheckoutId,
                ActorId,
                NonDeliveryRequestFor(fixture, "customer_unavailable", "TB258R3 relational"),
                CancellationToken.None)).IsSuccess);
            Assert.True((await orchestrator.StartReturnAsync(
                fixture.CheckoutId, ActorId, ReturnStartRequest(fixture), CancellationToken.None)).IsSuccess);
            Assert.True((await orchestrator.RecordReturnReceiptAsync(
                fixture.CheckoutId,
                ActorId,
                ReceiptRequestFor(fixture, new AdminReturnReceiptLineSelection(fixture.LineAId, 2m, 0)),
                CancellationToken.None)).IsSuccess);

            var resend = await orchestrator.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 1m, key),
                CancellationToken.None);
            Assert.True(resend.IsSuccess, resend.IsSuccess ? string.Empty : resend.FirstError?.Code);
            resendShipmentId = await RawScalarGuidAsync(
                target,
                "select shipment_id from fulfillment.shipments where resend_of_shipment_id = @id",
                ("id", fixture.ShipmentId))
                ?? throw new InvalidOperationException("the resend parcel was not persisted");

            // A pre-dispatch cancellation through the owning production command: history, not a delete.
            await NewAdapter(db).CancelShipmentAsync(
                fixture.FulfillmentId, resendShipmentId, ActorId, CancellationToken.None);
            Assert.Equal("Cancelled", await RawShipmentStatusAsync(target, resendShipmentId));
        }

        var shipmentsBefore = await CountShipmentsAsync(target, fixture.FulfillmentId);
        var receiptsBefore = await CountReceiptsAsync(target, fixture.ShipmentId);
        var resendsBefore = await CountResendShipmentsAsync(target, fixture.ShipmentId);

        await using (var db = NewFulfillmentDb(target))
        {
            var orchestrator = Build(NewAdapter(db), ports, orders);

            // The exact retained request answers the cancelled occurrence — the parcel is not recreated.
            var exact = await orchestrator.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 1m, key),
                CancellationToken.None);
            Assert.True(exact.IsSuccess, exact.IsSuccess ? string.Empty : exact.FirstError?.Code);
            var exactSnapshot = Assert.IsType<FulfillmentSnapshot>(exact.Value);
            var cancelledParcel = exactSnapshot.Shipments.Single(x => x.ShipmentId == resendShipmentId);
            Assert.Equal(ShipmentOperationStatus.Cancelled, cancelledParcel.Status);

            // Same key, DIFFERENT source parcel: a typed conflict, never the cancelled answer.
            var changedSource = await orchestrator.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequestFrom(fixture, resendShipmentId, fixture.LineAId, 1m, key),
                CancellationToken.None);
            Assert.False(changedSource.IsSuccess);
            Assert.Equal("fulfillment.resend.intent_conflict", changedSource.FirstError.Code);

            // Same key, DIFFERENT quantity: the same typed conflict.
            var changedQuantity = await orchestrator.ResendShipmentAsync(
                fixture.CheckoutId,
                ActorId,
                ResendRequest(fixture, fixture.LineAId, 2m, key),
                CancellationToken.None);
            Assert.False(changedQuantity.IsSuccess);
            Assert.Equal("fulfillment.resend.intent_conflict", changedQuantity.FirstError.Code);
        }

        // Nothing was written or reallocated by the reconcile or the two conflicts.
        Assert.Equal(shipmentsBefore, await CountShipmentsAsync(target, fixture.FulfillmentId));
        Assert.Equal(receiptsBefore, await CountReceiptsAsync(target, fixture.ShipmentId));
        Assert.Equal(resendsBefore, await CountResendShipmentsAsync(target, fixture.ShipmentId));
        Assert.Equal("Cancelled", await RawShipmentStatusAsync(target, resendShipmentId));

        RecordEvidence(
            $"H cancel fulfillment={fixture.FulfillmentId} checkout={fixture.CheckoutId} "
            + $"source={fixture.ShipmentId} cancelledResend={resendShipmentId} key={key} "
            + "exact=resolvedCancelled changedSource=conflict changedQuantity=conflict "
            + $"rows=shipments{shipmentsBefore}/receipts{receiptsBefore}/resends{resendsBefore}",
            "tb258r3-orchestrator-relational.txt");
    }

    /// <summary>Task-labelled evidence of the public-command run; nothing runs without the directory.</summary>
    private static void RecordEvidence(string line, string fileName = "tb258r2-orchestrator-relational.txt")
    {
        var directory = Environment.GetEnvironmentVariable("TB258_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        File.AppendAllText(
            Path.Combine(directory, fileName),
            line + Environment.NewLine);
    }

    private static AdminOrderOperationRequest NonDeliveryRequest(string reason, string? note) =>
        new(
            "record_non_delivery",
            SellerOrderId,
            FulfillmentId,
            ShipmentId,
            ReturnRequestId: null,
            CarrierDisplayName: null,
            TrackingReference: null,
            Reason: reason,
            IdempotencyKey: null,
            ReturnItems: null,
            Note: note);

    private static AdminOrderOperationRequest ReceiptRequest(params AdminReturnReceiptLineSelection[] items) =>
        new(
            "record_return_receipt",
            SellerOrderId,
            FulfillmentId,
            ShipmentId,
            ReturnRequestId: null,
            CarrierDisplayName: null,
            TrackingReference: null,
            Reason: null,
            IdempotencyKey: null,
            ReturnItems: null,
            ReceiptItems: items);

    private static AdminOrderOperationsOrchestrator Build(FulfillmentFake fulfillment, Ports ports) =>
        Build(fulfillment, ports, new CheckoutReaderFake());

    private static AdminOrderOperationsOrchestrator Build(
        IFulfillmentAdminOperations fulfillment,
        Ports ports,
        CheckoutReaderFake orders) =>
        new(
            orders,
            fulfillment,
            ports.Returns,
            null!,
            new AccessFake(ports.Access),
            new PaymentFake(),
            new PaymentProjectionFake(),
            new SettlementFake(),
            new InventoryRecoveryFake(),
            new SupplyFake(),
            new CommittedShippingFake(),
            new ShippingHistoryFake(),
            new OccurrenceStoreFake());

    private static FulfillmentSnapshot FulfillmentSnapshotWith(
        ShipmentOperationStatus shipmentStatus,
        bool delivered,
        bool returned = false) =>
        new(
            FulfillmentId,
            SellerOrderId,
            CheckoutId,
            SellerPartyId,
            FulfillmentOperationStatus.Dispatched,
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان",
            "1234567890",
            "post",
            "پیشتاز",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), LineId, 2m, 2m, null, 0m, 2m)],
            [
                new ShipmentSnapshot(
                    ShipmentId,
                    shipmentStatus,
                    "پست",
                    "TRK-0001",
                    delivered || returned ? new DateTimeOffset(2026, 11, 8, 9, 0, 0, TimeSpan.Zero) : null,
                    delivered ? new DateTimeOffset(2026, 11, 9, 9, 0, 0, TimeSpan.Zero) : null,
                    [new ShipmentLineSnapshot(LineId, 2m)]),
            ]);

    /// <summary>Shared helpers so a test can assert whether the fresh projection was consulted.</summary>
    private sealed class Ports(params string[] permissionIds)
    {
        public ReturnsFake Returns { get; } = new();

        public OrderAdminEffectiveAccess Access { get; } =
            new(permissionIds.Select(id => new OrderAdminPermissionGrant(id, false)).ToList());
    }

    private sealed class CheckoutReaderFake(
        Guid? checkoutId = null,
        Guid? sellerOrderId = null,
        Guid? sellerPartyId = null,
        Guid? lineId = null) : IAdminOrderOperationsCheckoutReader
    {
        public Task<AdminOrderOpsCheckoutSnapshot?> GetAsync(Guid requestedCheckoutId, CancellationToken cancellationToken) =>
            Task.FromResult<AdminOrderOpsCheckoutSnapshot?>(new AdminOrderOpsCheckoutSnapshot(
                checkoutId ?? CheckoutId,
                ActorId,
                [
                    new AdminOrderOpsSellerOrderSnapshot(
                        sellerOrderId ?? SellerOrderId,
                        sellerPartyId ?? SellerPartyId,
                        SellerOrderStatus.Paid,
                        null,
                        [new AdminOrderOpsLineSnapshot(lineId ?? LineId, 2m)]),
                ]));
    }

    private sealed class AccessFake(OrderAdminEffectiveAccess access) : IOrderAdminEffectiveAccessReader
    {
        public Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult(access);
    }

    private sealed class ReturnsFake : IReturnAdminOperations
    {
        public int ProjectionCalls { get; private set; }

        public Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken)
        {
            ProjectionCalls++;
            return Task.FromResult<IReadOnlyList<ReturnSnapshot>>([]);
        }

        public Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ReturnStatusOverlayRow>>([]);

        public Task<ReturnEligibilityResult> EvaluateEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken)
        {
            ProjectionCalls++;
            return Task.FromResult(new ReturnEligibilityResult(
                sellerOrderId,
                CheckoutId,
                false,
                "return.not_delivered",
                null,
                null,
                []));
        }

        public Task<ReturnSnapshot> CreateAdminInitiatedAsync(
            CreateAdminReturnCommand command,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ReturnSnapshot> ApproveAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReturnSnapshot> RejectAsync(
            Guid returnRequestId,
            Guid actorUserId,
            string? reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ReturnSnapshot> RetryRefundAsync(
            Guid returnRequestId,
            Guid actorUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class PaymentFake : IPaymentAdminGateway
    {
        public Task<PaymentAdminOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentAdminOperationalSnapshot?>(null);

        public Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RestoreAfterOrderCancelRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> ConfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> RejectDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> RestoreDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> UnconfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class PaymentProjectionFake : IOrderPaymentProjectionPort
    {
        public Task ApplyVerifiedSuccessAsync(
            Guid checkoutId,
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RevertVerifiedSuccessAsync(
            Guid checkoutId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task PromoteReservationsForManualPaymentReviewAsync(
            Guid checkoutId,
            DateTimeOffset reviewExpiresAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ReleaseReservationsAfterManualRejectAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class SettlementFake : ISettlementOrderAccrualPort
    {
        public Task<IReadOnlyDictionary<Guid, SellerOrderRestoreSettlementGate>> GetRestoreGatesAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, SellerOrderRestoreSettlementGate>>(
                new Dictionary<Guid, SellerOrderRestoreSettlementGate>());

        public Task NeutralizeUnpaidAccrualForCancelAsync(
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ReinstateAccrualAfterCancelRestoreAsync(
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task VoidUnpaidAccrualForPaymentAsync(
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class InventoryRecoveryFake : IAdminOrderOperationsInventoryRecoveryPort
    {
        public Task<AdminOrderInventoryRecoveryAssessment> AssessAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AdminOrderInventoryRecoveryAssessment(checkoutId, "A", false, string.Empty));

        public Task<AdminOrderInventoryRecoveryResult> RecoverAsync(
            Guid checkoutId,
            Guid actorUserId,
            string? reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class SupplyFake : IAdminOrderOperationsSupplyPort
    {
        public Task<AdminOrderOpsSupplyStatus> GetStatusAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminOrderOpsSupplyStatus(checkoutId, "Available", string.Empty, []));

        public Task<AdminOrderOpsSupplyEnsureResult> EnsurePaidDurableAsync(
            Guid checkoutId,
            string reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class CommittedShippingFake : IOrderCommittedCarrierShipmentFactsReader
    {
        public Task<OrderCommittedCarrierShipmentFacts?> GetAsync(
            Guid checkoutId,
            Guid shippingGroupId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderCommittedCarrierShipmentFacts?>(null);

        public Task<IReadOnlyList<OrderCommittedCarrierShipmentFacts>> ListByCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderCommittedCarrierShipmentFacts>>([]);
    }

    private sealed class ShippingHistoryFake : IOrderShippingGroupHistoryReader
    {
        public Task<IReadOnlyList<OrderShippingGroupHistoryRecord>> GetForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderShippingGroupHistoryRecord>>([]);
    }

    private sealed class OccurrenceStoreFake : IOrderOperationalGroupOccurrenceStore
    {
        public Task<Tooba.Order.Application.Admin.Operations.Ports.OperationalGroupOccurrenceRecord> ClaimAsync(
            OperationalGroupOccurrenceEnvelope envelope,
            string envelopeFingerprint,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<OperationalGroupOccurrenceAllocation> RecordMemberAllocationAsync(
            Guid occurrenceId,
            OperationalGroupOccurrenceAllocation allocation,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordCompletedAsync(
            Guid occurrenceId,
            Guid consolidatedPackageId,
            string packageNumber,
            string shippingMethodCode,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RecordCompletedSeparateAsync(
            Guid occurrenceId,
            string shippingMethodCode,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Tooba.Order.Application.Admin.Operations.Ports.OperationalGroupOccurrenceRecord?> GetAsync(
            Guid occurrenceId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Tooba.Order.Application.Admin.Operations.Ports.OperationalGroupOccurrenceRecord?>(null);
    }

    private sealed class FulfillmentFake : IFulfillmentAdminOperations
    {
        private readonly FulfillmentSnapshot _current;
        private readonly PostDispatchIntentResult _recorded;

        public FulfillmentFake(FulfillmentSnapshot current, PostDispatchIntentResult recorded)
        {
            _current = current;
            _recorded = recorded;
        }

        public int MutationCalls { get; private set; }
        public int NonDeliveryCalls { get; private set; }
        public List<PostDispatchIntent> ReconcileCalls { get; } = [];

        public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentSnapshot?>(_current);

        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FulfillmentSnapshot>>([_current]);

        public Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ConsolidatedPackageSnapshot>>([]);

        public Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(
            IReadOnlyList<Guid> shipmentIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ActivePackageMembershipSnapshot>>([]);

        public Task<PostDispatchIntentResult> ReconcilePostDispatchIntentAsync(
            PostDispatchIntent intent,
            CancellationToken cancellationToken)
        {
            ReconcileCalls.Add(intent);
            return Task.FromResult(_recorded);
        }

        public Task<FulfillmentSnapshot> RecordNonDeliveryAsync(
            Guid fulfillmentId,
            Guid shipmentId,
            Guid actorUserId,
            string reason,
            string? note,
            CancellationToken cancellationToken)
        {
            MutationCalls++;
            NonDeliveryCalls++;
            return Task.FromResult(_current);
        }

        public Task<FulfillmentSnapshot> StartReturnAsync(
            Guid fulfillmentId,
            Guid shipmentId,
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            MutationCalls++;
            return Task.FromResult(_current);
        }

        public Task<FulfillmentSnapshot> RecordReturnReceiptAsync(
            Guid fulfillmentId,
            Guid shipmentId,
            Guid actorUserId,
            IReadOnlyList<ReturnReceiptLineCommand> items,
            string? note,
            CancellationToken cancellationToken)
        {
            MutationCalls++;
            return Task.FromResult(_current);
        }

        public Task<FulfillmentShipmentCreateResult> CreateResendShipmentAsync(
            Guid fulfillmentId,
            Guid actorUserId,
            Guid originalShipmentId,
            IReadOnlyList<ShipmentLineCommand> items,
            CancellationToken cancellationToken,
            string? shippingDispatchKey = null)
        {
            MutationCalls++;
            return Task.FromResult(new FulfillmentShipmentCreateResult(_current, Guid.NewGuid()));
        }

        public Task<DamagedResolutionResult> ResolveDamagedGoodsAsync(
            Guid fulfillmentId,
            Guid actorUserId,
            Guid shipmentId,
            Guid orderLineId,
            decimal quantity,
            DamagedResolutionOutcome outcome,
            string? occurrenceKey,
            string? note,
            CancellationToken cancellationToken)
        {
            MutationCalls++;
            return Task.FromResult(new DamagedResolutionResult(_current, Guid.NewGuid(), DamagedResolutionStatus.Recorded));
        }

        public Task<DamagedResolutionIntentResult> ReconcileDamagedResolutionAsync(
            DamagedResolutionIntent intent,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DamagedResolutionIntentResult(PostDispatchIntentState.NotRecorded));

        public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IReadOnlyList<ShippingMethodOption> ListEnabledShippingMethods() => [];

        public string ResolveShippingMethodLabel(string? code, string? fallbackDisplayName) =>
            fallbackDisplayName ?? code ?? string.Empty;

        public Task<FulfillmentSnapshot> ProcessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> UnprocessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> PackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> UnpackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(
            Guid fulfillmentId,
            Guid actorUserId,
            string carrierDisplayName,
            IReadOnlyList<ShipmentLineCommand> items,
            string? shippingMethodCode,
            string? providerMetadataJson,
            CancellationToken cancellationToken,
            Guid? shippingGroupId = null,
            string? shippingDispatchKey = null) => throw new NotSupportedException();

        public Task<FulfillmentSnapshot> CancelShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> AssignTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> CorrectTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> DeliverShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FulfillmentSnapshot> DispatchShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(
            Guid checkoutId,
            IReadOnlyList<Guid> shipmentIds,
            string? shippingMethodCode,
            string? trackingReference,
            string? note,
            Guid actorUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(Guid consolidatedPackageId, string trackingReference, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task EnsureCreatedForPaidCheckoutAsync(Guid checkoutId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(
            IReadOnlyList<Guid> checkoutIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
    }

    // ---- TB-258-R2 relational fixture helpers ---------------------------------------------------

    /// <summary>The ONE canonical database; nothing runs (and nothing is claimed) without it.</summary>
    private const string RelationalEnvVar = "TB258_CONNECTION";

    private static AdminOrderOperationRequest NonDeliveryRequestFor(
        RelationalFixture fixture,
        string reason,
        string? note) =>
        new(
            "record_non_delivery",
            fixture.SellerOrderId,
            fixture.FulfillmentId,
            fixture.ShipmentId,
            ReturnRequestId: null,
            CarrierDisplayName: null,
            TrackingReference: null,
            Reason: reason,
            IdempotencyKey: null,
            ReturnItems: null,
            Note: note);

    private static AdminOrderOperationRequest ReturnStartRequest(RelationalFixture fixture) =>
        new(
            "start_return",
            fixture.SellerOrderId,
            fixture.FulfillmentId,
            fixture.ShipmentId,
            ReturnRequestId: null,
            CarrierDisplayName: null,
            TrackingReference: null,
            Reason: null,
            IdempotencyKey: null,
            ReturnItems: null);

    private static AdminOrderOperationRequest ReceiptRequestFor(
        RelationalFixture fixture,
        params AdminReturnReceiptLineSelection[] items) =>
        new(
            "record_return_receipt",
            fixture.SellerOrderId,
            fixture.FulfillmentId,
            fixture.ShipmentId,
            ReturnRequestId: null,
            CarrierDisplayName: null,
            TrackingReference: null,
            Reason: null,
            IdempotencyKey: null,
            ReturnItems: null,
            ReceiptItems: items);

    private static AdminOrderOperationRequest ResendRequest(
        RelationalFixture fixture,
        Guid lineId,
        decimal quantity,
        string key) =>
        ResendRequestFrom(fixture, fixture.ShipmentId, lineId, quantity, key);

    /// <summary>TB-258-R3: the same reviewed resend against an explicitly chosen source parcel.</summary>
    private static AdminOrderOperationRequest ResendRequestFrom(
        RelationalFixture fixture,
        Guid sourceShipmentId,
        Guid lineId,
        decimal quantity,
        string key) =>
        new(
            "resend_shipment",
            fixture.SellerOrderId,
            fixture.FulfillmentId,
            sourceShipmentId,
            ReturnRequestId: null,
            CarrierDisplayName: null,
            TrackingReference: null,
            Reason: null,
            IdempotencyKey: key,
            ReturnItems: null,
            Selections: [new AdminOrderLineSelection(lineId, quantity)]);

    private static FulfillmentDbContext NewFulfillmentDb(NpgsqlConnectionStringBuilder target)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    /// <summary>
    /// The PRODUCTION admin adapter over the PRODUCTION directory — the exact composition the Host resolves, never a
    /// test-local mapping of the aggregate.
    /// </summary>
    private static IFulfillmentAdminOperations NewAdapter(FulfillmentDbContext db) =>
        new FulfillmentAdminOperationsAdapter(
            new FulfillmentDirectory(
                db,
                new OpenFulfillmentUseCaseGuard(),
                new UnusedOrderReader(),
                new UnusedInventoryGateway(),
                new FulfillmentInstrumentation(),
                new FixedRelationalClock(),
                new RelationalIdGenerator()),
            new FulfillmentShippedQuantityReader(db),
            new ShippingMethodsOptions());

    /// <summary>
    /// TB-262: the SAME production adapter, but with an Inventory gateway that represents the replacement reservation
    /// seam (the real Inventory-owned port is proven by the Fulfillment relational matrix).
    /// </summary>
    private static IFulfillmentAdminOperations NewDamagedAdapter(
        FulfillmentDbContext db,
        IFulfillmentInventoryGateway inventoryGateway) =>
        new FulfillmentAdminOperationsAdapter(
            new FulfillmentDirectory(
                db,
                new OpenFulfillmentUseCaseGuard(),
                new UnusedOrderReader(),
                inventoryGateway,
                new FulfillmentInstrumentation(),
                new FixedRelationalClock(),
                new RelationalIdGenerator()),
            new FulfillmentShippedQuantityReader(db),
            new ShippingMethodsOptions());

    private static AdminOrderOperationRequest DamagedRequest(
        RelationalFixture fixture,
        Guid lineId,
        decimal quantity,
        string outcome,
        string key) =>
        new(
            "resolve_damaged_goods",
            fixture.SellerOrderId,
            fixture.FulfillmentId,
            fixture.ShipmentId,
            ReturnRequestId: null,
            CarrierDisplayName: null,
            TrackingReference: null,
            Reason: null,
            IdempotencyKey: key,
            ReturnItems: null,
            OrderLineId: lineId,
            Quantity: quantity,
            DamagedOutcome: outcome);

    private static async Task<RelationalFixture> CreateDispatchedFixtureAsync(
        NpgsqlConnectionStringBuilder target,
        IFulfillmentInventoryGateway? inventoryGateway = null,
        Guid? lineAReservationId = null,
        Guid? lineBReservationId = null)
    {
        var fulfillmentId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        await using var db = NewFulfillmentDb(target);
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId,
            Guid.NewGuid,
            sellerOrderId,
            checkoutId,
            sellerPartyId,
            Guid.NewGuid(),
            "TB258R2 relational receiver",
            "09120000000",
            "تهران",
            "تهران",
            "نشانی آزمون تسک 258-R2",
            "1234567890",
            "post",
            "پیشتاز",
            [(lineA, 2m, lineAReservationId), (lineB, 1m, lineBReservationId)],
            now,
            [
                new FulfillmentShippingGroupPlan(
                    groupId,
                    sellerPartyId,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    DateOnly.FromDateTime(now.UtcDateTime),
                    null,
                    null,
                    1,
                    [lineA, lineB]),
            ]);
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        db.ShippingGroups.AddRange(unit.ShippingGroups);
        await db.SaveChangesAsync();

        var directory = new FulfillmentDirectory(
            db,
            new OpenFulfillmentUseCaseGuard(),
            new UnusedOrderReader(),
            inventoryGateway ?? new UnusedInventoryGateway(),
            new FulfillmentInstrumentation(),
            new FixedRelationalClock(),
            new RelationalIdGenerator());

        await directory.MarkProcessingAsync(fulfillmentId, actorUserId, CancellationToken.None);
        await directory.PackSelectionsAsync(
            fulfillmentId,
            actorUserId,
            [
                new AppModels.FulfillmentSelectionCommand(lineA, 2m),
                new AppModels.FulfillmentSelectionCommand(lineB, 1m),
            ],
            CancellationToken.None);

        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            "پست",
            [new AppModels.ShipmentLineCommand(lineA, 2m), new AppModels.ShipmentLineCommand(lineB, 1m)],
            CancellationToken.None,
            shippingMethodCode: "post",
            providerMetadataJson:
                "{\"recipientName\":\"TB258R2 receiver\",\"recipientPhone\":\"09120000000\","
                + "\"postalCode\":\"1234567890\",\"destinationAddress\":\"TB258R2 task address\"}",
            shippingGroupId: groupId);
        await directory.AssignTrackingAsync(
            fulfillmentId,
            created.CreatedShipmentId,
            actorUserId,
            $"TRK-TB258R2-G-{created.CreatedShipmentId:N}",
            CancellationToken.None);
        await directory.DispatchShipmentAsync(
            fulfillmentId, created.CreatedShipmentId, actorUserId, CancellationToken.None);

        Assert.Equal("Dispatched", await RawShipmentStatusAsync(target, created.CreatedShipmentId));

        return new RelationalFixture(
            target,
            fulfillmentId,
            checkoutId,
            sellerPartyId,
            sellerOrderId,
            groupId,
            lineA,
            lineB,
            created.CreatedShipmentId,
            actorUserId);
    }

    private sealed record RelationalFixture(
        NpgsqlConnectionStringBuilder Target,
        Guid FulfillmentId,
        Guid CheckoutId,
        Guid SellerPartyId,
        Guid SellerOrderId,
        Guid GroupId,
        Guid LineAId,
        Guid LineBId,
        Guid ShipmentId,
        Guid ActorUserId)
    {
        /// <summary>A checkout that exists but owns no parcel of this fulfillment.</summary>
        public Guid ForeignCheckoutId { get; } = Guid.NewGuid();

        /// <summary>The reviewed resend occurrence key of this fixture.</summary>
        public string ResendKey => $"tb258r2-relational-{ShipmentId:N}";
    }

    private static async Task<int> CountShipmentsAsync(NpgsqlConnectionStringBuilder target, Guid fulfillmentId) =>
        await RawScalarAsync(
            target,
            "select count(*) from fulfillment.shipments where fulfillment_id = @id",
            ("id", fulfillmentId));

    private static async Task<int> CountReceiptsAsync(NpgsqlConnectionStringBuilder target, Guid shipmentId) =>
        await RawScalarAsync(
            target,
            "select count(*) from fulfillment.shipment_return_receipts where shipment_id = @id",
            ("id", shipmentId));

    /// <summary>TB-258-R3: the resend parcels funded by one returned source parcel.</summary>
    private static async Task<int> CountResendShipmentsAsync(
        NpgsqlConnectionStringBuilder target,
        Guid shipmentId) =>
        await RawScalarAsync(
            target,
            "select count(*) from fulfillment.shipments where resend_of_shipment_id = @id",
            ("id", shipmentId));

    /// <summary>TB-262: the durable damaged-goods decisions of one fulfillment unit.</summary>
    private static async Task<int> CountDamagedResolutionsAsync(
        NpgsqlConnectionStringBuilder target,
        Guid fulfillmentId) =>
        await RawScalarAsync(
            target,
            "select count(*) from fulfillment.damaged_goods_resolutions where fulfillment_id = @id",
            ("id", fulfillmentId));

    private static async Task<string?> RawShipmentStatusAsync(
        NpgsqlConnectionStringBuilder target,
        Guid shipmentId) =>
        await RawScalarStringAsync(
            target,
            "select status from fulfillment.shipments where shipment_id = @id",
            ("id", shipmentId));

    private static async Task<Guid?> RawScalarGuidAsync(
        NpgsqlConnectionStringBuilder target,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var value = await command.ExecuteScalarAsync();
        return value is Guid id ? id : null;
    }

    private static async Task<string?> RawScalarStringAsync(
        NpgsqlConnectionStringBuilder target,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        return await command.ExecuteScalarAsync() as string;
    }

    private static async Task<int> RawScalarAsync(
        NpgsqlConnectionStringBuilder target,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class FixedRelationalClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 10, 9, 30, 0, TimeSpan.Zero);
    }

    private sealed class RelationalIdGenerator : IIdGenerator
    {
        public Guid NewId() => Guid.NewGuid();
    }

    private sealed class UnusedOrderReader : Tooba.Order.Contracts.Fulfillment.IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258-R2 fixture does not read orders.");

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258-R2 fixture does not read orders.");
    }

    /// <summary>
    /// TB-262: the damaged-replacement Inventory seam as the owning contracts describe it. Only the replacement
    /// reservation is exercised; the original sale reservation is consumed once by the first dispatch.
    /// </summary>
    private sealed class ReplacementInventoryGateway
        : Tooba.Fulfillment.Application.Fulfillments.Ports.IFulfillmentInventoryGateway
    {
        public Guid ReservationId { get; } = Guid.NewGuid();

        public int ReserveCalls { get; private set; }

        public int OriginalConsumeCalls { get; private set; }

        public int CommitCalls { get; private set; }

        /// <summary>TB-262-R1: proves a parcel cancellation never releases the order-bound hold.</summary>
        public int ReleaseCalls { get; private set; }

        /// <summary>
        /// TB-262 RETENTION RULE: the NEW replacement hold is committed for the ACTIVE paid order (its TTL is cleared),
        /// so no hold timeout can ever return the stock to saleable availability. The committed SALE reservation is
        /// never touched here.
        /// </summary>
        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            Assert.Equal(ReservationId, reservationId);
            CommitCalls++;
            return Task.CompletedTask;
        }

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            OriginalConsumeCalls++;
            return Task.CompletedTask;
        }

        public Task ConsumeReplacementReservationAsync(
            Guid reservationId,
            string consumerKey,
            CancellationToken cancellationToken)
        {
            Assert.Equal(ReservationId, reservationId);
            Assert.False(string.IsNullOrWhiteSpace(consumerKey));
            return Task.CompletedTask;
        }

        public Task ReleaseReplacementReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            // TB-262-R1: an active order must never get its retained replacement stock back into saleable availability
            // through a parcel cancellation, so this is counted rather than silently ignored.
            ReleaseCalls++;
            return Task.CompletedTask;
        }

        public Task<FulfillmentReplacementReservation> ReserveReplacementReservationAsync(
            Guid originalReservationId,
            decimal quantity,
            string reservationKey,
            string externalReference,
            CancellationToken cancellationToken)
        {
            // The replacement is funded from the committed SALE reservation's own offer/stock context, never from a
            // client-supplied identity, and the external reference carries the durable decision id.
            Assert.NotEqual(Guid.Empty, originalReservationId);
            Assert.True(quantity > 0m);
            Assert.False(string.IsNullOrWhiteSpace(reservationKey));
            Assert.StartsWith("damaged-resolution:", externalReference);
            ReserveCalls++;
            return Task.FromResult(new FulfillmentReplacementReservation(
                ReservationId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                quantity,
                "Held"));
        }

        public Task<FulfillmentReplacementReservation?> FindReplacementReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentReplacementReservation?>(
                reservationId == ReservationId
                    ? new FulfillmentReplacementReservation(ReservationId, Guid.NewGuid(), Guid.NewGuid(), 1m, "Held")
                    : null);
    }

    private sealed class UnusedInventoryGateway
        : Tooba.Fulfillment.Application.Fulfillments.Ports.IFulfillmentInventoryGateway
    {
        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258-R2 fixture does not touch inventory.");

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-258-R2 fixture does not touch inventory.");
    }

    /// <summary>
    /// TB-262-R1: an Inventory seam with NO healthy stock for this offer. It reports exactly what the owning
    /// Inventory contract reports (<c>inventory.supply.unavailable</c>) and leaves no hold behind, so a resumed
    /// replacement that still succeeds proves the retained order-bound hold was reused, not re-reserved.
    /// </summary>
    private sealed class StarvedReplacementInventoryGateway
        : Tooba.Fulfillment.Application.Fulfillments.Ports.IFulfillmentInventoryGateway
    {
        public int ReserveAttempts { get; private set; }

        public int ReleaseCalls { get; private set; }

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-262-R1 starved fixture never commits a hold.");

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-262-R1 starved fixture never consumes a hold.");

        public Task ReleaseReplacementReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            ReleaseCalls++;
            return Task.CompletedTask;
        }

        public Task<FulfillmentReplacementReservation> ReserveReplacementReservationAsync(
            Guid originalReservationId,
            decimal quantity,
            string reservationKey,
            string externalReference,
            CancellationToken cancellationToken)
        {
            ReserveAttempts++;
            throw new ContractOperationException(
                Tooba.Inventory.Contracts.Errors.InventoryErrorCodes.SupplyUnavailable);
        }

        /// <summary>The retained hold is still live and unexpired: the only release path is an order cancellation.</summary>
        public Task<FulfillmentReplacementReservation?> FindReplacementReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentReplacementReservation?>(
                reservationId == Guid.Empty
                    ? null
                    : new FulfillmentReplacementReservation(
                        reservationId, Guid.NewGuid(), Guid.NewGuid(), 1m, "Held"));
    }
}
