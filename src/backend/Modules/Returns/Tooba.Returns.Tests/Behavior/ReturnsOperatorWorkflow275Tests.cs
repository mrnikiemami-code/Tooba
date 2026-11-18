using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Application.ReturnRequests.Queries;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Returns.Infrastructure.Queries;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-275 — the operator workflow behind the admin return queue: ONE work-stage classification shared by the queue, the
/// case workspace and the actions; a legacy partial receipt that can still be continued on a financially progressed
/// case; and a reviewed definite-failure retry whose child attempt identity is derived from the reviewed failure, so a
/// duplicate POST can never mint a second refund attempt or send money twice.
/// </summary>
public sealed class ReturnsOperatorWorkflow275Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T09:00:00Z");

    // ------------------------------------------------------------------ A: one work-stage authority

    [Fact]
    public void Work_stage_puts_receiving_before_the_decision_and_never_offers_an_action_it_cannot_run()
    {
        var noReceipt = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.Requested,
            items: [Item(ReturnItemDisposition.None)],
            receiving: Receiving(requested: 2m, received: 0m)));

        Assert.Equal(ReturnWorkCategory.Receiving, noReceipt.Category);
        Assert.True(noReceipt.CanRecordReceipt);

        // A delivered-customer case can never be decided before its goods were actually received and inspected.
        Assert.False(noReceipt.CanDecide);
        Assert.DoesNotContain(ReturnWorkActionCodes.Approve, noReceipt.ActionCodes);
        Assert.DoesNotContain(ReturnWorkActionCodes.Reject, noReceipt.ActionCodes);
        Assert.True(noReceipt.NeedsOperator);

        var partiallyReceived = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.Requested,
            items: [Item(ReturnItemDisposition.None)],
            receiving: Receiving(requested: 2m, received: 1m)));
        Assert.Equal(ReturnWorkCategory.Receiving, partiallyReceived.Category);
        Assert.True(partiallyReceived.CanRecordReceipt);
        Assert.False(partiallyReceived.CanDecide);

        // Once every requested unit is recorded, the SAME case becomes a decision and stops accepting receipts.
        var complete = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.Requested,
            items: [Item(ReturnItemDisposition.None)],
            receiving: Receiving(requested: 2m, received: 2m)));
        Assert.Equal(ReturnWorkCategory.Decision, complete.Category);
        Assert.True(complete.CanDecide);
        Assert.False(complete.CanRecordReceipt);
        Assert.Equal(
            // TB-279 adds the additive per-item resolution verb to the same case: the claim is fully received but its
            // effective plan still needs the operator's accepted/rejected split before money can be named.
            new[]
            {
                ReturnWorkActionCodes.PublishResolution,
                ReturnWorkActionCodes.Approve,
                ReturnWorkActionCodes.Reject,
            },
            complete.ActionCodes);
    }

    [Fact]
    public void Work_stage_is_derived_from_the_one_recovery_answer()
    {
        // Approved money with an unknown (pending) attempt: system work, not an operator notification.
        var pending = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.RefundProcessing,
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision,
            items: [Item(ReturnItemDisposition.None)],
            receiving: Receiving(requested: 1m, received: 1m),
            attempts: [Attempt(RefundAttemptStatus.Pending)]));
        Assert.Equal(ReturnWorkCategory.Refund, pending.Category);
        Assert.True(pending.CanResumeRefund);
        Assert.True(pending.IsOpen);
        Assert.False(pending.NeedsOperator);
        Assert.Equal(new[] { ReturnWorkActionCodes.ResumeRefund }, pending.ActionCodes);

        // A durable definite failure is the reviewed retry stage and exposes the exact failed attempt.
        var failedAttempt = Attempt(RefundAttemptStatus.Failed);
        var failed = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.RefundFailed,
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision,
            items: [Item(ReturnItemDisposition.None)],
            receiving: Receiving(requested: 1m, received: 1m),
            attempts: [failedAttempt]));
        Assert.Equal(ReturnWorkCategory.RefundFailed, failed.Category);
        Assert.True(failed.CanRetryRefund);
        Assert.True(failed.NeedsOperator);
        Assert.Equal(failedAttempt.RefundAttemptId, failed.FailedRefundAttemptId);
        Assert.Null(failed.PendingRefundAttemptId);
        Assert.Equal(new[] { ReturnWorkActionCodes.RetryRefund }, failed.ActionCodes);

        // Confirmed money over unsettled goods is physical work only.
        var disposition = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.Completed,
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision,
            items: [Item(ReturnItemDisposition.None)],
            receiving: Receiving(requested: 1m, received: 1m),
            attempts: [Attempt(RefundAttemptStatus.Succeeded)]));
        Assert.Equal(ReturnWorkCategory.Disposition, disposition.Category);
        Assert.True(disposition.CanSettlePhysical);
        Assert.True(disposition.NeedsOperator);

        // Every durable physical fact recorded: settled, not "open work".
        var settled = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.Completed,
            recordedDecision: ReturnRecoveryPolicy.ApprovedDecision,
            items: [Item(ReturnItemDisposition.SaleableRestock)],
            receiving: Receiving(requested: 1m, received: 1m, usable: 1m),
            attempts: [Attempt(RefundAttemptStatus.Succeeded)]));
        Assert.Equal(ReturnWorkCategory.Settled, settled.Category);
        Assert.False(settled.IsOpen);

        // A decided-away case is closed history with no executable verb.
        var rejected = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.Rejected,
            recordedDecision: ReturnRecoveryPolicy.RejectedDecision,
            items: [Item(ReturnItemDisposition.None)],
            receiving: Receiving(requested: 1m, received: 0m)));
        Assert.Equal(ReturnWorkCategory.Closed, rejected.Category);
        Assert.Empty(rejected.ActionCodes);

        // Contradictory stored receiving evidence never reads as an allowed action.
        var incoherent = ReturnWorkPolicy.Describe(Snapshot(
            ReturnRequestStatus.Requested,
            items: [Item(ReturnItemDisposition.None)],
            receiving: new ReturnReceivingSummary([], IsComplete: false, IsCoherent: false)));
        Assert.False(incoherent.CanRecordReceipt);
        Assert.False(incoherent.CanDecide);
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, incoherent.BlockerCode);
    }

    [Fact]
    public void Every_legacy_queue_token_lands_on_the_honest_stage_and_never_on_everything()
    {
        Assert.Equal(AdminReturnWorkQueueSelection.Open, AdminReturnWorkQueueSelection.Canonicalize(null));
        Assert.Equal(AdminReturnWorkQueueSelection.Open, AdminReturnWorkQueueSelection.Canonicalize("all"));
        Assert.Equal(ReturnWorkCategoryCodes.Decision, AdminReturnWorkQueueSelection.Canonicalize("pending_review"));
        Assert.Equal(ReturnWorkCategoryCodes.Refund, AdminReturnWorkQueueSelection.Canonicalize("refund_pending"));
        Assert.Equal(ReturnWorkCategoryCodes.RefundFailed, AdminReturnWorkQueueSelection.Canonicalize("refund_failed"));
        Assert.Equal(AdminReturnWorkQueueSelection.History, AdminReturnWorkQueueSelection.Canonicalize("rejected"));
        Assert.Equal(ReturnWorkCategoryCodes.Receiving, AdminReturnWorkQueueSelection.Canonicalize("receiving"));

        // An unknown token is NOT "everything": it falls back to the honest active set.
        Assert.Equal(AdminReturnWorkQueueSelection.Open, AdminReturnWorkQueueSelection.Canonicalize("something-else"));
    }

    [Fact]
    public void Grid_policy_canonicalizes_the_selector_and_refuses_it_inside_an_advanced_expression()
    {
        var normalized = AdminReturnGridQueryPolicy.Instance.Normalize(new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("queueFilter", "equals", "refund_failed", null, null)],
            null));
        var selection = Assert.Single(normalized.Filters);
        Assert.Equal("workCategory", selection.Field);
        Assert.Equal(ReturnWorkCategoryCodes.RefundFailed, selection.Value);

        // An absent selector still constrains the page to the active work set (no silent "everything").
        var untargeted = AdminReturnGridQueryPolicy.Instance.Normalize(new GridQueryRequest(1, 20, null, [], [], null));
        Assert.Equal(AdminReturnWorkQueueSelection.Open, Assert.Single(untargeted.Filters).Value);

        // A selector is not a column: it can never be evaluated inside the advanced-filter expression.
        var refused = Assert.Throws<GridQueryValidationException>(() => AdminReturnGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(
                1,
                20,
                null,
                [],
                [],
                new GridAdvancedFilterExpression(
                    [new GridAdvancedFilterCondition("adv-0", "workCategory", "equals", "decision", null, null)],
                    []))));
        Assert.Equal("grid.advancedFilter.field.invalid", refused.ErrorCode);

        // More than one selection value is a transport mistake, not a superset.
        Assert.Throws<GridQueryValidationException>(() => AdminReturnGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(
                1,
                20,
                null,
                [],
                [new GridFilterRequest("workCategory", "equals", "decision", null, ["decision", "refund"])],
                null)));
    }

    // ------------------------------------------------------------------ C1: partial receiving continuation

    [Fact]
    public async Task A_financially_progressed_case_can_still_finish_its_partial_receiving()
    {
        var fixture = await SeedAsync(Staged.PartiallyReceivedPendingMoney);
        await using var order = fixture.Open();

        var before = (await order.Snapshot())!;
        Assert.Equal(ReturnWorkCategory.Receiving, before.Work!.Category);
        Assert.True(before.Work.CanRecordReceipt);
        Assert.False(before.Receiving!.IsComplete);
        Assert.Equal(1m, before.Receiving.Remaining);

        // The remaining delivery is recorded on the SAME case even though its money already started.
        var recorded = await order.RecordAsync("delivery-2", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));
        Assert.True(recorded.Receiving!.IsComplete);
        Assert.Equal(2, recorded.Receipts!.Count);
        Assert.Equal(2m, recorded.Receiving.Usable);

        // Now the workspace offers the money work, not another receipt.
        Assert.False(recorded.Work!.CanRecordReceipt);
        Assert.Equal(ReturnWorkCategory.Refund, recorded.Work.Category);
        Assert.True(recorded.Work.CanResumeRefund);
    }

    [Fact]
    public async Task A_disposed_or_complete_case_never_accepts_a_further_receipt()
    {
        var completeFixture = await SeedAsync(Staged.CompleteReceiptPendingMoney);
        await using (var complete = completeFixture.Open())
        {
            var refused = await Assert.ThrowsAsync<ContractOperationException>(() => complete.RecordAsync(
                "delivery-extra", (complete.ReturnItemId, 1m, ReturnItemCondition.Usable)));
            Assert.Equal(ReturnsErrorCodes.ReceiptStateInvalid, refused.Code);
            Assert.Equal(1, await complete.ReceiptCount());
        }

        var disposedFixture = await SeedAsync(Staged.PartiallyReceivedDisposed);
        await using (var disposed = disposedFixture.Open())
        {
            var refused = await Assert.ThrowsAsync<ContractOperationException>(() => disposed.RecordAsync(
                "delivery-2", (disposed.ReturnItemId, 1m, ReturnItemCondition.Usable)));
            Assert.Equal(ReturnsErrorCodes.ReceiptStateInvalid, refused.Code);
            Assert.Equal(1, await disposed.ReceiptCount());
        }
    }

    [Fact]
    public async Task A_rejected_case_never_receives_new_goods()
    {
        var fixture = await SeedAsync(Staged.RejectedWithPartialReceipt);
        await using var order = fixture.Open();

        var before = (await order.Snapshot())!;
        Assert.Equal(ReturnRequestStatus.Rejected, before.Status);
        // A decided-away case is history even while its stored receiving is incomplete.
        Assert.Equal(ReturnWorkCategory.Closed, before.Work!.Category);
        Assert.False(before.Work.CanRecordReceipt);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-after-reject", (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptStateInvalid, refused.Code);
        Assert.Equal(1, await order.ReceiptCount());
    }

    // ------------------------------------------------------------------ C2: reviewed definite-failure retry

    [Fact]
    public async Task Retry_refuses_an_identity_that_is_not_a_stored_failure_of_this_case()
    {
        var fixture = await SeedAsync(Staged.DefiniteFailureWithReceipt);
        await using var order = fixture.Open();

        // A random id is not a stored row of this case.
        var unknown = await Assert.ThrowsAsync<ContractOperationException>(
            () => order.RetryAsync(Guid.NewGuid()));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, unknown.Code);

        // A real stored attempt of this case that is NOT a definite failure is not a reviewable failure either.
        var succeededFixture = await SeedAsync(Staged.SucceededWithReceipt);
        await using var succeeded = succeededFixture.Open();
        var completed = Assert.Single(await succeeded.Attempts());
        Assert.Equal(RefundAttemptStatus.Succeeded, completed.Status);
        var notFailed = await Assert.ThrowsAsync<ContractOperationException>(
            () => succeeded.RetryAsync(completed.RefundAttemptId));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, notFailed.Code);
        Assert.Empty(succeeded.Gateway.Calls);
        Assert.Equal(1, await succeeded.AttemptCount());
    }

    [Fact]
    public async Task Retry_binds_the_child_attempt_to_the_reviewed_failure_deterministically()
    {
        var fixture = await SeedAsync(Staged.DefiniteFailureWithReceipt);
        await using var order = fixture.Open();
        var reviewed = Assert.Single(await order.Attempts());

        var retried = await order.RetryAsync(reviewed.RefundAttemptId);

        // The child is the DETERMINISTIC identity of this reviewed failure — not a count-based key.
        var child = Assert.Single(
            await order.Attempts(),
            x => x.RefundAttemptId != reviewed.RefundAttemptId);
        Assert.Equal(DerivedChildId(order.ReturnRequestId, reviewed.RefundAttemptId), child.RefundAttemptId);
        Assert.Equal(DerivedChildKey(reviewed.RefundAttemptId), child.IdempotencyKey);
        Assert.Equal(2, await order.AttemptCount());
        Assert.Equal(DerivedChildKey(reviewed.RefundAttemptId), Assert.Single(order.Gateway.Calls).IdempotencyKey);
        Assert.Equal(ReturnRequestStatus.Completed, retried.Status);
    }

    [Fact]
    public async Task A_duplicate_post_answers_the_recorded_child_without_sending_money_again()
    {
        var fixture = await SeedAsync(Staged.DefiniteFailureWithReceipt);
        Guid reviewedAttemptId;
        Guid childAttemptId;
        await using (var first = fixture.Open())
        {
            reviewedAttemptId = Assert.Single(await first.Attempts()).RefundAttemptId;
            Assert.Equal(ReturnRequestStatus.Completed, (await first.RetryAsync(reviewedAttemptId)).Status);
            Assert.Single(first.Gateway.Calls);
            childAttemptId = Assert.Single(
                await first.Attempts(),
                x => x.RefundAttemptId != reviewedAttemptId).RefundAttemptId;
        }

        // TB-276 (B): a second, independent operator scope replays the PARENT intent. The read no longer offers a
        // retry for the settled case, and the duplicate POST answers the SAME durable child — no new attempt, no send.
        await using var duplicate = fixture.Open();
        Assert.Equal(ReturnWorkCategory.Settled, (await duplicate.Snapshot())!.Work!.Category);
        var replayed = await duplicate.RetryAsync(reviewedAttemptId);
        Assert.Equal(ReturnRequestStatus.Completed, replayed.Status);
        Assert.Contains(replayed.RefundAttempts, x => x.RefundAttemptId == childAttemptId);
        Assert.Empty(duplicate.Gateway.Calls);
        Assert.Equal(2, await duplicate.AttemptCount());
    }

    [Fact]
    public async Task Retry_refuses_a_reviewed_attempt_that_is_not_the_stored_failure()
    {
        var fixture = await SeedAsync(Staged.DefiniteFailureWithReceipt);
        await using var order = fixture.Open();

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => order.RetryAsync(Guid.NewGuid()));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, refused.Code);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Equal(ReturnRequestStatus.RefundFailed, (await order.Snapshot())!.Status);
    }

    [Fact]
    public async Task Retry_requires_an_explicit_reviewed_failure_at_every_boundary()
    {
        var fixture = await SeedAsync(Staged.DefiniteFailureWithReceipt);
        await using var order = fixture.Open();
        var stored = Assert.Single(await order.Attempts());

        // TB-276 (B): an absent id is NOT "the current failure" — the caller must state the attempt it reviewed.
        var missing = await Assert.ThrowsAsync<ContractOperationException>(() => order.RetryAsync(null));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, missing.Code);
        var empty = await Assert.ThrowsAsync<ContractOperationException>(() => order.RetryAsync(Guid.Empty));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, empty.Code);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Equal(ReturnRequestStatus.RefundFailed, (await order.Snapshot())!.Status);

        // The explicit, actually stored review is still the lawful command.
        Assert.Equal(ReturnRequestStatus.Completed, (await order.RetryAsync(stored.RefundAttemptId)).Status);
        Assert.Single(order.Gateway.Calls);
    }

    [Fact]
    public async Task Retry_is_refused_when_the_case_has_no_single_reviewable_failure()
    {
        var fixture = await SeedAsync(Staged.AmbiguousFailuresWithReceipt);
        await using var order = fixture.Open();
        var attempts = await order.Attempts();
        Assert.Equal(2, attempts.Count);

        // An explicit id cannot resolve an ambiguous case: the read publishes no single reviewable failure, and the
        // write applies the same rule, so nothing is minted for either named row.
        foreach (var attempt in attempts)
        {
            var refused = await Assert.ThrowsAsync<ContractOperationException>(
                () => order.RetryAsync(attempt.RefundAttemptId));
            Assert.Equal(ReturnsErrorCodes.RecoveryAttemptUnbound, refused.Code);
        }

        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(2, await order.AttemptCount());

        // The READ agrees: the case is a definite failure that no single reviewed attempt can execute.
        Assert.Equal(ReturnWorkCategory.RefundFailed, (await order.Snapshot())!.Work!.Category);
    }

    [Fact]
    public async Task After_the_reviewed_retry_fails_again_the_child_is_the_only_reviewable_failure()
    {
        var fixture = await SeedAsync(Staged.DefiniteFailureWithReceipt);
        Guid parentId;
        Guid childId;
        await using (var first = fixture.Open())
        {
            parentId = Assert.Single(await first.Attempts()).RefundAttemptId;
            first.Gateway.Succeed = false;
            Assert.Equal(ReturnRequestStatus.RefundFailed, (await first.RetryAsync(parentId)).Status);
            childId = Assert.Single(await first.Attempts(), x => x.RefundAttemptId != parentId).RefundAttemptId;
        }

        // The READ now answers with the CHILD: it is the current definite failure, and the parent is history.
        await using var next = fixture.Open();
        var snapshot = (await next.Snapshot())!;
        Assert.Equal(ReturnWorkCategory.RefundFailed, snapshot.Work!.Category);
        Assert.True(snapshot.Work.CanRetryRefund);
        Assert.Equal(childId, snapshot.Work.FailedRefundAttemptId);

        // TB-276 (B): re-submitting the PARENT is the replay of an already-applied intent, so it answers its own
        // deterministic child (the failed row) and mints/sends nothing.
        var replay = await next.RetryAsync(parentId);
        Assert.Equal(ReturnRequestStatus.RefundFailed, replay.Status);
        Assert.Contains(replay.RefundAttempts, x => x.RefundAttemptId == childId);
        Assert.Empty(next.Gateway.Calls);
        Assert.Equal(2, await next.AttemptCount());

        // A NEW deliberate review bound to the child is the only lawful next attempt.
        next.Gateway.Succeed = true;
        Assert.Equal(ReturnRequestStatus.Completed, (await next.RetryAsync(childId)).Status);
        Assert.Single(next.Gateway.Calls);
        Assert.Equal(3, await next.AttemptCount());
    }

    /// <summary>
    /// TB-276: the served case detail must show the same product/quantity/unit as the queue row, resolved from the
    /// owning Order/Catalog Contracts reads — and an owning read that cannot answer leaves the display facts unknown
    /// instead of inventing a product name or defaulting a missing unit.
    /// </summary>
    [Fact]
    public async Task Admin_detail_adds_each_line_product_and_unit_without_touching_the_stored_facts()
    {
        var fixture = await SeedAsync(Staged.CompleteReceiptPendingMoney);
        await using var harness = fixture.Open();
        var variantId = Guid.NewGuid();

        var answered = new AdminReturnDetailQueryEngine(
            harness.Directory,
            new FixedGridEnrichment(fixture.OrderLineId, variantId, "pcs"),
            new FixedCatalogTitles(variantId, "لنوو اولترابوک مدل ۲"));
        var detail = await answered.QueryAsync(fixture.ReturnRequestId, CancellationToken.None);

        Assert.NotNull(detail);
        var line = Assert.Single(detail!.Items);
        Assert.Equal("لنوو اولترابوک مدل ۲", line.ProductLabel);
        Assert.Equal("pcs", line.UnitLabel);
        // Display enrichment never edits the owning facts.
        Assert.Equal(fixture.Requested, line.Quantity);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, detail.Status);

        // An owning read that answers nothing keeps the display facts null: no invented title, no default «واحد».
        var silent = new AdminReturnDetailQueryEngine(
            harness.Directory,
            new FixedGridEnrichment(fixture.OrderLineId, Guid.Empty, null),
            new FixedCatalogTitles(null, null));
        var blind = await silent.QueryAsync(fixture.ReturnRequestId, CancellationToken.None);
        Assert.NotNull(blind);
        Assert.Null(Assert.Single(blind!.Items).ProductLabel);
        Assert.Null(blind.Items[0].UnitLabel);

        // A case that does not exist is still a 404-shaped absence, not an empty enriched page.
        Assert.Null(await answered.QueryAsync(Guid.NewGuid(), CancellationToken.None));
    }

    // ------------------------------------------------------------------ fixtures

    private enum Staged
    {
        PartiallyReceivedPendingMoney,
        CompleteReceiptPendingMoney,
        PartiallyReceivedDisposed,
        DefiniteFailureWithReceipt,
        AmbiguousFailuresWithReceipt,
        SucceededWithReceipt,
        RejectedWithPartialReceipt,
    }

    private static Guid ItemId { get; } = Guid.Parse("01920000-0000-7000-8000-000000000275");

    private static Guid SellerPartyId { get; } = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

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

    private static ReturnItemSnapshot Item(ReturnItemDisposition disposition) =>
        new(
            ItemId,
            Guid.Parse("01920000-0000-7000-8000-000000000276"),
            1m,
            1000m,
            "IRR",
            // A received usable line must name its owning stock binding, otherwise a restock could never be confirmed
            // by Inventory and the case is (correctly) a typed blocker instead of "settled".
            Guid.Parse("01920000-0000-7000-8000-000000000279"),
            Disposition: disposition);

    private static RefundAttemptSnapshot Attempt(RefundAttemptStatus status) =>
        new(
            Guid.Parse("01920000-0000-7000-8000-000000000277"),
            Guid.Parse("01920000-0000-7000-8000-000000000278"),
            1000m,
            "IRR",
            status,
            "refund-reviewed",
            status == RefundAttemptStatus.Failed ? null : "ref-275",
            status == RefundAttemptStatus.Failed ? "REFUND_DECLINED" : null,
            Now,
            status == RefundAttemptStatus.Pending ? null : Now);

    private static ReturnSnapshot Snapshot(
        ReturnRequestStatus status,
        IReadOnlyList<ReturnItemSnapshot> items,
        ReturnReceivingSummary? receiving = null,
        IReadOnlyList<RefundAttemptSnapshot>? attempts = null,
        string? recordedDecision = null) =>
        new(
            Guid.Parse("01920000-0000-7000-8000-000000000274"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "دلیل",
            "IRR",
            1000m,
            Guid.Parse("01920000-0000-7000-8000-000000000278"),
            RefundDestination.OriginalPayment,
            Now,
            Now,
            items,
            attempts ?? [],
            RecordedDecision: recordedDecision,
            Receiving: receiving);

    /// <summary>The exact deterministic child identity of ONE reviewed failure (the contract this suite pins).</summary>
    private static Guid DerivedChildId(Guid returnRequestId, Guid failedAttemptId) =>
        new(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes($"tb275-refund-retry:{returnRequestId:N}:{failedAttemptId:N}"))
            .AsSpan(0, 16));

    /// <summary>The exact deterministic provider key of that same child attempt.</summary>
    private static string DerivedChildKey(Guid failedAttemptId) => $"refund-retry-{failedAttemptId:N}";

    /// <summary>One opened operator scope: its own context, its own gateway, the shared in-memory case.</summary>
    private sealed class Harness : IAsyncDisposable
    {
        private readonly ReturnDirectory _directory;

        public Harness(Fixture fixture, ReturnsDbContext db)
        {
            Fixture = fixture;
            Db = db;
            Gateway = new ScriptedGateway();
            var clock = new MutableClock(Now);
            var orderReader = new FixedOrderReader(fixture.OrderSnapshot);
            _directory = new ReturnDirectory(
                db,
                new OpenReturnUseCaseGuard(),
                orderReader,
                new ReturnEligibilityEvaluator(
                    orderReader,
                    new FixedFulfillmentReader(fixture.OrderSnapshot, fixture.OrderLineId),
                    db,
                    clock),
                new FixedPaymentReader(fixture.Requested * fixture.UnitPrice),
                Gateway,
                new ScriptedWallet(),
                new ScriptedInventory(),
                new ReturnsInstrumentation(),
                NullLogger<ReturnDirectory>.Instance,
                clock,
                new UuidV7IdGenerator());
        }

        public Fixture Fixture { get; }

        public IReturnDirectory Directory => _directory;

        public ReturnsDbContext Db { get; }

        public ScriptedGateway Gateway { get; }

        public Guid ReturnRequestId => Fixture.ReturnRequestId;

        public Guid ReturnItemId => Fixture.ReturnItemId;

        public Task<ReturnSnapshot> RecordAsync(
            string occurrenceKey,
            params (Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)[] lines) =>
            _directory.RecordReceiptAsync(
                new RecordReturnReceiptCommand(
                    ReturnRequestId,
                    Guid.NewGuid(),
                    occurrenceKey,
                    lines.Select(x => new CustomerReceiptLine(x.ReturnItemId, x.Quantity, x.Condition)).ToList()),
                CancellationToken.None);

        public Task<ReturnSnapshot> RetryAsync(Guid? expectedFailedAttemptId) =>
            _directory.RetryRefundAsync(
                new RetryReturnRefundCommand(ReturnRequestId, Guid.NewGuid())
                {
                    ExpectedFailedAttemptId = expectedFailedAttemptId,
                },
                CancellationToken.None);

        public Task<ReturnSnapshot?> Snapshot() => _directory.GetAsync(ReturnRequestId, CancellationToken.None);

        public Task<int> AttemptCount() =>
            Db.RefundAttempts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> ReceiptCount() =>
            Db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public async Task<IReadOnlyList<RefundAttempt>> Attempts() =>
            await Db.RefundAttempts.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    /// <summary>The shared, already-staged case facts a test may open one or more independent scopes over.</summary>
    private sealed record Fixture(
        string DbName,
        Guid ReturnRequestId,
        Guid ReturnItemId,
        Guid OrderLineId,
        decimal Requested,
        decimal UnitPrice,
        OrderReturnContextSnapshot OrderSnapshot)
    {
        public Harness Open() => new(this, CreateDb(DbName));
    }

    private static async Task<Fixture> SeedAsync(Staged stage)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var db = CreateDb(dbName);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();
        const decimal requested = 2m;
        const decimal unitPrice = 1000m;

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            SellerPartyId,
            customer,
            $"tb275-{returnRequestId:N}",
            "دلیل مرجوعی",
            "IRR",
            [(lineId, requested, unitPrice, (Guid?)reservationId)],
            Now,
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);

        // Every staged case is financially progressed: it carries the ONE durable approval decision the money path
        // requires (TB-273-R1 A), so the retry/receiving checks below are exercised on real stored evidence. A rejected
        // stage carries the honest rejecting decision instead.
        if (stage == Staged.RejectedWithPartialReceipt)
        {
            request.Reject(Now);
            db.ReturnDecisions.Add(new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = ReturnRecoveryPolicy.RejectedDecision,
                Reason = "کالا مطابق سیاست مرجوعی نیست.",
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            });
        }
        else
        {
            db.ReturnDecisions.Add(new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = ReturnRecoveryPolicy.ApprovedDecision,
                Reason = null,
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            });
        }

        var paymentId = Guid.NewGuid();
        switch (stage)
        {
            case Staged.PartiallyReceivedPendingMoney:
            case Staged.CompleteReceiptPendingMoney:
            case Staged.PartiallyReceivedDisposed:
                request.Approve(paymentId, Now);
                request.MarkRefundProcessing(Now);
                var pending = request.BeginRefundAttempt(
                    Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
                if (stage == Staged.PartiallyReceivedDisposed)
                {
                    pending.MarkSucceeded("ref-275", Now);
                    request.MarkRefundSucceeded(Now);
                }

                db.RefundAttempts.Add(pending);
                break;

            case Staged.DefiniteFailureWithReceipt:
                request.Approve(paymentId, Now);
                request.MarkRefundProcessing(Now);
                var failed = request.BeginRefundAttempt(
                    Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
                failed.MarkFailed("REFUND_DECLINED", Now);
                request.MarkRefundFailed(Now);
                db.RefundAttempts.Add(failed);
                break;

            case Staged.SucceededWithReceipt:
                request.Approve(paymentId, Now);
                request.MarkRefundProcessing(Now);
                var completedAttempt = request.BeginRefundAttempt(
                    Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
                completedAttempt.MarkSucceeded("ref-275", Now);
                request.MarkRefundSucceeded(Now);
                db.RefundAttempts.Add(completedAttempt);
                break;

            case Staged.AmbiguousFailuresWithReceipt:
                request.Approve(paymentId, Now);
                request.MarkRefundProcessing(Now);
                var firstFailure = request.BeginRefundAttempt(
                    Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}-1", Now);
                firstFailure.MarkFailed("REFUND_DECLINED", Now);
                request.MarkRefundFailed(Now);
                request.MarkRefundProcessing(Now);
                var secondFailure = request.BeginRefundAttempt(
                    Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}-2", Now);
                secondFailure.MarkFailed("REFUND_DECLINED", Now);
                request.MarkRefundFailed(Now);
                db.RefundAttempts.AddRange([firstFailure, secondFailure]);
                break;
        }

        // The recorded physical receipt of each stage: partial or complete, never invented after a disposition.
        var received = stage is Staged.PartiallyReceivedPendingMoney
            or Staged.PartiallyReceivedDisposed
            or Staged.RejectedWithPartialReceipt
            ? 1m
            : 2m;
        var receipt = ReturnReceipt.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            returnRequestId,
            $"tb275-delivery-1-{stage}",
            Guid.NewGuid(),
            Now,
            [(request.Items.Single().ReturnItemId, received, ReturnItemCondition.Usable)]);
        db.ReturnReceipts.Add(receipt);
        db.ReturnReceiptLines.AddRange(receipt.Lines);
        if (stage == Staged.PartiallyReceivedDisposed)
        {
            request.Items.Single().MarkDisposition(ReturnItemDisposition.SaleableRestock);
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await db.DisposeAsync();

        var orderSnapshot = new OrderReturnContextSnapshot(
            orderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            true,
            "IRR",
            [new OrderReturnLineSnapshot(lineId, requested, unitPrice, "IRR", reservationId, true, 7, null, null, QuantityDecimalPlacesSnapshot: 0)]);

        return new Fixture(
            dbName,
            returnRequestId,
            request.Items.Single().ReturnItemId,
            lineId,
            requested,
            unitPrice,
            orderSnapshot);
    }

    private static ReturnsDbContext CreateDb(string name) => new(
        new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FixedOrderReader(OrderReturnContextSnapshot snapshot) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderReturnContextSnapshot?>(
                snapshot.SellerOrderId == sellerOrderId ? snapshot : null);
    }

    private sealed class FixedFulfillmentReader(OrderReturnContextSnapshot order, Guid lineId)
        : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentReturnEligibilitySnapshot?>(
                new FulfillmentReturnEligibilitySnapshot(
                    sellerOrderId,
                    new Dictionary<Guid, decimal> { [lineId] = order.Lines.Single().Quantity },
                    Now,
                    new Dictionary<Guid, DateTimeOffset> { [lineId] = Now },
                    [new LineDeliverySlice(lineId, order.Lines.Single().Quantity, Now)]));

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, DateTimeOffset?> map = sellerOrderIds
                .Where(x => x == order.SellerOrderId)
                .ToDictionary(x => x, _ => (DateTimeOffset?)Now);
            return Task.FromResult(map);
        }
    }

    private sealed class FixedPaymentReader(decimal amount) : IPaymentReturnReader
    {
        private readonly Guid _paymentId = Guid.NewGuid();

        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid paymentId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(
                    paymentId == Guid.Empty ? _paymentId : paymentId,
                    Guid.NewGuid(),
                    amount,
                    "IRR",
                    "Succeeded"));

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(_paymentId, checkoutId, amount, "IRR", "Succeeded"));
    }

    private sealed class ScriptedGateway : IPaymentRefundGateway
    {
        public List<(Guid PaymentId, decimal Amount, string Currency, string IdempotencyKey)> Calls { get; } = [];

        /// <summary>Scripted outcome of the NEXT provider call, so a retry-of-a-retry can be proven to fail again.</summary>
        public bool Succeed { get; set; } = true;

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((paymentId, amount, currency, idempotencyKey));
            return Task.FromResult(Succeed
                ? new GatewayRefundResult(true, $"ref-275-{Calls.Count}", null)
                : new GatewayRefundResult(false, null, "REFUND_DECLINED"));
        }
    }

    private sealed class ScriptedWallet : IWalletRefundCreditPort
    {
        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletRefundCreditResultDto(amount, false));
    }

    private sealed class ScriptedInventory : IReturnInventoryGateway
    {
        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>Only the line read the admin detail needs; every other grid read is not part of this surface.</summary>
    private sealed class FixedGridEnrichment(Guid lineId, Guid variantId, string? unitCode) : IOrderGridEnrichmentReader
    {
        public Task<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>> GetLinesAsync(
            IReadOnlyList<Guid> orderLineIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>>(
                orderLineIds.Contains(lineId) && variantId != Guid.Empty
                    ? new Dictionary<Guid, OrderGridLineSnapshot>
                    {
                        [lineId] = new(lineId, variantId, unitCode, 0, null, true, 7),
                    }
                    : new Dictionary<Guid, OrderGridLineSnapshot>());

        public Task<IReadOnlyDictionary<Guid, string>> GetOrderNumbersAsync(
            IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> SearchSellerOrderIdsByOrderNumberAsync(
            string term, int take, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> FilterSellerOrderIdsByOrderNumberAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, string>> GetRecipientNamesAsync(
            IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> SearchCheckoutIdsByRecipientAsync(
            string term, int take, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> FilterCheckoutIdsByRecipientAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>The Catalog title answer; a null variant is "no title known", never a placeholder.</summary>
    private sealed class FixedCatalogTitles(Guid? variantId, string? title) : ICatalogVariantLookup
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                variantId is not null && title is not null && variantIds.Contains(variantId.Value)
                    ? new Dictionary<Guid, string> { [variantId.Value] = title }
                    : new Dictionary<Guid, string>());

        public Task<CatalogVariantLookupResult?> FindVariantAsync(
            Guid variantId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
