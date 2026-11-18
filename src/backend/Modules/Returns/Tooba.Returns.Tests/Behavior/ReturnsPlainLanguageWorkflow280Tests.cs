using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-280 — the READ-ONLY capability projection behind the shopkeeper surface. The workspace may only offer the review
/// of goods that actually arrived: a coherent zero-receipt case has nothing to review, a partial receipt still reaches a
/// lawful review of what did arrive, and a complete reviewed plan is never asked to be published twice. None of these
/// facts changes a write rule — they only stop the surface from advertising an action the owning writer would refuse.
/// </summary>
public sealed class ReturnsPlainLanguageWorkflow280Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-18T09:00:00Z");

    private static Guid ItemId { get; } = Guid.Parse("01920000-0000-7000-8000-000000000280");

    [Fact]
    public void Nothing_received_offers_only_the_arrival_and_never_a_zero_ceiling_review()
    {
        var work = ReturnWorkPolicy.Describe(Snapshot(receiving: Receiving(requested: 2m, received: 0m)));

        Assert.Equal(ReturnWorkCategory.Receiving, work.Category);
        Assert.True(work.CanRecordReceipt);
        Assert.False(work.CanPublishResolution);
        Assert.False(work.CanDecide);
        Assert.Equal(new[] { ReturnWorkActionCodes.RecordReceipt }, work.ActionCodes);
    }

    [Fact]
    public void What_actually_arrived_can_be_reviewed_even_when_the_claim_is_not_fully_received()
    {
        var work = ReturnWorkPolicy.Describe(Snapshot(receiving: Receiving(requested: 2m, received: 1m, usable: 1m)));

        Assert.Equal(ReturnWorkCategory.Receiving, work.Category);
        Assert.True(work.CanPublishResolution);
        Assert.Contains(ReturnWorkActionCodes.PublishResolution, work.ActionCodes);
        // The decision still waits for a complete physical basis; the review is the only lawful next step.
        Assert.False(work.CanDecide);
        Assert.DoesNotContain(ReturnWorkActionCodes.Approve, work.ActionCodes);
    }

    [Fact]
    public void A_complete_reviewed_plan_is_never_asked_to_be_published_again()
    {
        var work = ReturnWorkPolicy.Describe(Snapshot(
            receiving: Receiving(requested: 2m, received: 2m, usable: 2m),
            resolution: new ReturnResolutionSnapshot(
                1,
                IsComplete: true,
                ApprovedAmount: 2000m,
                FromReviewedPlan: true,
                Lines:
                [
                    new ReturnResolutionLineSnapshot(
                        ItemId,
                        Requested: 2m,
                        Received: 2m,
                        Usable: 2m,
                        Damaged: 0m,
                        AcceptedUsable: 2m,
                        AcceptedDamaged: 0m,
                        RejectedReceivedUsable: 0m,
                        RejectedReceivedDamaged: 0m,
                        RejectedUnreceived: 0m,
                        Reason: "هر دو سالم بود",
                        Version: 1),
                ])));

        Assert.False(work.CanPublishResolution);
        Assert.DoesNotContain(ReturnWorkActionCodes.PublishResolution, work.ActionCodes);
        // The operator still decides — the review simply is not repeated.
        Assert.True(work.CanDecide);
        Assert.Contains(ReturnWorkActionCodes.Approve, work.ActionCodes);
        Assert.Contains(ReturnWorkActionCodes.Reject, work.ActionCodes);
    }

    [Fact]
    public void Money_already_in_flight_never_reopens_the_review_of_the_goods()
    {
        var work = ReturnWorkPolicy.Describe(Snapshot(
            receiving: Receiving(requested: 1m, received: 1m, usable: 1m),
            status: ReturnRequestStatus.RefundProcessing,
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision,
            attempts: [Attempt(RefundAttemptStatus.Pending)]));

        Assert.False(work.CanPublishResolution);
        Assert.DoesNotContain(ReturnWorkActionCodes.PublishResolution, work.ActionCodes);
        Assert.Equal(ReturnWorkCategory.Refund, work.Category);
    }

    private static ReturnReceivingSummary Receiving(decimal requested, decimal received, decimal usable = 0m) =>
        ReturnReceivingSummary.Describe(
            [(ItemId, requested)],
            received <= 0m
                ? []
                :
                [
                    (
                        (Guid?)null,
                        ItemId,
                        received,
                        usable > 0m ? ReturnItemCondition.Usable : ReturnItemCondition.Damaged),
                ],
            owningReceiptIds: null);

    private static ReturnItemSnapshot Item() =>
        new(
            ItemId,
            Guid.Parse("01920000-0000-7000-8000-000000000281"),
            2m,
            1000m,
            "IRR",
            Guid.Parse("01920000-0000-7000-8000-000000000282"),
            Disposition: ReturnItemDisposition.None);

    private static RefundAttemptSnapshot Attempt(RefundAttemptStatus status) =>
        new(
            Guid.Parse("01920000-0000-7000-8000-000000000283"),
            Guid.Parse("01920000-0000-7000-8000-000000000284"),
            1000m,
            "IRR",
            status,
            "refund-reviewed",
            status == RefundAttemptStatus.Failed ? null : "ref-280",
            status == RefundAttemptStatus.Failed ? "REFUND_DECLINED" : null,
            Now,
            status == RefundAttemptStatus.Pending ? null : Now);

    private static ReturnSnapshot Snapshot(
        ReturnReceivingSummary receiving,
        ReturnRequestStatus status = ReturnRequestStatus.Requested,
        string? recordedDecision = null,
        IReadOnlyList<RefundAttemptSnapshot>? attempts = null,
        ReturnResolutionSnapshot? resolution = null) =>
        new(
            Guid.Parse("01920000-0000-7000-8000-000000000285"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "دلیل مشتری",
            "IRR",
            2000m,
            Guid.Parse("01920000-0000-7000-8000-000000000286"),
            RefundDestination.OriginalPayment,
            Now,
            Now,
            [Item()],
            attempts ?? [],
            RecordedDecision: recordedDecision,
            Receiving: receiving,
            Resolution: resolution);
}
