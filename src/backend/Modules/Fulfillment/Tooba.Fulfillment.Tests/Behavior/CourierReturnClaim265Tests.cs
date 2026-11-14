using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-265: one returned parcel's physically received quantity is ONE pool. The claim is the durable guard that stops a
/// resend, a damaged replacement and a Returns case from funding the same units, and it is released only by the owning
/// terminal decision or by the approval settlement.
/// </summary>
public sealed class CourierReturnClaim265Tests
{
    private static readonly Guid Fulfillment = Guid.Parse("01999999-0000-7000-8000-000000000001");
    private static readonly Guid Shipment = Guid.Parse("01999999-0000-7000-8000-000000000002");
    private static readonly Guid Receipt = Guid.Parse("01999999-0000-7000-8000-000000000003");
    private static readonly Guid Actor = Guid.Parse("01999999-0000-7000-8000-000000000004");
    private static readonly Guid Line = Guid.Parse("01999999-0000-7000-8000-000000000005");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-13T10:00:00Z");

    private static CourierReturnClaim Claim(decimal quantity = 1m, CourierReturnClaimCondition condition = CourierReturnClaimCondition.Usable) =>
        CourierReturnClaim.Create(
            Guid.Parse("01999999-0000-7000-8000-0000000000aa"),
            Fulfillment,
            Shipment,
            Receipt,
            Actor,
            "tb265-occurrence-1",
            [new CourierReturnClaimLine(Line, quantity, condition)],
            Now);

    [Fact]
    public void Claim_is_active_and_guards_the_same_received_quantity()
    {
        var claim = Claim();

        Assert.True(claim.IsActive);
        Assert.Equal(CourierReturnClaimStatus.Requested, claim.Status);
        Assert.NotNull(claim.ActiveKey);
        Assert.True(claim.AnswersIntent(Fulfillment, Shipment, [new CourierReturnClaimLine(Line, 1m, CourierReturnClaimCondition.Usable)]));
        Assert.False(claim.AnswersIntent(Fulfillment, Shipment, [new CourierReturnClaimLine(Line, 2m, CourierReturnClaimCondition.Usable)]));
        Assert.False(claim.AnswersIntent(Fulfillment, Shipment, [new CourierReturnClaimLine(Line, 1m, CourierReturnClaimCondition.Damaged)]));
    }

    [Fact]
    public void Usable_and_damaged_are_separate_claims_of_the_same_line()
    {
        var usable = Claim(condition: CourierReturnClaimCondition.Usable);
        var damaged = Claim(condition: CourierReturnClaimCondition.Damaged);

        Assert.NotEqual(usable.ActiveKey, damaged.ActiveKey);
        Assert.False(usable.AnswersIntent(Fulfillment, Shipment, [new CourierReturnClaimLine(Line, 1m, CourierReturnClaimCondition.Damaged)]));
    }

    [Fact]
    public void Linking_the_case_is_exactly_once_and_a_different_case_is_refused()
    {
        var claim = Claim();
        var caseId = Guid.Parse("01999999-0000-7000-8000-0000000000bb");

        claim.LinkCase(caseId, Now);
        Assert.Equal(caseId, claim.ReturnRequestId);

        // Exact replay of the same link is a harmless no-op.
        claim.LinkCase(caseId, Now.AddMinutes(1));
        Assert.Equal(caseId, claim.ReturnRequestId);

        var other = Guid.Parse("01999999-0000-7000-8000-0000000000cc");
        Assert.Throws<ContractOperationException>(() => claim.LinkCase(other, Now));
        Assert.Equal(caseId, claim.ReturnRequestId);
    }

