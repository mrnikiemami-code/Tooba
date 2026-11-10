using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-258 (pure domain): the post-dispatch exception lifecycle — non-delivery, physical return, origin receipt and
/// the quantity-safe resend allocation. These are the invariants that make a courier return representable WITHOUT
/// a fake delivery, a pre-dispatch cancel, or a second consumption of the original packing/reservation.
/// </summary>
/// <remarks>
/// The fixture models one paid order of two lines that physically left the warehouse in one parcel, plus the exact
/// quantities the customer paid for. Nothing here touches storage; the real relational proof lives in
/// <c>PostDispatchExceptionRelational258Tests</c>.
/// </remarks>
public sealed class PostDispatchException258Tests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);
    private static readonly DateTimeOffset T2 = T0.AddHours(2);
    private static readonly DateTimeOffset T3 = T0.AddHours(3);
    private static readonly DateTimeOffset T4 = T0.AddHours(4);
    private static readonly Guid Actor = Guid.Parse("018f2580-0000-7000-8000-0000000000a1");

    private sealed record Fixture(
        FulfillmentUnit Unit,
        Guid ShippingGroupId,
        Guid LineA,
        Guid LineB,
        Guid ShipmentId);

    /// <summary>One dispatched parcel carrying 2 units of A and 1 unit of B, inside the accepted shipping group.</summary>
    private static Fixture DispatchedParcel()
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
            "گیرندهٔ آزمون ۲۵۸",
            "09120000000",
            "تهران",
            "تهران",
            "نشانی آزمون تسک ۲۵۸",
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
            [(lineA, 2m), (lineB, 1m)],
            T0,
            "post",
            "پیشتاز",
            shippingGroupId: groupId);
        unit.AssignTracking(shipment.ShipmentId, "TRK-TB258-1", T0);
        unit.ApplyShipmentDispatched(shipment.ShipmentId, T0);
        return new Fixture(unit, groupId, lineA, lineB, shipment.ShipmentId);
    }

    private static Shipment Parcel(Fixture fixture, Guid shipmentId) =>
        fixture.Unit.Shipments.Single(x => x.ShipmentId == shipmentId);

    // ---------------------------------------------------------------- A: representable physical return

    [Fact]
    public void Non_delivery_is_recorded_without_pretending_delivery_or_cancel_and_without_resend_capacity()
    {
        var fixture = DispatchedParcel();
        var original = Parcel(fixture, fixture.ShipmentId);
        var shippedBefore = fixture.Unit.Items.Single(x => x.OrderLineId == fixture.LineA).QuantityShipped;
        Assert.Equal(2m, shippedBefore);

        fixture.Unit.RecordShipmentNonDelivery(
            fixture.ShipmentId,
            "customer_unavailable",
            "مشتری پاسخ نداد", Actor, T1);

        var parcel = Parcel(fixture, fixture.ShipmentId);
        Assert.Equal(ShipmentStatus.NonDelivered, parcel.Status);
        Assert.Null(parcel.DeliveredAt);
        Assert.Equal(T0, parcel.DispatchedAt);
        Assert.Equal("TRK-TB258-1", parcel.TrackingReference);
        Assert.Equal("customer_unavailable", parcel.NonDeliveryReason);
        Assert.Equal(T1, parcel.NonDeliveredAt);
        // The goods physically left the warehouse: the shipped side of every quantity reader is untouched.
        Assert.Equal(2m, fixture.Unit.Items.Single(x => x.OrderLineId == fixture.LineA).QuantityShipped);
        // A non-delivery alone is NOT a receipt: zero resend capacity exists yet.
        Assert.Equal(0m, fixture.Unit.ReturnedUsableQuantity(fixture.LineA));
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));
        Assert.False(fixture.Unit.HasResendableQuantity());
        Assert.NotEqual(FulfillmentStatus.Delivered, fixture.Unit.Status);
        Assert.Equal(original.ShipmentId, parcel.ShipmentId);
    }

    [Fact]
    public void Non_delivery_replay_is_idempotent_but_a_changed_reason_is_a_conflict()
    {
        var fixture = DispatchedParcel();
        fixture.Unit.RecordShipmentNonDelivery(fixture.ShipmentId, "refused", null, Actor,
            T1);
        fixture.Unit.RecordShipmentNonDelivery(fixture.ShipmentId, "refused", null, Actor,
            T2);
        Assert.Equal(T1, Parcel(fixture, fixture.ShipmentId).NonDeliveredAt);

        var conflict = Assert.Throws<ContractOperationException>(() =>
            fixture.Unit.RecordShipmentNonDelivery(fixture.ShipmentId, "address_not_found", null, Actor,
            T3));
        Assert.Equal("fulfillment.nondelivery.conflict", conflict.Code);
    }

    [Fact]
    public void A_genuine_later_delivery_may_resolve_non_delivery_but_never_a_returning_parcel()
    {
        var fixture = DispatchedParcel();
        fixture.Unit.RecordShipmentNonDelivery(fixture.ShipmentId, "customer_unavailable", null, Actor,
            T1);
        fixture.Unit.StartShipmentReturn(fixture.ShipmentId, Actor,
            T2);

        var refused = Assert.Throws<ContractOperationException>(() =>
            fixture.Unit.ApplyShipmentDelivered(fixture.ShipmentId, T3));
        Assert.Equal("fulfillment.deliver.invalid_status", refused.Code);
        Assert.Null(Parcel(fixture, fixture.ShipmentId).DeliveredAt);
    }

    // ---------------------------------------------------------------- B: illegal transitions

    [Fact]
    public void Delivered_parcel_can_never_be_marked_undelivered_and_created_parcel_cannot_skip_dispatch()
    {
        var created = DispatchedParcel();
        var preDispatchShipment = created.Unit.CreateShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            "پست",
            [(created.LineA, 1m)],
            T1,
            "post",
            "پیشتاز",
            shippingGroupId: created.ShippingGroupId);
        var preDispatch = Assert.Throws<ContractOperationException>(() =>
            created.Unit.RecordShipmentNonDelivery(preDispatchShipment.ShipmentId, "other", null, Actor,
            T1));
        Assert.Equal("fulfillment.nondelivery.invalid_status", preDispatch.Code);

        var delivered = DispatchedParcel();
        delivered.Unit.ApplyShipmentDelivered(delivered.ShipmentId, T2);
        Assert.Equal(ShipmentStatus.Delivered, Parcel(delivered, delivered.ShipmentId).Status);
        Assert.Equal(2m, delivered.Unit.DeliveredQuantity(delivered.LineA));

        var afterDelivered = Assert.Throws<ContractOperationException>(() =>
            delivered.Unit.RecordShipmentNonDelivery(delivered.ShipmentId, "other", null, Actor,
            T3));
        Assert.Equal("fulfillment.nondelivery.after_delivered", afterDelivered.Code);
        Assert.Equal(ShipmentStatus.Delivered, Parcel(delivered, delivered.ShipmentId).Status);
        Assert.NotNull(Parcel(delivered, delivered.ShipmentId).DeliveredAt);

        var cannotCancel = Assert.Throws<ContractOperationException>(() =>
            delivered.Unit.CancelShipment(delivered.ShipmentId, T4));
        Assert.Equal("fulfillment.shipment.cancel_invalid", cannotCancel.Code);
    }

    [Fact]
    public void Return_can_only_start_after_nondelivery_and_receipt_only_while_returning()
    {
        var fixture = DispatchedParcel();
        var tooEarly = Assert.Throws<ContractOperationException>(() =>
            fixture.Unit.StartShipmentReturn(fixture.ShipmentId, Actor,
            T1));
        Assert.Equal("fulfillment.return.invalid_status", tooEarly.Code);

        var receiptTooEarly = Assert.Throws<ContractOperationException>(() =>
            Receipt(fixture.Unit, fixture.ShipmentId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)], T1));
        Assert.Equal("fulfillment.return.receipt_invalid_status", receiptTooEarly.Code);

        fixture.Unit.RecordShipmentNonDelivery(fixture.ShipmentId, "refused", null, Actor,
            T1);
        fixture.Unit.StartShipmentReturn(fixture.ShipmentId, Actor,
            T2);
        Assert.Equal(ShipmentStatus.Returning, Parcel(fixture, fixture.ShipmentId).Status);
        Assert.Equal(T2, Parcel(fixture, fixture.ShipmentId).ReturnStartedAt);
        // Returning still grants nothing: the goods have not physically arrived.
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));
    }

    // ---------------------------------------------------------------- C: exact capacity

    [Fact]
    public void Origin_receipt_makes_only_usable_units_resendable_and_rejects_excess_or_foreign_lines()
    {
        var fixture = DispatchedParcel();
        fixture.Unit.RecordShipmentNonDelivery(fixture.ShipmentId, "refused", null, Actor,
            T1);
        fixture.Unit.StartShipmentReturn(fixture.ShipmentId, Actor,
            T2);

        var exceeds = Assert.Throws<ContractOperationException>(() =>
            Receipt(fixture.Unit, fixture.ShipmentId, [(fixture.LineA, 3m, ReturnReceiptCondition.Usable)], T3));
        Assert.Equal("fulfillment.return.receipt_qty_exceeds", exceeds.Code);

        var foreign = Assert.Throws<ContractOperationException>(() =>
            Receipt(fixture.Unit, fixture.ShipmentId, [(Guid.NewGuid(), 1m, ReturnReceiptCondition.Usable)], T3));
        Assert.Equal("fulfillment.return.receipt_line_foreign", foreign.Code);

        // TB-258-R1: the duplicate authority is (line, condition); the SAME line+condition twice is refused.
        var duplicate = Assert.Throws<ContractOperationException>(() =>
            Receipt(
                fixture.Unit,
                fixture.ShipmentId,
                [(fixture.LineA, 1m, ReturnReceiptCondition.Usable), (fixture.LineA, 1m, ReturnReceiptCondition.Usable)],
                T3));
        Assert.Equal("fulfillment.return.receipt_line_duplicate", duplicate.Code);

        // A mixed-condition receipt that EXCEEDS what this parcel carried is refused as a whole.
        var mixedExcess = Assert.Throws<ContractOperationException>(() =>
            Receipt(
                fixture.Unit,
                fixture.ShipmentId,
                [(fixture.LineA, 2m, ReturnReceiptCondition.Usable), (fixture.LineA, 1m, ReturnReceiptCondition.Damaged)],
                T3));
        Assert.Equal("fulfillment.return.receipt_qty_exceeds", mixedExcess.Code);

        var zero = Assert.Throws<ContractOperationException>(() =>
            Receipt(fixture.Unit, fixture.ShipmentId, [(fixture.LineA, 0m, ReturnReceiptCondition.Usable)], T3));
        Assert.Equal("fulfillment.return.receipt_qty_positive", zero.Code);

        // Nothing above created capacity: a refused receipt is not a receipt.
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));

        Receipt(
            fixture.Unit,
            fixture.ShipmentId,
            [(fixture.LineA, 1m, ReturnReceiptCondition.Usable), (fixture.LineB, 1m, ReturnReceiptCondition.Damaged)],
            T3);

        Assert.Equal(ShipmentStatus.Returned, Parcel(fixture, fixture.ShipmentId).Status);
        Assert.Equal(T3, Parcel(fixture, fixture.ShipmentId).ReturnedAt);
        Assert.Equal(1m, fixture.Unit.ReturnedUsableQuantity(fixture.LineA));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.LineA));
        // Damaged goods are recorded but never become resend capacity.
        Assert.Equal(0m, fixture.Unit.ReturnedUsableQuantity(fixture.LineB));
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineB));
        Assert.True(fixture.Unit.HasResendableQuantity());

        var secondReceipt = Assert.Throws<ContractOperationException>(() =>
            Receipt(fixture.Unit, fixture.ShipmentId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)], T4));
        Assert.Equal("fulfillment.return.receipt_already_recorded", secondReceipt.Code);
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.LineA));
    }

    // ---------------------------------------------------------------- A/E: resend conservation

    [Fact]
    public void Resend_creates_a_new_linked_parcel_that_consumes_capacity_once_and_preserves_the_original()
    {
        var fixture = ReturnedWithUsableCapacity();
        var original = Parcel(fixture, fixture.ShipmentId);
        var packedBefore = fixture.Unit.Items.Single(x => x.OrderLineId == fixture.LineA).QuantityPacked;

        var resend = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-A");

        Assert.True(resend.IsResend);
        Assert.Equal(fixture.ShipmentId, resend.ResendOfShipmentId);
        Assert.Equal(ShipmentStatus.Created, resend.Status);
        Assert.Equal(fixture.ShippingGroupId, resend.ShippingGroupId);
        Assert.Equal(original.ShippingMethodCode, resend.ShippingMethodCode);
        // The original parcel keeps its dispatch/tracking/return history verbatim.
        Assert.Equal(ShipmentStatus.Returned, original.Status);
        Assert.Equal(T0, original.DispatchedAt);
        Assert.Equal("TRK-TB258-1", original.TrackingReference);

        // Capacity is consumed exactly once, and the fresh packed remainder is untouched.
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));
        Assert.Equal(packedBefore, fixture.Unit.Items.Single(x => x.OrderLineId == fixture.LineA).QuantityPacked);
        // A Created resend is funded by returned capacity, so it never looks like an open first allocation.
        Assert.Equal(0m, fixture.Unit.OpenAllocatedQuantity(fixture.LineA));

        var over = Assert.Throws<ContractOperationException>(() => fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-B"));
        Assert.Equal("fulfillment.resend.exceeds_returned", over.Code);

        var foreign = Assert.Throws<ContractOperationException>(() => fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(Guid.NewGuid(), 1m)],
            T4,
            null,
            null,
            "tb258-resend-C"));
        // A line that is not part of this fulfillment is refused before any parcel row exists.
        Assert.Equal("fulfillment.order_line.not_found", foreign.Code);
        Assert.DoesNotContain(fixture.Unit.Shipments, x => x.IsResend && x.ShippingDispatchKey == "tb258-resend-C");
    }
    [Fact]
    public void Resend_dispatch_never_inflates_lifetime_shipped_and_delivery_counts_once()
    {
        var fixture = ReturnedWithUsableCapacity();
        var resend = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-D");

        fixture.Unit.AssignTracking(resend.ShipmentId, "TRK-TB258-R1", T4);
        fixture.Unit.ApplyShipmentDispatched(resend.ShipmentId, T4);
        fixture.Unit.ApplyShipmentDelivered(resend.ShipmentId, T4);

        // Lifetime shipped stays what originally left the warehouse — the resend moved returned goods, not new ones.
        Assert.Equal(2m, fixture.Unit.Items.Single(x => x.OrderLineId == fixture.LineA).QuantityShipped);
        Assert.Equal(1m, fixture.Unit.DeliveredQuantity(fixture.LineA));
        // Only 1 of the 2 ordered units reached the customer, so the unit is NOT delivered.
        Assert.NotEqual(FulfillmentStatus.Delivered, fixture.Unit.Status);
    }

    [Fact]
    public void Cancelling_a_pre_dispatch_resend_releases_its_returned_capacity_once()
    {
        var fixture = ReturnedWithUsableCapacity();
        var resend = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-E");
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));

        fixture.Unit.CancelShipment(resend.ShipmentId, T4);
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.LineA));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.LineA));
        // The original parcel is still readable and still returned.
        Assert.Equal(ShipmentStatus.Returned, Parcel(fixture, fixture.ShipmentId).Status);

        var replay = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-F");
        Assert.Equal(ShipmentStatus.Created, replay.Status);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));
    }

    [Fact]
    public void A_resend_of_a_resend_is_judged_against_the_root_receipt_capacity()
    {
        var fixture = ReturnedWithUsableCapacity();
        var firstResend = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-G");
        fixture.Unit.AssignTracking(firstResend.ShipmentId, "TRK-TB258-R2", T4);
        fixture.Unit.ApplyShipmentDispatched(firstResend.ShipmentId, T4);
        fixture.Unit.RecordShipmentNonDelivery(firstResend.ShipmentId, "refused", null, Actor,
            T4);
        fixture.Unit.StartShipmentReturn(firstResend.ShipmentId, Actor,
            T4);
        Receipt(fixture.Unit, firstResend.ShipmentId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)], T4);

        // The same physical unit came back a second time, so exactly one usable unit is in hand again.
        Assert.Equal(2m, fixture.Unit.ReturnedUsableQuantity(fixture.LineA));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(fixture.LineA));
        // TB-258-R1: the FIRST source is fully committed by the live resend, so it can never fund a second branch.
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.ShipmentId, fixture.LineA));
        Assert.Equal(1m, fixture.Unit.ResendableQuantity(firstResend.ShipmentId, fixture.LineA));

        var secondResend = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            firstResend.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-H");
        // TB-258-R1: immediate physical lineage is preserved — the next resend links the parcel that physically came
        // back, never an ancestor it cannot draw capacity from.
        Assert.Equal(firstResend.ShipmentId, secondResend.ResendOfShipmentId);
        Assert.Equal(fixture.ShippingGroupId, secondResend.ShippingGroupId);
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(firstResend.ShipmentId, fixture.LineA));
        Assert.Equal(0m, fixture.Unit.ResendableQuantity(fixture.LineA));
        var blocked = Assert.Throws<ContractOperationException>(() => fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-I"));
        Assert.Equal("fulfillment.resend.exceeds_returned", blocked.Code);
    }

    // ---------------------------------------------------------------- D: retry identity

    [Fact]
    public void The_same_dispatch_key_replays_the_same_resend_parcel()
    {
        var fixture = ReturnedWithUsableCapacity();
        var first = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-retry");
        var replay = fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T4,
            null,
            null,
            "tb258-resend-retry");

        Assert.Equal(first.ShipmentId, replay.ShipmentId);
        Assert.Single(fixture.Unit.Shipments, x => x.IsResend);
    }

    [Fact]
    public void A_resend_cannot_be_created_before_the_original_is_physically_returned()
    {
        var fixture = DispatchedParcel();
        var early = Assert.Throws<ContractOperationException>(() => fixture.Unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            fixture.ShipmentId,
            Actor,
            string.Empty,
            [(fixture.LineA, 1m)],
            T1,
            null,
            null,
            "tb258-resend-early"));
        Assert.Equal("fulfillment.resend.original_not_returned", early.Code);
        Assert.Empty(fixture.Unit.Shipments.Where(x => x.IsResend));
    }

    // ---------------------------------------------------------------- E: aggregate delivery truth

    [Fact]
    public void One_other_in_transit_parcel_keeps_the_unit_undelivered()
    {
        var fixture = DispatchedParcel();
        // A second, smaller parcel of the same group is still in transit (one remaining packed unit of line A).
        var second = fixture.Unit.CreateShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            "پست",
            [(fixture.LineA, 1m)],
            T1,
            "post",
            "پیشتاز",
            shippingGroupId: fixture.ShippingGroupId);
        fixture.Unit.AssignTracking(second.ShipmentId, "TRK-TB258-2", T1);
        fixture.Unit.ApplyShipmentDispatched(second.ShipmentId, T1);

        fixture.Unit.ApplyShipmentDelivered(fixture.ShipmentId, T2);

        // DeliveredQuantity is real per-parcel truth, not "everything shipped was delivered".
        Assert.Equal(2m, fixture.Unit.DeliveredQuantity(fixture.LineA));
        Assert.NotEqual(FulfillmentStatus.Delivered, fixture.Unit.Status);
    }

    // ---- helpers -------------------------------------------------------------------------------

    private static Fixture ReturnedWithUsableCapacity()
    {
        var fixture = DispatchedParcel();
        fixture.Unit.RecordShipmentNonDelivery(fixture.ShipmentId, "customer_unavailable", null, Actor,
            T1);
        fixture.Unit.StartShipmentReturn(fixture.ShipmentId, Actor,
            T2);
        Receipt(fixture.Unit, fixture.ShipmentId, [(fixture.LineA, 1m, ReturnReceiptCondition.Usable)], T3);
        return fixture;
    }

    private static ShipmentReturnReceipt Receipt(
        FulfillmentUnit unit,
        Guid shipmentId,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, ReturnReceiptCondition Condition)> items,
        DateTimeOffset now) =>
        unit.RecordReturnReceipt(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            shipmentId,
            Guid.NewGuid(),
            items,
            now,
            null,
            now);
}
