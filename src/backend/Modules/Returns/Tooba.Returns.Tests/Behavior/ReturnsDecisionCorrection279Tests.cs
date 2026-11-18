using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-279: the pure authorities of the audited correction, the per-item resolution, the delivery-slice allocation and
/// the shared refund budget. Every fact here reads ONLY stored-fact inputs, so no provider, database or HTTP path is
/// touched: the owning directory and the migration are proven separately.
/// </summary>
public sealed class ReturnsDecisionCorrection279Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SellerOrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CheckoutId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SellerPartyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PaymentId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    // ------------------------------------------------------------------ A: the reviewed resolution plan

    [Fact]
    public void A_partial_resolution_refunds_only_the_accepted_quantities()
    {
        // 2 requested, 2 received (1 usable + 1 damaged), operator accepts 1 and rejects 1.
        var snapshot = Snapshot(
            items: [Item(Guid.NewGuid(), quantity: 2m, unitPrice: 100m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 2m, usable: 1m, damaged: 1m));

        var plan = ReturnResolutionPolicy.Describe(
            ReturnOrigin.CustomerDelivered,
            snapshot.Items,
            snapshot.Receiving,
            [
                new ReturnResolutionLineSnapshot(
                    snapshot.Items[0].ReturnItemId,
                    Requested: 2m,
                    Received: 2m,
                    Usable: 1m,
                    Damaged: 1m,
                    Accepted: 1m,
                    Rejected: 1m,
                    Reason: "یک عدد سالم پذیرفته شد",
                    Version: 1),
            ],
            storedVersion: 1,
            originalAmount: 200m);

        Assert.True(plan.IsComplete);
        Assert.True(plan.FromReviewedPlan);
        Assert.Equal(100m, plan.ApprovedAmount);
        var line = Assert.Single(plan.Lines);
        Assert.Equal(1m, line.AcceptedUsable);
        Assert.Equal(0m, line.AcceptedDamaged);
        Assert.Equal(1m, line.RejectedReceived);
    }

    [Fact]
    public void A_plan_that_accepts_more_than_was_received_is_refused_not_clamped()
    {
        var snapshot = Snapshot(
            items: [Item(Guid.NewGuid(), quantity: 2m, unitPrice: 100m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 2m, usable: 1m, damaged: 0m));

        var plan = ReturnResolutionPolicy.Describe(
            ReturnOrigin.CustomerDelivered,
            snapshot.Items,
            snapshot.Receiving,
            [
                new ReturnResolutionLineSnapshot(
                    snapshot.Items[0].ReturnItemId, 2m, 1m, 1m, 0m, 2m, 0m, "همه پذیرفته", 1),
            ],
            storedVersion: 1,
            originalAmount: 200m);

        Assert.False(plan.IsComplete);
        Assert.Equal(0m, plan.ApprovedAmount);
        Assert.Equal(ReturnsErrorCodes.ResolutionReceivedExceeded, plan.BlockerCode);
    }

    [Fact]
    public void A_plan_that_leaves_part_of_the_claim_unaccounted_is_incomplete()
    {
        var snapshot = Snapshot(
            items: [Item(Guid.NewGuid(), quantity: 3m, unitPrice: 50m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 3m, usable: 3m, damaged: 0m));

        var plan = ReturnResolutionPolicy.Describe(
            ReturnOrigin.CustomerDelivered,
            snapshot.Items,
            snapshot.Receiving,
            [
                new ReturnResolutionLineSnapshot(
                    snapshot.Items[0].ReturnItemId, 3m, 3m, 3m, 0m, 2m, 0m, "دو عدد", 1),
            ],
            storedVersion: 1,
            originalAmount: 150m);

        Assert.False(plan.IsComplete);
        Assert.Equal(ReturnsErrorCodes.ResolutionIncomplete, plan.BlockerCode);
    }

    [Fact]
    public void No_plan_and_no_receipt_keeps_the_legacy_amount_out_of_the_physical_math()
    {
        // A pre-279 approved case that never recorded a customer receipt cannot claim the baseline "everything arrived".
        var snapshot = Snapshot(
            status: ReturnRequestStatus.Approved,
            items: [Item(Guid.NewGuid(), quantity: 1m, unitPrice: 200m, ReturnItemDisposition.None)],
            receiving: null,
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision);

        var plan = ReturnResolutionPolicy.Resolve(snapshot);
        Assert.False(plan.IsComplete);
        Assert.False(plan.FromReviewedPlan);
        Assert.Equal(ReturnsErrorCodes.ResolutionRequired, plan.BlockerCode);
        Assert.False(ReturnResolutionPolicy.IsPhysicalComplete(snapshot));
    }

    // ------------------------------------------------------------------ B: audited correction availability

    [Fact]
    public void A_no_money_decision_may_be_corrected_and_a_started_money_fact_may_not()
    {
        var approvable = Snapshot(
            status: ReturnRequestStatus.Approved,
            items: [Item(Guid.NewGuid(), quantity: 1m, unitPrice: 200m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 1m, usable: 1m, damaged: 0m),
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision,
            effectiveDecision: ReturnRecoveryPolicy.ApprovedDecision);

        var available = ReturnCorrectionPolicy.Describe(approvable);
        Assert.True(available.CanReopenForReview);
        Assert.Null(available.BlockerCode);

        var withMoney = approvable with
        {
            RefundAttempts =
            [
                new RefundAttemptSnapshot(
                    Guid.NewGuid(), PaymentId, 200m, "IRR", RefundAttemptStatus.Pending, "refund-1", null, null, Now, null),
            ],
        };

        var refused = ReturnCorrectionPolicy.Describe(withMoney);
        Assert.False(refused.CanReopenForReview);
        Assert.Equal(ReturnsErrorCodes.CorrectionFinancialEffectRecorded, refused.BlockerCode);
    }

    [Fact]
    public void A_handed_back_item_or_a_courier_case_is_never_correctable()
    {
        var itemId = Guid.NewGuid();
        var withCustody = Snapshot(
            status: ReturnRequestStatus.Rejected,
            items: [Item(itemId, quantity: 1m, unitPrice: 200m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 1m, usable: 1m, damaged: 0m),
            recordedDecision: ReturnRecoveryPolicy.RejectedDecision,
            effectiveDecision: ReturnRecoveryPolicy.RejectedDecision,
            custody: [new ReturnCustodySnapshot(Guid.NewGuid(), itemId, 1m, ReturnItemCondition.Usable, CustomerId, "رسید-۱", Now)]);

        Assert.Equal(
            ReturnsErrorCodes.CorrectionCustodyRecorded,
            ReturnCorrectionPolicy.Describe(withCustody).BlockerCode);

        var courier = withCustody with { Origin = ReturnOrigin.CourierOrigin, Custody = [] };
        Assert.Equal(
            ReturnsErrorCodes.CorrectionSourceUnsupported,
            ReturnCorrectionPolicy.Describe(courier).BlockerCode);
    }

    [Fact]
    public void An_undecided_case_has_nothing_to_correct()
    {
        var requested = Snapshot(
            items: [Item(Guid.NewGuid(), quantity: 1m, unitPrice: 200m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 1m, usable: 1m, damaged: 0m));

        var assessment = ReturnCorrectionPolicy.Describe(requested);
        Assert.False(assessment.CanReopenForReview);
        Assert.Equal(ReturnsErrorCodes.CorrectionNotApplicable, assessment.BlockerCode);
    }

    // ------------------------------------------------------------------ C: per-portion physical completion

    [Fact]
    public void A_mixed_line_is_not_settled_until_every_accepted_portion_and_every_handback_is_recorded()
    {
        var itemId = Guid.NewGuid();
        var rejected = Snapshot(
            status: ReturnRequestStatus.Rejected,
            items: [Item(itemId, quantity: 2m, unitPrice: 100m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 2m, usable: 1m, damaged: 1m),
            recordedDecision: ReturnRecoveryPolicy.RejectedDecision,
            effectiveDecision: ReturnRecoveryPolicy.RejectedDecision,
            resolution: Plan(itemId, accepted: 0m, rejected: 2m, usable: 1m, damaged: 1m));

        // Received goods still in the operator's hands: the case cannot read as physically complete.
        Assert.False(ReturnResolutionPolicy.IsPhysicalComplete(rejected));
        Assert.Equal(2m, ReturnResolutionPolicy.OwedCustody(
            Assert.Single(ReturnResolutionPolicy.Resolve(rejected).Lines),
            rejected.Custody));

        var handedBack = rejected with
        {
            Custody =
            [
                new ReturnCustodySnapshot(Guid.NewGuid(), itemId, 2m, ReturnItemCondition.Usable, CustomerId, "رسید-۲", Now),
            ],
        };

        Assert.True(ReturnResolutionPolicy.IsPhysicalComplete(handedBack));
        Assert.Equal(0m, ReturnResolutionPolicy.OwedCustody(
            Assert.Single(ReturnResolutionPolicy.Resolve(handedBack).Lines),
            handedBack.Custody));
    }

    [Fact]
    public void An_accepted_usable_portion_still_owes_its_own_restock_fact()
    {
        var itemId = Guid.NewGuid();
        var accepted = Snapshot(
            status: ReturnRequestStatus.Approved,
            items: [Item(itemId, quantity: 2m, unitPrice: 100m, ReturnItemDisposition.None)],
            receiving: Receiving(requested: 2m, usable: 1m, damaged: 1m),
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision,
            effectiveDecision: ReturnRecoveryPolicy.ApprovedDecision,
            resolution: Plan(itemId, accepted: 1m, rejected: 1m, usable: 1m, damaged: 1m),
            custody: [new ReturnCustodySnapshot(Guid.NewGuid(), itemId, 1m, ReturnItemCondition.Usable, CustomerId, "رسید-۳", Now)]);

        // The handback is recorded but the accepted usable unit was never restocked: not complete.
        Assert.False(ReturnResolutionPolicy.IsPhysicalComplete(accepted));

        var restocked = accepted with
        {
            Items = [Item(itemId, quantity: 2m, unitPrice: 100m, ReturnItemDisposition.SaleableRestock)],
        };

        Assert.True(ReturnResolutionPolicy.IsPhysicalComplete(restocked));
    }

    // ------------------------------------------------------------------ D: delivery-slice allocation

    [Fact]
    public void An_old_legacy_case_is_discounted_from_the_oldest_slice_only()
    {
        var lineId = Guid.NewGuid();
        var slices = new[]
        {
            new ReturnDeliverySliceSource(lineId, 1m, Now.AddDays(-6)),
            new ReturnDeliverySliceSource(lineId, 1m, Now.AddDays(-1)),
        };

        var availability = ReturnAllocationPolicy.Describe(
            lineId,
            slices,
            windowDays: 7,
            now: Now,
            allocatedElsewhere: new Dictionary<string, decimal>(),
            legacyUnallocated: 1m);

        Assert.Equal(2, availability.Count);
        Assert.Equal(0m, availability[0].AvailableQuantity);
        Assert.Equal(1m, availability[1].AvailableQuantity);
        Assert.All(availability, x => Assert.True(x.IsInWindow));
    }

    [Fact]
    public void Allocation_takes_the_oldest_deadline_first_and_refuses_insufficient_capacity()
    {
        var lineId = Guid.NewGuid();
        var availability = ReturnAllocationPolicy.Describe(
            lineId,
            [
                new ReturnDeliverySliceSource(lineId, 1m, Now.AddDays(-5)),
                new ReturnDeliverySliceSource(lineId, 2m, Now.AddDays(-1)),
            ],
            windowDays: 7,
            now: Now,
            allocatedElsewhere: new Dictionary<string, decimal>());

        var allocated = ReturnAllocationPolicy.Allocate(availability, 2m);
        Assert.NotNull(allocated);
        Assert.Equal(2, allocated!.Count);
        Assert.Equal(1m, allocated[0].Quantity);
        Assert.Equal(1m, allocated[1].Quantity);
        Assert.True(allocated[0].DeliveredAt < allocated[1].DeliveredAt);

        // A slice whose window closed keeps its quantity but may never fund a new claim.
        var expired = ReturnAllocationPolicy.Describe(
            lineId,
            [new ReturnDeliverySliceSource(lineId, 1m, Now.AddDays(-30))],
            windowDays: 7,
            now: Now,
            allocatedElsewhere: new Dictionary<string, decimal>());
        Assert.False(Assert.Single(expired).IsInWindow);
        Assert.Null(ReturnAllocationPolicy.Allocate(expired, 1m));
    }

    [Fact]
    public void Two_deliveries_at_the_same_instant_merge_into_one_honest_slice()
    {
        var lineId = Guid.NewGuid();
        var deliveredAt = Now.AddDays(-2);
        var availability = ReturnAllocationPolicy.Describe(
            lineId,
            [
                new ReturnDeliverySliceSource(lineId, 1m, deliveredAt),
                new ReturnDeliverySliceSource(lineId, 1m, deliveredAt),
            ],
            windowDays: 7,
            now: Now,
            allocatedElsewhere: new Dictionary<string, decimal>());

        var slice = Assert.Single(availability);
        Assert.Equal(2m, slice.SliceQuantity);
        Assert.Equal(2m, slice.AvailableQuantity);
    }

    // ------------------------------------------------------------------ E: the shared refund budget

    [Fact]
    public void The_budget_lock_is_stable_per_payment_and_never_collides_across_payments()
    {
        var first = ReturnBudgetSerialization.AdvisoryLockKey(PaymentId);
        var again = ReturnBudgetSerialization.AdvisoryLockKey(PaymentId);
        var other = ReturnBudgetSerialization.AdvisoryLockKey(Guid.NewGuid());

        Assert.Equal(first, again);
        Assert.NotEqual(first, other);
        // The namespace is part of the key, so this invariant cannot be confused with another advisory lock.
        Assert.NotEqual(PaymentId.GetHashCode(), first);
    }

    // ------------------------------------------------------------------ fixtures

    private static ReturnResolutionSnapshot Plan(
        Guid itemId,
        decimal accepted,
        decimal rejected,
        decimal usable,
        decimal damaged) =>
        new(
            Version: 1,
            IsComplete: true,
            ApprovedAmount: 0m,
            FromReviewedPlan: true,
            Lines:
            [
                new ReturnResolutionLineSnapshot(
                    itemId, accepted + rejected, usable + damaged, usable, damaged, accepted, rejected, "بررسی‌شده", 1),
            ]);

    private static ReturnItemSnapshot Item(
        Guid itemId,
        decimal quantity,
        decimal unitPrice,
        ReturnItemDisposition disposition) =>
        new(itemId, Guid.NewGuid(), quantity, unitPrice, "IRR", null, ReturnItemCondition.Usable, disposition);

    private static ReturnReceivingSummary Receiving(
        decimal requested,
        decimal usable,
        decimal damaged) =>
        new(
            // The receiving summary is the stored inspection fact that drives the per-portion arithmetic, so the line
            // identity only has to be a stable placeholder of the same case.
            [
                new ReturnReceivingLine(
                    Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    requested,
                    usable + damaged,
                    usable,
                    damaged),
            ],
            IsComplete: usable + damaged >= requested,
            IsCoherent: true);

    private static ReturnSnapshot Snapshot(
        ReturnRequestStatus status = ReturnRequestStatus.Requested,
        IReadOnlyList<ReturnItemSnapshot>? items = null,
        ReturnReceivingSummary? receiving = null,
        string? recordedDecision = null,
        string? effectiveDecision = null,
        ReturnResolutionSnapshot? resolution = null,
        IReadOnlyList<RefundAttemptSnapshot>? refundAttempts = null,
        IReadOnlyList<ReturnCustodySnapshot>? custody = null) =>
        new(
            ReturnRequestId: Guid.NewGuid(),
            SellerOrderId: SellerOrderId,
            CheckoutId: CheckoutId,
            SellerPartyId: SellerPartyId,
            RequestedByUserId: CustomerId,
            Status: status,
            Reason: "دلیل",
            Currency: "IRR",
            RefundAmount: 200m,
            PaymentId: PaymentId,
            RefundDestination: RefundDestination.Wallet,
            CreatedAt: Now.AddDays(-1),
            UpdatedAt: Now,
            Items: items ?? [],
            RefundAttempts: refundAttempts ?? [],
            Receiving: receiving,
            RecordedDecision: recordedDecision,
            EffectiveDecision: effectiveDecision,
            Resolution: resolution,
            Custody: custody);
}