    [Fact]
    public void Reject_or_cancel_releases_only_this_branch_and_keeps_history()
    {
        var claim = Claim();
        claim.Release(CourierReturnClaimStatus.Rejected, Now);

        Assert.Equal(CourierReturnClaimStatus.Rejected, claim.Status);
        Assert.Null(claim.ActiveKey);
        Assert.False(claim.IsActive);

        // A replay of any terminal release is a no-op, so a retried rejection never re-activates the claim.
        claim.Release(CourierReturnClaimStatus.Cancelled, Now.AddMinutes(1));
        Assert.Equal(CourierReturnClaimStatus.Rejected, claim.Status);
        Assert.Null(claim.ActiveKey);

        // A rejected claim can never be re-linked or approved afterwards.
        Assert.Throws<ContractOperationException>(() => claim.LinkCase(Guid.NewGuid(), Now));
        Assert.Throws<ContractOperationException>(() => claim.MarkApproved(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Approval_settlement_is_final_and_records_the_released_quantity_once()
    {
        var claim = Claim();
        var caseId = Guid.Parse("01999999-0000-7000-8000-0000000000bb");
        claim.LinkCase(caseId, Now);

        claim.MarkApproved(caseId, Now);
        claim.MarkSettled(1m, Now);

        Assert.Equal(CourierReturnClaimStatus.Approved, claim.Status);
        Assert.Null(claim.ActiveKey);
        Assert.Equal(1m, claim.ReleasedHeldQuantity);
        Assert.NotNull(claim.SettledAt);

        // The durable settlement answer is stable, so a replay after a lost response cannot release twice.
        claim.MarkApproved(caseId, Now.AddMinutes(5));
        claim.MarkSettled(5m, Now.AddMinutes(5));
        Assert.Equal(1m, claim.ReleasedHeldQuantity);
        Assert.Equal(Now, claim.SettledAt);

        // An approved claim is terminal: it can never be released back into capacity.
        Assert.Throws<ContractOperationException>(() => claim.Release(CourierReturnClaimStatus.Rejected, Now));
    }

    [Fact]
    public void An_invalid_claim_intent_is_refused_before_anything_is_recorded()    {
        Assert.Throws<ContractOperationException>(() => Claim(quantity: 0m));
        Assert.Throws<ContractOperationException>(() => CourierReturnClaim.Create(
            Guid.Empty, Fulfillment, Shipment, Receipt, Actor, "key", [new CourierReturnClaimLine(Line, 1m, CourierReturnClaimCondition.Usable)], Now));
        Assert.Throws<ContractOperationException>(() => CourierReturnClaim.Create(
            Guid.NewGuid(), Fulfillment, Shipment, Receipt, Actor, "  ", [new CourierReturnClaimLine(Line, 1m, CourierReturnClaimCondition.Usable)], Now));
        Assert.Throws<ContractOperationException>(() => CourierReturnClaim.Create(
            Guid.NewGuid(), Fulfillment, Shipment, Receipt, Actor, "key",
            [new CourierReturnClaimLine(Line, 1m, CourierReturnClaimCondition.Usable), new CourierReturnClaimLine(Line, 1m, CourierReturnClaimCondition.Usable)],
            Now));
    }

    /// <summary>
    /// TB-265: the claim is not just a Returns-side bookkeeping row — the owning unit must stop reporting the claimed
    /// units as resend / damaged-decision capacity, otherwise the same returned parcel could fund a resend and a
    /// refund of the same physical units.
    /// </summary>
    [Fact]
    public void A_binding_claim_consumes_the_same_received_capacity_on_the_unit()
    {
        var line = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            Fulfillment,
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sellerPartyId,
            Guid.NewGuid(),
            "گیرندهٔ آزمون ۲۶۵",
            "09120000065",
            "تهران",
            "تهران",
            "نشانی آزمون تسک ۲۶۵",
            "1234567891",
            "post",
            "پیشتاز",
            [(line, 3m, (Guid?)null)],
            Now,
            [
                new FulfillmentShippingGroupPlan(
                    groupId,
                    sellerPartyId,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    DateOnly.FromDateTime(Now.UtcDateTime),
                    null,
                    null,
                    1,
                    [line]),
            ]);

        unit.MarkProcessing(Now);
        unit.MarkPacked(Now);
        var shipment = unit.CreateShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            "پست",
            [(line, 3m)],
            Now,
            "post",
            "پیشتاز",
            shippingGroupId: groupId);
        unit.AssignTracking(shipment.ShipmentId, "TRK-TB265-1", Now);
        unit.ApplyShipmentDispatched(shipment.ShipmentId, Now);
        unit.RecordShipmentNonDelivery(shipment.ShipmentId, "customer_unavailable", null, Actor, Now.AddHours(1));
        unit.StartShipmentReturn(shipment.ShipmentId, Actor, Now.AddHours(2));
        unit.RecordReturnReceipt(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            shipment.ShipmentId,
            Actor,
            [(line, 2m, ReturnReceiptCondition.Usable), (line, 1m, ReturnReceiptCondition.Damaged)],
            Now.AddHours(3),
            null,
            Now.AddHours(3));

        // Before any case exists the whole received quantity is lawful capacity.
        Assert.Equal(2m, unit.ResendableQuantity(shipment.ShipmentId, line));
        Assert.Equal(1m, unit.OutstandingDamagedQuantity(shipment.ShipmentId, line));

        // A case funding 1 healthy unit and the 1 damaged unit consumes exactly those units.
        unit.AttachLoadedReturnCapacityClaims(
        [
            new ReturnedCapacityClaim(shipment.ShipmentId, line, 1m, CourierReturnClaimCondition.Usable),
            new ReturnedCapacityClaim(shipment.ShipmentId, line, 1m, CourierReturnClaimCondition.Damaged),
        ]);

        Assert.Equal(1m, unit.ResendableQuantity(line));
        Assert.Equal(0m, unit.OutstandingDamagedQuantity(shipment.ShipmentId, line));

        // The claim never borrows from, or lends to, an unrelated line.
        Assert.Equal(0m, unit.ResendableQuantity(Guid.NewGuid(), line));
    }
}
