using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-258-R1 (pure domain): the four source-review defects of the post-dispatch return/resend path.
/// (A) a resend occurrence is a prebound, validated intent replayed by its owning parcel before any capacity gate;
/// (B) capacity belongs to the physically received SOURCE parcel and keeps immediate lineage through cycles;
/// (C) a single parcel line may come back split across conditions (usable + damaged).
/// </summary>
public sealed class PostDispatchException258R1Tests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);
    private static readonly DateTimeOffset T2 = T0.AddHours(2);
    private static readonly DateTimeOffset T3 = T0.AddHours(3);
    private static readonly DateTimeOffset T4 = T0.AddHours(4);
    private static readonly Guid Actor = Guid.Parse("018f2581-0000-7000-8000-0000000000b1");
    private static readonly Guid OtherActor = Guid.Parse("018f2581-0000-7000-8000-0000000000b2");

    /// <summary>One line of 2 ordered/packed units split into TWO independently dispatched parcels of 1 unit each.</summary>
    private sealed record TwoParcelFixture(
        FulfillmentUnit Unit,
        Guid ShippingGroupId,
        Guid LineA,
        Guid FirstParcelId,
        Guid SecondParcelId);

    private static TwoParcelFixture TwoParcelsOneLine()
    {
        var lineA = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sellerPartyId,
            Guid.NewGuid(),
            "گیرندهٔ آزمون ۲۵۸-آر۱",
            "09120000001",
            "تهران",
            "تهران",
            "نشانی آزمون تسک ۲۵۸-آر۱",
            "1234567891",
            "post",
            "پیشتاز",
            [(lineA, 2m, (Guid?)null)],
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
                    [lineA]),
            ]);

        unit.MarkProcessing(T0);
        unit.MarkPacked(T0);
        var first = unit.CreateShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            "پست",
            [(lineA, 1m)],
            T0,
            "post",
            "پیشتاز",
            shippingGroupId: groupId);
        var second = unit.CreateShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            "پست",
            [(lineA, 1m)],
            T0,
            "post",
            "پیشتاز",
            shippingGroupId: groupId);
        unit.AssignTracking(first.ShipmentId, "TRK-TB258R1-1", T0);
        unit.AssignTracking(second.ShipmentId, "TRK-TB258R1-2", T0);
        unit.ApplyShipmentDispatched(first.ShipmentId, T0);
        unit.ApplyShipmentDispatched(second.ShipmentId, T0);
        return new TwoParcelFixture(unit, groupId, lineA, first.ShipmentId, second.ShipmentId);
    }

    private static Shipment Parcel(FulfillmentUnit unit, Guid shipmentId) =>
        unit.Shipments.Single(x => x.ShipmentId == shipmentId);

    private static void ReturnParcel(
        FulfillmentUnit unit,
        Guid shipmentId,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, ReturnReceiptCondition Condition)> items)
    {
        unit.RecordShipmentNonDelivery(shipmentId, "customer_unavailable", null, Actor, T1);
        unit.StartShipmentReturn(shipmentId, Actor, T2);
        unit.RecordReturnReceipt(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            shipmentId,
            Actor,
            items,
            T3,
            null,
            T3);
    }

    private static Shipment Resend(
        FulfillmentUnit unit,
        Guid sourceShipmentId,
        string key,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items,
        Guid? actor = null) =>
        unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            sourceShipmentId,
            actor ?? Actor,
            string.Empty,
            items,
            T4,
            null,
            null,
            key);

    // ------------------------------------------------------------------ A: occurrence intent

    [Fact]
    public void The_same_occurrence_and_intent_replays_the_recorded_parcel_even_after_capacity_is_consumed()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);

        var first = Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-occ-A", [(fixture.LineA, 1m)]);
        // The source capacity is now fully committed: a fresh request would be refused, but the SAME occurrence is
        // the recorded intent and must still answer with its exact parcel.
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));
        var replay = Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-occ-A", [(fixture.LineA, 1m)]);

        Assert.Equal(first.ShipmentId, replay.ShipmentId);
        Assert.Single(fixture.Unit.Shipments, x => x.IsResend);
        Assert.Equal(Actor, first.CreatedByUserId);
    }

    [Fact]
    public void A_changed_quantity_or_source_under_a_recorded_occurrence_is_a_typed_conflict_with_no_new_rows()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        ReturnParcel(fixture.Unit, fixture.SecondParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-occ-B", [(fixture.LineA, 1m)]);

        var changedQuantity = Assert.Throws<ContractOperationException>(() =>
            Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-occ-B", [(fixture.LineA, 0.5m)]));
        Assert.Equal("fulfillment.resend.intent_conflict", changedQuantity.Code);

        // Same key, different source parcel: the recorded parcel belongs to the first intent, never this one.
        var changedSource = Assert.Throws<ContractOperationException>(() =>
            Resend(fixture.Unit, fixture.SecondParcelId, "tb258r1-occ-B", [(fixture.LineA, 1m)]));
        Assert.Equal("fulfillment.resend.intent_conflict", changedSource.Code);

        Assert.Single(fixture.Unit.Shipments, x => x.IsResend);
        // The second parcel's own capacity is untouched by the conflicting attempts.
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.SecondParcelId, fixture.LineA));
    }

    [Fact]
    public void A_blank_occurrence_is_a_typed_refusal_and_never_a_non_idempotent_fallback()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);

        var missing = Assert.Throws<ContractOperationException>(() =>
            Resend(fixture.Unit, fixture.FirstParcelId, "   ", [(fixture.LineA, 1m)]));
        Assert.Equal("fulfillment.resend.occurrence_required", missing.Code);
        Assert.Empty(fixture.Unit.Shipments.Where(x => x.IsResend));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));
    }

    [Fact]
    public void A_cancelled_occurrence_keeps_its_history_and_is_never_silently_recreated_by_the_same_key()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        var first = Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-occ-C", [(fixture.LineA, 1m)]);
        fixture.Unit.CancelShipment(first.ShipmentId, T4);

        var retry = Assert.Throws<ContractOperationException>(() =>
            Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-occ-C", [(fixture.LineA, 1m)]));
        Assert.Equal("fulfillment.resend.occurrence_cancelled", retry.Code);

        // The cancelled record is still readable history, and a genuinely new occurrence is lawful.
        var cancelled = Parcel(fixture.Unit, first.ShipmentId);
        Assert.Equal(ShipmentStatus.Cancelled, cancelled.Status);        Assert.Equal("tb258r1-occ-C", cancelled.ShippingDispatchKey);
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));

        var replacement = Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-occ-C2", [(fixture.LineA, 1m)]);
        Assert.NotEqual(first.ShipmentId, replacement.ShipmentId);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));
    }

    // ------------------------------------------------------------------ B: per-source capacity

    [Fact]
    public void Two_returned_parcels_of_the_same_line_cannot_lend_each_other_capacity()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        ReturnParcel(fixture.Unit, fixture.SecondParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        // 2 usable units exist in the unit, but only 1 belongs to the first parcel.
        Assert.Equal(2m, fixture.Unit.ResendableQuantity(fixture.LineA));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));

        var borrow = Assert.Throws<ContractOperationException>(() =>
            Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-src-A", [(fixture.LineA, 2m)]));
        Assert.Equal("fulfillment.resend.exceeds_returned", borrow.Code);

        Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-src-B", [(fixture.LineA, 1m)]);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.SecondParcelId, fixture.LineA));

        Resend(fixture.Unit, fixture.SecondParcelId, "tb258r1-src-C", [(fixture.LineA, 1m)]);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));
        Assert.Equal(2, fixture.Unit.Shipments.Count(x => x.IsResend));
    }

    [Fact]
    public void A_returned_resend_funds_exactly_one_next_cycle_and_never_a_second_live_branch()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        var r1 = Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-cyc-1", [(fixture.LineA, 1m)]);
        fixture.Unit.AssignTracking(r1.ShipmentId, "TRK-TB258R1-R1", T4);
        fixture.Unit.ApplyShipmentDispatched(r1.ShipmentId, T4);
        // The customer never received it, so it physically comes back and funds one more cycle.
        ReturnParcel(fixture.Unit, r1.ShipmentId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(r1.ShipmentId, fixture.LineA));
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));

        var r2 = Resend(fixture.Unit, r1.ShipmentId, "tb258r1-cyc-2", [(fixture.LineA, 1m)]);
        Assert.Equal(r1.ShipmentId, r2.ResendOfShipmentId);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(r1.ShipmentId, fixture.LineA));

        // The first receipt is fully represented by the live chain: it cannot fund another branch.
        var extra = Assert.Throws<ContractOperationException>(() =>
            Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-cyc-3", [(fixture.LineA, 1m)]));
        Assert.Equal("fulfillment.resend.exceeds_returned", extra.Code);
        // Customer delivery is still counted once per delivered parcel only.
        Assert.Equal(0m, fixture.Unit.DeliveredQuantity(fixture.LineA));
        Assert.NotEqual(FulfillmentStatus.Delivered, fixture.Unit.Status);
    }

    [Fact]
    public void Cancelling_a_resend_releases_only_its_own_source_allocation()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        ReturnParcel(fixture.Unit, fixture.SecondParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)]);
        var resend = Resend(fixture.Unit, fixture.FirstParcelId, "tb258r1-can-A", [(fixture.LineA, 1m)]);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.SecondParcelId, fixture.LineA));

        fixture.Unit.CancelShipment(resend.ShipmentId, T4);
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));
        // The sibling source was never touched by the cancelled allocation.
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.SecondParcelId, fixture.LineA));
    }

    [Fact]
    public void Damaged_or_in_transit_goods_grant_zero_capacity()
    {
        var fixture = TwoParcelsOneLine();
        ReturnParcel(fixture.Unit, fixture.FirstParcelId, [(fixture.LineA, 1m, ReturnReceiptCondition.Damaged)]);
        Assert.Equal(0m, fixture.Unit.ReturnedUsableQuantity(fixture.FirstParcelId, fixture.LineA));
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.FirstParcelId, fixture.LineA));
        // The second parcel is still in transit: a non-delivery alone is not a receipt.
        fixture.Unit.RecordShipmentNonDelivery(fixture.SecondParcelId, "refused", null, Actor, T1);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.SecondParcelId, fixture.LineA));
    }

    // ------------------------------------------------------------------ C: mixed-condition receipt

    [Fact]
    public void One_parcel_line_can_be_received_as_two_usable_plus_one_damaged_and_grants_only_two()
    {
        var lineA = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sellerPartyId,
            Guid.NewGuid(),
            "گیرندهٔ آزمون ۲۵۸-آر۱-سی",
            "09120000002",
            "تهران",
            "تهران",
            "نشانی آزمون تسه ۲۵۸-آر۱-سی",
            "1234567892",
            "post",
            "پیشتاز",
            [(lineA, 3m, (Guid?)null)],
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
                    [lineA]),
            ]);
        unit.MarkProcessing(T0);
        unit.MarkPacked(T0);
        var parcel = unit.CreateShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            "پست",
            [(lineA, 3m)],
            T0,
            "post",
            "پیشتاز",
            shippingGroupId: groupId);
        unit.AssignTracking(parcel.ShipmentId, "TRK-TB258R1-3", T0);
        unit.ApplyShipmentDispatched(parcel.ShipmentId, T0);

        ReturnParcel(
            unit,
            parcel.ShipmentId,
            [
                (lineA, 2m, ReturnReceiptCondition.Usable),
                (lineA, 1m, ReturnReceiptCondition.Damaged),
            ]);

        var receipt = unit.ReturnReceipts.Single(r => r.ShipmentId == parcel.ShipmentId);
        Assert.Equal(2, receipt.Items.Count);
        Assert.Equal(2m, unit.ReturnedUsableQuantity(parcel.ShipmentId, lineA));
        Assert.Equal(2m, unit.ResendableQuantity(parcel.ShipmentId, lineA));
        Assert.Equal(Actor, receipt.ReceivedByUserId);
        // The physical receipt is one final record: a second, changed receipt is refused.
        var second = Assert.Throws<ContractOperationException>(() => unit.RecordReturnReceipt(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            parcel.ShipmentId,
            Actor,
            [(lineA, 3m, ReturnReceiptCondition.Usable)],
            T4,
            null,
            T4));
        Assert.Equal("fulfillment.return.receipt_already_recorded", second.Code);
        Assert.Equal(2m, unit.ResendableQuantity(parcel.ShipmentId, lineA));
    }

    // ------------------------------------------------------------------ D: durable operator audit

    [Fact]
    public void The_operator_identity_of_each_post_dispatch_fact_is_durable_on_the_owning_parcel()
    {
        var fixture = TwoParcelsOneLine();
        fixture.Unit.RecordShipmentNonDelivery(fixture.FirstParcelId, "refused", null, Actor, T1);
        fixture.Unit.StartShipmentReturn(fixture.FirstParcelId, OtherActor, T2);
        fixture.Unit.RecordReturnReceipt(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.FirstParcelId,
            Actor,
            [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)],
            T3,
            null,
            T3);

        var parcel = Parcel(fixture.Unit, fixture.FirstParcelId);
        Assert.Equal(Actor, parcel.NonDeliveredByUserId);
        Assert.Equal(OtherActor, parcel.ReturnStartedByUserId);
        Assert.Equal(Actor, fixture.Unit.ReturnReceipts.Single().ReceivedByUserId);
    }

    [Fact]
    public void An_anonymous_post_dispatch_fact_is_refused()
    {
        var fixture = TwoParcelsOneLine();
        var noActor = Assert.Throws<ContractOperationException>(() =>
            fixture.Unit.RecordShipmentNonDelivery(fixture.FirstParcelId, "refused", null, Guid.Empty, T1));
        Assert.Equal("fulfillment.nondelivery.actor_required", noActor.Code);
        Assert.Equal(ShipmentStatus.Dispatched, Parcel(fixture.Unit, fixture.FirstParcelId).Status);
    }
}
