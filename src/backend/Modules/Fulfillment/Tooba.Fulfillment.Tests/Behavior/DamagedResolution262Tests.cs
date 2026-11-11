using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-262 (pure domain): the damaged-goods decision of a physically returned parcel line.
///
/// These are the invariants that make damage a real, owned operational state instead of a dead end: damaged units are
/// never usable resend capacity, they never raise saleable stock (the original sale already consumed them), and each
/// damaged unit is bound to exactly ONE durable, source-scoped decision so it can never fund both a replacement and a
/// referral/another replacement. Nothing here touches storage; the real relational proof lives elsewhere.
/// </summary>
/// <remarks>
/// The fixture models one paid order of two lines that left the warehouse in one parcel, failed delivery, came back
/// and was physically received at origin with per-condition quantities.
/// </remarks>
public sealed class DamagedResolution262Tests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);
    private static readonly DateTimeOffset T2 = T0.AddHours(2);
    private static readonly DateTimeOffset T3 = T0.AddHours(3);
    private static readonly DateTimeOffset T4 = T0.AddHours(4);
    private static readonly DateTimeOffset T5 = T0.AddHours(5);
    private static readonly DateTimeOffset T6 = T0.AddHours(6);
    private static readonly Guid Actor = Guid.Parse("018f2620-0000-7000-8000-0000000000a1");

    private sealed record Fixture(
        FulfillmentUnit Unit,
        Guid ShippingGroupId,
        Guid LineA,
        Guid LineB,
        Guid ShipmentId);

    /// <summary>
    /// One delivered-failed parcel carrying 3 units of A and 1 unit of B, physically received back at origin with the
    /// requested split of A: `usableA` healthy units and `damagedA` damaged units.
    /// </summary>
    private static Fixture ReturnedParcel(decimal usableA, decimal damagedA)
    {
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sellerPartyId,
            Guid.NewGuid(),
            "گیرندهٔ آزمون ۲۶۲",
            "09120000001",
            "تهران",
            "تهران",
            "نشانی آزمون تسک ۲۶۲",
            "1234567890",
            "post",
            "پیشتاز",
            [(lineA, 3m, (Guid?)null), (lineB, 1m, (Guid?)null)],
            T0,
            [
                new FulfillmentShippingGroupPlan(
                    groupId,
                    sellerPartyId,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    DateOnly.FromDateTime(T0.UtcDateTime),
                    null,
                    null,
                    1,
                    [lineA, lineB]),
            ]);

        unit.MarkProcessing(T0);
        unit.MarkPacked(T0);
        var shipment = unit.CreateShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            "پست",
            [(lineA, 3m), (lineB, 1m)],
            T0,
            "post",
            "پیشتاز",
            shippingGroupId: groupId);
        unit.AssignTracking(shipment.ShipmentId, "TRK-TB262-1", T0);
        unit.ApplyShipmentDispatched(shipment.ShipmentId, T0);
        unit.RecordShipmentNonDelivery(shipment.ShipmentId, "customer_unavailable", null, Actor, T1);
        unit.StartShipmentReturn(shipment.ShipmentId, Actor, T2);

        var items = new List<(Guid OrderLineId, decimal Quantity, ReturnReceiptCondition Condition)>();
        if (usableA > 0m) items.Add((lineA, usableA, ReturnReceiptCondition.Usable));
        if (damagedA > 0m) items.Add((lineA, damagedA, ReturnReceiptCondition.Damaged));
        items.Add((lineB, 1m, ReturnReceiptCondition.Usable));
        unit.RecordReturnReceipt(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            shipment.ShipmentId,
            Actor,
            items,
            T3,
            null,
            T3);

        return new Fixture(unit, groupId, lineA, lineB, shipment.ShipmentId);
    }

    // ---------------------------------------------------------------- A: damaged is not resend capacity

    [Fact]
    public void A_damaged_unit_never_becomes_resend_capacity_and_stays_outstanding()
    {
        var fixture = ReturnedParcel(usableA: 0m, damagedA: 1m);

        Assert.Equal(1m, fixture.Unit.ReceiptDamagedQuantity(fixture.ShipmentId, fixture.LineA));
        Assert.Equal(0m, fixture.Unit.ReturnedUsableQuantity(fixture.LineA));
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));
        // The sibling healthy line keeps its own capacity; damaged units of A never lend to it or from it.
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.LineB));
        // The damaged unit is a real unresolved requirement, not a silent loss.
        Assert.Equal(1m, fixture.Unit.OutstandingDamagedQuantity(fixture.ShipmentId, fixture.LineA));
        Assert.True(fixture.Unit.HasOutstandingDamagedQuantity(fixture.ShipmentId));
        // No customer unit was delivered by this path.
        Assert.Equal(0m, fixture.Unit.DeliveredQuantity(fixture.LineA));
    }

    [Fact]
    public void Usable_two_plus_damaged_one_keeps_the_two_conditions_strictly_separate()
    {
        var fixture = ReturnedParcel(usableA: 2m, damagedA: 1m);

        // Healthy returned capacity funds a resend of exactly 2 and nothing more.
        Assert.Equal(2m, fixture.Unit.ResendableQuantity(fixture.LineA));
        // Damaged units are NOT part of that capacity and remain an owned obligation.
        Assert.Equal(1m, fixture.Unit.ReceiptDamagedQuantity(fixture.ShipmentId, fixture.LineA));
        Assert.Equal(1m, fixture.Unit.OutstandingDamagedQuantity(fixture.ShipmentId, fixture.LineA));
        // A sibling line's condition facts are never merged into this one.
        Assert.Equal(0m, fixture.Unit.ReceiptDamagedQuantity(fixture.ShipmentId, fixture.LineB));
    }

    // ---------------------------------------------------------------- B: one durable, bounded decision

    [Fact]
    public void A_decision_is_bounded_by_its_own_source_parcel_outstanding_damaged_quantity()
    {
        var fixture = ReturnedParcel(usableA: 2m, damagedA: 1m);

        var over = Assert.Throws<ContractOperationException>(() => fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 2m, DamagedResolutionOutcome.Replacement,
            "occ-over", Actor, null, T4));
        Assert.Equal("fulfillment.damage.exceeds_outstanding", over.Code);

        var healthyLine = Assert.Throws<ContractOperationException>(() => fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineB, 1m, DamagedResolutionOutcome.Replacement,
            "occ-healthy", Actor, null, T4));
        Assert.Equal("fulfillment.damage.no_damaged_quantity", healthyLine.Code);

        var foreignLine = Assert.Throws<ContractOperationException>(() => fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, Guid.NewGuid(), 1m, DamagedResolutionOutcome.Replacement,
            "occ-foreign", Actor, null, T4));
        Assert.Equal("fulfillment.damage.line_foreign", foreignLine.Code);

        var blankKey = Assert.Throws<ContractOperationException>(() => fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement,
            "   ", Actor, null, T4));
        Assert.Equal("fulfillment.damage.resolution_occurrence_required", blankKey.Code);

        Assert.Empty(fixture.Unit.DamagedResolutions);
        Assert.Equal(1m, fixture.Unit.OutstandingDamagedQuantity(fixture.ShipmentId, fixture.LineA));
    }

    [Fact]
    public void An_exact_retry_answers_the_recorded_decision_and_a_changed_intent_conflicts()
    {
        var fixture = ReturnedParcel(usableA: 2m, damagedA: 1m);
        var resolutionId = Guid.NewGuid();

        var recorded = fixture.Unit.RecordDamagedResolution(
            resolutionId, fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement,
            "occ-1", Actor, "یادداشت", T4);
        // The damaged unit is now committed exactly once.
        Assert.Equal(0m, fixture.Unit.OutstandingDamagedQuantity(fixture.ShipmentId, fixture.LineA));
        Assert.Single(fixture.Unit.DamagedResolutions);

        // Same occurrence + exact intent is a replay: the SAME decision, nothing new written.
        var replay = fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement,
            "occ-1", Actor, null, T5);
        Assert.Equal(recorded.DamagedGoodsResolutionId, replay.DamagedGoodsResolutionId);
        Assert.Single(fixture.Unit.DamagedResolutions);

        var changedQuantity = Assert.Throws<ContractOperationException>(() => fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Referral,
            "occ-1", Actor, null, T5));
        Assert.Equal("fulfillment.damage.resolution_intent_conflict", changedQuantity.Code);

        // A different occurrence can never resolve the same unit a second time.
        var second = Assert.Throws<ContractOperationException>(() => fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement,
            "occ-2", Actor, null, T5));
        Assert.Equal("fulfillment.damage.exceeds_outstanding", second.Code);

        // The read-only exact-intent answer follows the same identity rules.
        var answer = fixture.Unit.ResolveRecordedDamagedResolution(
            "occ-1", fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement);
        Assert.Equal(FulfillmentUnit.RecordedDamagedResolutionState.Recorded, answer.State);
        var conflict = fixture.Unit.ResolveRecordedDamagedResolution(
            "occ-1", fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Referral);
        Assert.Equal(FulfillmentUnit.RecordedDamagedResolutionState.Conflict, conflict.State);
        var absent = fixture.Unit.ResolveRecordedDamagedResolution(
            "occ-missing", fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement);
        Assert.Equal(FulfillmentUnit.RecordedDamagedResolutionState.NotRecorded, absent.State);
    }

    [Fact]
    public void A_referral_holds_the_units_without_allocating_anything()
    {
        var fixture = ReturnedParcel(usableA: 2m, damagedA: 1m);

        var referral = fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Referral,
            "occ-referral", Actor, null, T4);

        // The unit is bound (no second decision) but nothing is claimed complete and nothing was allocated.
        Assert.Equal(0m, fixture.Unit.OutstandingDamagedQuantity(fixture.ShipmentId, fixture.LineA));
        Assert.Equal(DamagedResolutionStatus.Recorded, referral.Status);
        Assert.Null(referral.ReplacementReservationId);
        Assert.Null(referral.ReplacementShipmentId);
        Assert.Single(fixture.Unit.Shipments);
        // Healthy capacity is untouched by a damaged decision.
        Assert.Equal(2m, fixture.Unit.ResendableQuantity(fixture.LineA));

        var notReplacement = Assert.Throws<ContractOperationException>(() =>
            fixture.Unit.CreateReplacementShipment(
                Guid.NewGuid(), () => Guid.NewGuid(), referral.DamagedGoodsResolutionId, Actor, string.Empty, T5));
        Assert.Equal("fulfillment.damage.resolution_not_replacement", notReplacement.Code);
    }

    // ---------------------------------------------------------------- C: replacement parcel lifecycle

    [Fact]
    public void A_replacement_needs_its_own_reservation_and_links_the_exact_source()
    {
        var fixture = ReturnedParcel(usableA: 2m, damagedA: 1m);
        var resolution = fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement,
            "occ-replacement", Actor, null, T4);

        var withoutReservation = Assert.Throws<ContractOperationException>(() =>
            fixture.Unit.CreateReplacementShipment(
                Guid.NewGuid(), () => Guid.NewGuid(), resolution.DamagedGoodsResolutionId, Actor, string.Empty, T5));
        Assert.Equal("fulfillment.damage.reservation_required", withoutReservation.Code);

        var reservationId = Guid.NewGuid();
        fixture.Unit.AttachDamagedResolutionReservation(resolution.DamagedGoodsResolutionId, reservationId, T5);

        var replacement = fixture.Unit.CreateReplacementShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), resolution.DamagedGoodsResolutionId, Actor, string.Empty, T5);

        Assert.True(replacement.IsReplacement);
        Assert.Equal(fixture.ShipmentId, replacement.ResendOfShipmentId);
        Assert.Equal(reservationId, replacement.ReplacementReservationId);
        Assert.Equal(resolution.DamagedGoodsResolutionId, replacement.DamagedResolutionId);
        Assert.Equal(fixture.ShippingGroupId, replacement.ShippingGroupId);
        Assert.Equal("post", replacement.ShippingMethodCode);
        Assert.Equal(1m, replacement.Items.Single(x => x.OrderLineId == fixture.LineA).Quantity);
        // The replacement is funded by its OWN reservation, never by returned usable capacity.
        Assert.Equal(2m, fixture.Unit.ResendableQuantity(fixture.LineA));
        Assert.Equal(DamagedResolutionStatus.Fulfilled, resolution.Status);

        // Creating it twice replays the SAME recorded parcel instead of allocating another one.
        var replay = fixture.Unit.CreateReplacementShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), resolution.DamagedGoodsResolutionId, Actor, string.Empty, T5.AddHours(1));
        Assert.Equal(replacement.ShipmentId, replay.ShipmentId);
        Assert.Equal(2, fixture.Unit.Shipments.Count);
    }

    [Fact]
    public void Cancelling_a_pre_dispatch_replacement_retains_the_hold_until_the_order_is_cancelled()
    {
        var fixture = ReturnedParcel(usableA: 2m, damagedA: 1m);
        var resolution = fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement,
            "occ-cancel", Actor, null, T4);
        var reservationId = Guid.NewGuid();
        fixture.Unit.AttachDamagedResolutionReservation(resolution.DamagedGoodsResolutionId, reservationId, T5);
        var replacement = fixture.Unit.CreateReplacementShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), resolution.DamagedGoodsResolutionId, Actor, string.Empty, T5);

        fixture.Unit.CancelShipment(replacement.ShipmentId, T5.AddMinutes(1));
        fixture.Unit.RetainReplacementStockForCancelledParcel(replacement.ShipmentId, T5.AddMinutes(1));

        // RETENTION RULE: the order is still active, so the purpose-bound hold must NOT become saleable again.
        Assert.Equal(reservationId, resolution.ReplacementReservationId);
        Assert.Null(resolution.ReplacementShipmentId);
        // Still OWES the replacement, with the hold retained and a fresh attempt pending.
        Assert.Equal(DamagedResolutionStatus.Reserved, resolution.Status);
        Assert.Equal(2, resolution.Attempt);
        // The damaged unit stays bound to that pending decision, and the healthy returned capacity was never touched.
        Assert.Equal(0m, fixture.Unit.OutstandingDamagedQuantity(fixture.ShipmentId, fixture.LineA));
        Assert.Equal(2m, fixture.Unit.ResendableQuantity(fixture.LineA));
        // A retry rebinds the SAME retained allocation instead of reserving a second unit.
        var retried = fixture.Unit.CreateReplacementShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), resolution.DamagedGoodsResolutionId, Actor, string.Empty, T6);
        Assert.Equal(reservationId, retried.ReplacementReservationId);
        Assert.Equal(3, fixture.Unit.Shipments.Count);

        // Only cancelling the owning ORDER releases the unconsumed hold, exactly once.
        var released = fixture.Unit.ReleaseReplacementStockForOrderCancel(T6.AddMinutes(1));
        Assert.Equal(new[] { reservationId }, released);
        Assert.Null(resolution.ReplacementReservationId);
        Assert.Equal(DamagedResolutionStatus.Cancelled, resolution.Status);
        Assert.Empty(fixture.Unit.ReleaseReplacementStockForOrderCancel(T6.AddMinutes(2)));
    }

    /// <summary>
    /// TB-262-R1 (B): the parcel-cancellation retention is EXACT association work. Replaying the cancellation of an
    /// OLD parcel after a retry created its new linked parcel must not detach the new link, advance the attempt or
    /// free the held allocation — otherwise a stale request could mint a second parcel against the same hold.
    /// </summary>
    [Fact]
    public void A_stale_cancel_replay_never_detaches_the_new_replacement_parcel()
    {
        var fixture = ReturnedParcel(usableA: 2m, damagedA: 1m);
        var resolution = fixture.Unit.RecordDamagedResolution(
            Guid.NewGuid(), fixture.ShipmentId, fixture.LineA, 1m, DamagedResolutionOutcome.Replacement,
            "occ-stale", Actor, null, T4);
        var reservationId = Guid.NewGuid();
        fixture.Unit.AttachDamagedResolutionReservation(resolution.DamagedGoodsResolutionId, reservationId, T5);

        var first = fixture.Unit.CreateReplacementShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), resolution.DamagedGoodsResolutionId, Actor, string.Empty, T5);
        fixture.Unit.CancelShipment(first.ShipmentId, T5.AddMinutes(1));
        fixture.Unit.RetainReplacementStockForCancelledParcel(first.ShipmentId, T5.AddMinutes(1));
        Assert.Equal(2, resolution.Attempt);

        var second = fixture.Unit.CreateReplacementShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), resolution.DamagedGoodsResolutionId, Actor, string.Empty, T6);
        Assert.NotEqual(first.ShipmentId, second.ShipmentId);
        Assert.Equal(second.ShipmentId, resolution.ReplacementShipmentId);
        Assert.Equal(DamagedResolutionStatus.Fulfilled, resolution.Status);

        // A replayed cancellation of the OLD (already cancelled) parcel is a no-op — twice, and after the retry.
        fixture.Unit.RetainReplacementStockForCancelledParcel(first.ShipmentId, T6.AddMinutes(1));
        fixture.Unit.RetainReplacementStockForCancelledParcel(first.ShipmentId, T6.AddMinutes(2));

        Assert.Equal(second.ShipmentId, resolution.ReplacementShipmentId);
        Assert.Equal(2, resolution.Attempt);
        Assert.Equal(DamagedResolutionStatus.Fulfilled, resolution.Status);
        Assert.Equal(reservationId, resolution.ReplacementReservationId);
        // Exactly ONE live replacement parcel owns the held units; the stale replay created nothing.
        Assert.Single(fixture.Unit.Shipments.Where(x => x.IsReplacement && x.Status != ShipmentStatus.Cancelled));

        // Cancelling the CURRENT parcel is still the one valid detach, and it advances the attempt exactly once.
        fixture.Unit.CancelShipment(second.ShipmentId, T6.AddMinutes(3));
        fixture.Unit.RetainReplacementStockForCancelledParcel(second.ShipmentId, T6.AddMinutes(3));
        Assert.Null(resolution.ReplacementShipmentId);
        Assert.Equal(3, resolution.Attempt);
        Assert.Equal(DamagedResolutionStatus.Reserved, resolution.Status);
        Assert.Equal(reservationId, resolution.ReplacementReservationId);
        Assert.True(resolution.AwaitsReplacementParcel);
    }
}
