using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Application.Validation;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-274 — the customer receipt/inspection fact of a delivered-customer RMA and the gates it now owns.
/// <para>
/// The facts below are the ones the task asked for: one Returns-owned receipt occurrence per delivery with per-line
/// received/usable/damaged quantities (partial and mixed, never over-received, never split), an exact replay under the
/// same occurrence key with a typed conflict on a changed intent, ZERO money/stock effects from recording, and ONE
/// shared receiving authority that an ordinary approval, a new refund attempt and a restock must pass. Only the
/// INSPECTED usable quantity may become saleable stock, and only after the money is confirmed; a damaged unit never
/// reaches saleable inventory. A rejection carries its reviewed reason in the same durable decision row.
/// </para>
/// </summary>
public sealed class ReturnsCustomerReceipts274Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T09:00:00Z");

    // ------------------------------------------------------------------ D: the pure receiving authority

    [Fact]
    public void The_receiving_summary_is_pure_and_fail_closed()
    {
        var item = Guid.NewGuid();

        // A partial delivery is visible as a remainder, never rounded up to "received".
        var partial = ReturnReceivingSummary.Describe([(item, 3m)], [(item, 2m, ReturnItemCondition.Usable)]);
        Assert.True(partial.HasLines);
        Assert.False(partial.IsComplete);
        Assert.True(partial.IsCoherent);
        var line = Assert.Single(partial.Lines);
        Assert.Equal(3m, line.Requested);
        Assert.Equal(2m, line.Received);
        Assert.Equal(2m, line.Usable);
        Assert.Equal(0m, line.Damaged);
        Assert.Equal(1m, partial.Remaining);

        // Mixed inspection on the SAME requested line: usable and damaged are separate quantities of one item.
        var mixed = ReturnReceivingSummary.Describe(
            [(item, 2m)],
            [(item, 1m, ReturnItemCondition.Usable), (item, 1m, ReturnItemCondition.Damaged)]);
        Assert.True(mixed.IsComplete);
        Assert.True(mixed.IsCoherent);
        Assert.Equal(2m, mixed.Received);
        Assert.Equal(1m, mixed.Usable);
        Assert.Equal(1m, mixed.Damaged);
        Assert.Equal(0m, mixed.Remaining);

        // A receipt naming a line this case does not own, or receiving more than was requested, is a contradiction
        // that can never read as "complete".
        var foreign = ReturnReceivingSummary.Describe([(item, 1m)], [(Guid.NewGuid(), 1m, ReturnItemCondition.Usable)]);
        Assert.False(foreign.IsCoherent);
        Assert.False(foreign.IsComplete);
        Assert.Equal(0m, Assert.Single(foreign.Lines).Received);

        var overReceived = ReturnReceivingSummary.Describe([(item, 1m)], [(item, 2m, ReturnItemCondition.Usable)]);
        Assert.False(overReceived.IsCoherent);
        Assert.False(overReceived.IsComplete);

        // An empty case has no completeness fact at all.
        var empty = ReturnReceivingSummary.Describe([], []);
        Assert.False(empty.HasLines);
        Assert.False(empty.IsComplete);
        Assert.True(empty.IsCoherent);
    }

    [Fact]
    public void The_receipt_aggregate_refuses_every_impossible_construction()
    {
        var item = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        Assert.Equal("return.receipt.occurrence_required", Assert.Throws<InvalidOperationException>(() =>
            ReturnReceipt.Create(Guid.NewGuid(), () => Guid.NewGuid(), requestId, "   ", Guid.NewGuid(), Now,
                [(item, 1m, ReturnItemCondition.Usable)])).Message);

        Assert.Equal("return.receipt.line_invalid", Assert.Throws<InvalidOperationException>(() =>
            ReturnReceipt.Create(Guid.NewGuid(), () => Guid.NewGuid(), requestId, "occ", Guid.NewGuid(), Now, []))
            .Message);

        // Same (item, condition) twice inside ONE occurrence would double-count a physical unit.
        Assert.Equal("return.receipt.line_invalid", Assert.Throws<InvalidOperationException>(() =>
            ReturnReceipt.Create(Guid.NewGuid(), () => Guid.NewGuid(), requestId, "occ", Guid.NewGuid(), Now,
                [
                    (item, 1m, ReturnItemCondition.Usable),
                    (item, 1m, ReturnItemCondition.Usable),
                ])).Message);

        // The persisted six-decimal scale is a hard boundary, not a rounding suggestion.
        Assert.Equal("return.receipt.line_invalid", Assert.Throws<InvalidOperationException>(() =>
            ReturnReceipt.Create(Guid.NewGuid(), () => Guid.NewGuid(), requestId, "occ", Guid.NewGuid(), Now,
                [(item, 1.0000001m, ReturnItemCondition.Usable)])).Message);

        var receipt = ReturnReceipt.Create(Guid.NewGuid(), () => Guid.NewGuid(), requestId, " occurrence ", Guid.NewGuid(), Now,
            [(item, 1m, ReturnItemCondition.Usable), (item, 1m, ReturnItemCondition.Damaged)]);
        Assert.Equal("occurrence", receipt.OccurrenceKey);
        Assert.Equal(2, receipt.Lines.Count);
    }

    [Fact]
    public void The_transport_validator_refuses_blank_occurrence_empty_lines_and_duplicate_conditions()
    {
        var validator = new RecordReturnReceiptCommandValidator();
        var item = Guid.NewGuid();

        var blankKey = validator.Validate(new RecordReturnReceiptCommand(
            Guid.NewGuid(), Guid.NewGuid(), " ", [new CustomerReceiptLine(item, 1m)]));
        Assert.False(blankKey.IsValid);
        Assert.Contains(blankKey.Errors, x => x.ErrorCode == ReturnsValidationCodes.ReceiptOccurrenceRequired);

        var noLines = validator.Validate(new RecordReturnReceiptCommand(
            Guid.NewGuid(), Guid.NewGuid(), "occ", []));
        Assert.False(noLines.IsValid);
        Assert.Contains(noLines.Errors, x => x.ErrorCode == ReturnsValidationCodes.ReceiptLinesEmpty);

        var duplicate = validator.Validate(new RecordReturnReceiptCommand(
            Guid.NewGuid(), Guid.NewGuid(), "occ",
            [new CustomerReceiptLine(item, 1m), new CustomerReceiptLine(item, 1m)]));
        Assert.False(duplicate.IsValid);
        Assert.Contains(duplicate.Errors, x => x.ErrorCode == ReturnsValidationCodes.ReceiptDuplicateLine);

        var imprecise = validator.Validate(new RecordReturnReceiptCommand(
            Guid.NewGuid(), Guid.NewGuid(), "occ", [new CustomerReceiptLine(item, 1.0000001m)]));
        Assert.False(imprecise.IsValid);
        Assert.Contains(imprecise.Errors, x => x.ErrorCode == ReturnsValidationCodes.ReceiptQuantityInvalid);

        Assert.True(validator.Validate(new RecordReturnReceiptCommand(
            Guid.NewGuid(), Guid.NewGuid(), "occ",
            [new CustomerReceiptLine(item, 1m, ReturnItemCondition.Usable)]))
            .IsValid);
    }

    [Fact]
    public void The_wire_condition_token_is_parsed_and_an_undefined_token_is_a_stable_refusal()
    {
        // Absent means the documented default, never an undefined condition.
        Assert.Equal(ReturnItemCondition.Usable, ReturnItemConditionParser.Parse(null).Value);
        Assert.Equal(ReturnItemCondition.Usable, ReturnItemConditionParser.Parse("  ").Value);

        // The two defined tokens parse (case/space tolerant, as the recovery-kind parser already is).
        Assert.Equal(ReturnItemCondition.Usable, ReturnItemConditionParser.Parse("Usable").Value);
        Assert.Equal(ReturnItemCondition.Damaged, ReturnItemConditionParser.Parse(" DAMAGED ").Value);

        // A typo is a typed refusal, so a damaged unit can never be silently received as saleable.
        var unknown = ReturnItemConditionParser.Parse("broken");
        Assert.True(unknown.IsFailure);
        Assert.Equal(ReturnsErrorCodes.ReceiptLineInvalid, unknown.FirstError.Code);
    }

    // ------------------------------------------------------------------ A: the Returns-owned receipt fact

    [Fact]
    public async Task A_recorded_receipt_is_the_physical_fact_and_moves_no_money_or_stock()
    {
        await using var order = await SeedAsync(requested: 2m, reservationId: Guid.NewGuid());

        var recorded = await order.RecordAsync("delivery-1",
            (order.ReturnItemId, 2m, ReturnItemCondition.Usable));

        // The physical fact is stored and described from stored rows only.
        Assert.Equal(ReturnRequestStatus.Requested, recorded.Status);
        var receiving = Assert.IsType<ReturnReceivingSummary>(recorded.Receiving);
        Assert.True(receiving.IsComplete);
        Assert.True(receiving.IsCoherent);
        Assert.Equal(2m, receiving.Usable);
        Assert.Equal(0m, receiving.Damaged);
        Assert.Equal(1, await order.ReceiptCount());
        Assert.Equal(1, await order.ReceiptLineCount());

        // Recording is a statement, not a decision: no approval, no attempt, no money, no stock.
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(0, await order.AttemptCount());
        Assert.Equal(0, await order.DecisionCount());
        Assert.Equal(ReturnItemDisposition.None, (await order.Items()).Single().Disposition);
    }

    [Fact]
    public async Task Partial_occurrences_accumulate_without_over_receiving()
    {
        await using var order = await SeedAsync(requested: 3m);

        var first = await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));
        var partial = Assert.IsType<ReturnReceivingSummary>(first.Receiving);
        Assert.False(partial.IsComplete);
        Assert.Equal(1m, partial.Received);
        Assert.Equal(2m, partial.Remaining);

        // A second delivery under its OWN occurrence identity: the requested line is not split, only covered.
        var second = await order.RecordAsync("delivery-2", (order.ReturnItemId, 2m, ReturnItemCondition.Usable));
        var complete = Assert.IsType<ReturnReceivingSummary>(second.Receiving);
        Assert.True(complete.IsComplete);
        Assert.Equal(3m, complete.Received);
        Assert.Equal(2, await order.ReceiptCount());

        // A third occurrence would exceed what the customer was asked to return: refused with zero write.
        var over = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-3", (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptOverReceived, over.Code);
        Assert.Equal(2, await order.ReceiptCount());
        Assert.Equal(2, await order.ReceiptLineCount());
    }

    [Fact]
    public async Task Mixed_usable_and_damaged_in_one_occurrence_stays_one_requested_line()
    {
        await using var order = await SeedAsync(requested: 2m, reservationId: Guid.NewGuid());

        var recorded = await order.RecordAsync(
            "delivery-1",
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable),
            (order.ReturnItemId, 1m, ReturnItemCondition.Damaged));

        var receiving = Assert.IsType<ReturnReceivingSummary>(recorded.Receiving);
        Assert.True(receiving.IsComplete);
        Assert.Equal(1m, receiving.Usable);
        Assert.Equal(1m, receiving.Damaged);
        Assert.Equal(2m, receiving.Received);

        // Exactly one requested line still exists, and nothing was decided or stocked by the inspection.
        Assert.Single(await order.Items());
        Assert.Equal(0, await order.DecisionCount());
        Assert.Empty(order.Inventory.Restocked);
    }

    [Fact]
    public async Task An_exact_replay_answers_the_stored_receipt_and_a_changed_intent_is_a_conflict()
    {
        await using var order = await SeedAsync(requested: 2m);

        var stored = await order.RecordAsync(
            "delivery-1",
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable),
            (order.ReturnItemId, 1m, ReturnItemCondition.Damaged));

        // The same reviewed occurrence with the same inspected intent replays the stored fact: nothing is doubled.
        var replay = await order.RecordAsync(
            " delivery-1 ",
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable),
            (order.ReturnItemId, 1m, ReturnItemCondition.Damaged));
        Assert.Equal(1, await order.ReceiptCount());
        Assert.Equal(2, await order.ReceiptLineCount());
        Assert.Equal(
            Assert.IsType<ReturnReceivingSummary>(stored.Receiving).Usable,
            Assert.IsType<ReturnReceivingSummary>(replay.Receiving).Usable);

        // The SAME identity carrying a different inspected quantity is never a replay.
        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-1", (order.ReturnItemId, 2m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptConflict, conflict.Code);
        Assert.Equal(2, await order.ReceiptLineCount());
    }

    [Fact]
    public async Task A_courier_origin_case_never_records_a_second_receipt_here()
    {
        await using var order = await SeedCourierAsync();

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "courier-again", (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptOriginInvalid, refused.Code);
        Assert.Equal(0, await order.ReceiptCount());
    }

    [Fact]
    public async Task A_decided_case_never_accepts_a_new_physical_receipt()
    {
        await using var order = await SeedAsync(requested: 1m);
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));
        await order.ApproveAsync();

        // The approval already recorded the physical facts its money was based on; a later receipt may not move them.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-2", (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptStateInvalid, refused.Code);
        Assert.Equal(1, await order.ReceiptCount());
    }

    [Fact]
    public async Task Invalid_receipt_shapes_are_refused_before_any_write()
    {
        await using var order = await SeedAsync(requested: 2m);
        var foreignItem = Guid.NewGuid();

        var blank = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "  ", (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptOccurrenceRequired, blank.Code);

        var empty = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync("delivery-1"));
        Assert.Equal(ReturnsErrorCodes.ReceiptLineInvalid, empty.Code);

        var foreign = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-1", (foreignItem, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptLineInvalid, foreign.Code);

        var zero = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-1", (order.ReturnItemId, 0m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptLineInvalid, zero.Code);

        var imprecise = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-1", (order.ReturnItemId, 1.0000001m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptLineInvalid, imprecise.Code);

        var duplicate = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-1",
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable),
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptLineInvalid, duplicate.Code);

        // Not one refused attempt left a partial receipt behind.
        Assert.Equal(0, await order.ReceiptCount());
        Assert.Equal(0, await order.ReceiptLineCount());
        Assert.Equal(ReturnRequestStatus.Requested, (await order.Snapshot())!.Status);
    }

    // ------------------------------------------------------------------ B: one shared receiving gate

    [Fact]
    public async Task Approval_without_receiving_is_refused_with_zero_money_and_zero_attempt()
    {
        await using var order = await SeedAsync(requested: 2m);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.ApproveAsync());
        Assert.Equal(ReturnsErrorCodes.ReceivingIncomplete, refused.Code);

        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(0, await order.AttemptCount());
        Assert.Equal(0, await order.DecisionCount());
        Assert.Equal(ReturnRequestStatus.Requested, (await order.Snapshot())!.Status);
    }

    [Fact]
    public async Task A_partial_receipt_does_not_authorize_approval()
    {
        await using var order = await SeedAsync(requested: 2m);
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.ApproveAsync());
        Assert.Equal(ReturnsErrorCodes.ReceivingIncomplete, refused.Code);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(0, await order.AttemptCount());

        // Completing the physical coverage is what authorizes the SAME approval.
        await order.RecordAsync("delivery-2", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));
        var approved = await order.ApproveAsync();

        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);
        var call = Assert.Single(order.Gateway.Calls);
        Assert.Equal(2000m, call.Amount);
        Assert.Equal(1, await order.AttemptCount());
    }

    [Fact]
    public async Task Approval_over_a_complete_receipt_restocks_only_the_inspected_usable_quantity()
    {
        await using var order = await SeedAsync(requested: 3m, reservationId: Guid.NewGuid());

        await order.RecordAsync(
            "delivery-1",
            (order.ReturnItemId, 2m, ReturnItemCondition.Usable),
            (order.ReturnItemId, 1m, ReturnItemCondition.Damaged));

        var approved = await order.ApproveAsync();
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);

        // ONLY the inspected usable quantity becomes saleable stock, once, under the owning per-item key.
        var restock = Assert.Single(order.Inventory.Restocked);
        Assert.Equal(2m, restock.Quantity);
        Assert.Equal(
            $"return-restock-{order.ReturnRequestId:N}-{order.ReturnItemId:N}",
            restock.IdempotencyKey);

        var item = Assert.Single(await order.Items());
        Assert.Equal(ReturnItemDisposition.SaleableRestock, item.Disposition);
        Assert.Single(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);

        // The inspection decided the disposition: nothing damaged became saleable and only one attempt exists.
        Assert.Equal(1, await order.AttemptCount());
    }

    [Fact]
    public async Task A_fully_damaged_receipt_never_calls_saleable_inventory()
    {
        await using var order = await SeedAsync(requested: 2m, reservationId: Guid.NewGuid());
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 2m, ReturnItemCondition.Damaged));

        var approved = await order.ApproveAsync();
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);

        // The money is confirmed, but a damaged unit is never saleable stock: the line is non-saleable and Inventory
        // is never called at all.
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(
            ReturnItemDisposition.DamagedNonSaleable,
            (await order.Items()).Single().Disposition);
        Assert.Single(order.Gateway.Calls);
    }

    [Fact]
    public async Task A_new_attempt_retry_is_gated_by_the_same_receiving_authority()
    {
        await using var order = await SeedAsync(requested: 2m, stage: Staged.FailedMoneyWithoutReceipt);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RetryAsync());
        Assert.Equal(ReturnsErrorCodes.ReceivingIncomplete, refused.Code);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(1, await order.AttemptCount());
    }

    [Fact]
    public async Task Physical_recovery_after_confirmed_money_restocks_only_usable_units()
    {
        await using var order = await SeedAsync(requested: 2m, reservationId: Guid.NewGuid());

        await order.RecordAsync(
            "delivery-1",
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable),
            (order.ReturnItemId, 1m, ReturnItemCondition.Damaged));

        // The owning restock fails once during approval, so the money is confirmed while the physical phase stays open.
        order.Inventory.FailuresRemaining = 1;
        await order.ApproveAsync();

        Assert.Equal(ReturnItemDisposition.None, (await order.Items()).Single().Disposition);
        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, read.Phase);
        Assert.Equal(ReturnRecoveryKind.SettlePhysicalDisposition, read.RecoveryKind);
        Assert.True(read.FinancialComplete);

        // The physical intent restocks ONLY the usable unit, with zero money calls and no new attempt.
        var recovered = await order.RecoverAsync(ReturnRecoveryKind.SettlePhysicalDisposition);
        var restock = Assert.Single(order.Inventory.Restocked);
        Assert.Equal(1m, restock.Quantity);
        Assert.Equal(ReturnItemDisposition.SaleableRestock, (await order.Items()).Single().Disposition);
        Assert.Single(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Equal(ReturnRecoveryPhase.Settled, recovered.Recovery!.Phase);
    }

    [Fact]
    public async Task An_incoherent_stored_receipt_never_authorizes_approval_or_restock()
    {
        await using var order = await SeedAsync(requested: 2m, reservationId: Guid.NewGuid());
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 2m, ReturnItemCondition.Usable));

        // A contradictory stored row (more units than requested) that the write boundary itself would have refused.
        var storedLine = await order.Db.ReturnReceiptLines.AsTracking().SingleAsync();
        order.Db.Entry(storedLine).Property(x => x.Quantity).CurrentValue = 5m;
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        Assert.False((await order.Snapshot())!.Receiving!.IsCoherent);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.ApproveAsync());
        Assert.Equal(ReturnsErrorCodes.ReceivingIncomplete, refused.Code);
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(0, await order.AttemptCount());
    }

    // ------------------------------------------------------------------ C: the durable rejection reason

    [Fact]
    public async Task A_new_rejection_requires_a_meaningful_reason()
    {
        await using var order = await SeedAsync(requested: 1m);

        foreach (var reason in new[] { null, "", "   " })
        {
            var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RejectAsync(reason));
            Assert.Equal(ReturnsErrorCodes.RejectionReasonRequired, refused.Code);
        }

        Assert.Equal(0, await order.DecisionCount());
        Assert.Equal(ReturnRequestStatus.Requested, (await order.Snapshot())!.Status);

        var tooLong = await Assert.ThrowsAsync<ContractOperationException>(() => order.RejectAsync(new string('x', 513)));
        Assert.Equal(ReturnsErrorCodes.RejectionReasonRequired, tooLong.Code);
        Assert.Equal(0, await order.DecisionCount());
    }

    [Fact]
    public async Task The_winning_rejection_persists_its_trimmed_reason_and_replays_exactly()
    {
        await using var order = await SeedAsync(requested: 1m);

        var rejected = await order.RejectAsync("  بسته آسیب‌دیده بود  ");
        Assert.Equal(ReturnRequestStatus.Rejected, rejected.Status);
        Assert.Equal("بسته آسیب‌دیده بود", rejected.DecisionReason);

        var stored = Assert.Single(await order.Decisions());
        Assert.Equal(ReturnRecoveryPolicy.RejectedDecision, stored.Decision);
        Assert.Equal("بسته آسیب‌دیده بود", stored.Reason);

        // The same decision with the SAME normalized reason replays the stored row: nothing is rewritten.
        var replay = await order.RejectAsync("بسته آسیب‌دیده بود");
        Assert.Equal(ReturnRequestStatus.Rejected, replay.Status);
        Assert.Equal("بسته آسیب‌دیده بود", replay.DecisionReason);
        Assert.Equal(1, await order.DecisionCount());

        // Nothing financial or physical happened for a rejected case.
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(0, await order.AttemptCount());
    }

    [Fact]
    public async Task A_changed_reason_on_the_winning_rejection_is_a_typed_conflict()
    {
        await using var order = await SeedAsync(requested: 1m);
        await order.RejectAsync("دلیل اول");

        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() => order.RejectAsync("دلیل دوم"));
        Assert.Equal(ReturnsErrorCodes.DecisionReasonConflict, conflict.Code);

        // The durable decision keeps its original statement and the case is still the same rejection.
        Assert.Equal("دلیل اول", Assert.Single(await order.Decisions()).Reason);
        Assert.Equal(ReturnRequestStatus.Rejected, (await order.Snapshot())!.Status);
        Assert.Equal(1, await order.DecisionCount());
    }

    [Fact]
    public async Task An_approval_never_records_a_rejection_reason()
    {
        await using var order = await SeedAsync(requested: 1m);
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));

        var approved = await order.ApproveAsync();
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);
        Assert.Null(approved.DecisionReason);

        var decision = Assert.Single(await order.Decisions());
        Assert.Equal(ReturnRecoveryPolicy.ApprovedDecision, decision.Decision);
        Assert.Null(decision.Reason);
    }

    // ------------------------------------------------------------------ legacy completion unblocked by real receiving

    [Fact]
    public async Task Real_receiving_may_unblock_a_money_complete_case_without_ever_repeating_its_money()
    {
        await using var order = await SeedAsync(
            requested: 2m, reservationId: Guid.NewGuid(), stage: Staged.MoneyConfirmedWithoutDisposition);

        // Money is already confirmed and the physical side is entirely unsettled.
        Assert.Equal(ReturnRequestStatus.Completed, (await order.Snapshot())!.Status);
        var before = (await order.Snapshot())!.Receiving!;
        Assert.False(before.IsComplete);
        Assert.Equal(0m, before.Received);
        Assert.Equal(1, await order.AttemptCount());

        // The operator records the REAL inspection of the goods that actually arrived.
        var recorded = await order.RecordAsync(
            "delivery-late",
            (order.ReturnItemId, 2m, ReturnItemCondition.Usable));
        Assert.True(recorded.Receiving!.IsComplete);
        Assert.True(recorded.Receiving!.IsCoherent);
        Assert.Empty(order.Inventory.Restocked);

        var described = (await order.Snapshot())!;
        Assert.NotNull(described.Recovery);
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, described.Recovery!.Phase);
        Assert.Equal(ReturnRecoveryKind.SettlePhysicalDisposition, described.Recovery.RecoveryKind);

        // Only the physical intent runs; the confirmed money is never sent again.
        var recovered = await order.RecoverAsync(ReturnRecoveryKind.SettlePhysicalDisposition);
        Assert.Equal(2m, Assert.Single(order.Inventory.Restocked).Quantity);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Equal(ReturnRecoveryPhase.Settled, recovered.Recovery!.Phase);
    }

    [Fact]
    public async Task Unknown_ordinary_money_is_never_resumed_without_a_complete_receipt()
    {
        await using var order = await SeedAsync(
            requested: 2m, reservationId: Guid.NewGuid(), stage: Staged.PendingMoneyWithoutReceipt);

        // The READ states the same blocker the action raises: no kind, so no client can offer an execution.
        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryReceivingRequired, read.BlockerCode);
        Assert.Null(read.RecoveryKind);
        Assert.False(read.FinancialComplete);

        var attemptId = Assert.Single(await order.Attempts()).RefundAttemptId;
        Assert.Equal(attemptId, read.RefundAttemptId);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => order.RecoverAsync(ReturnRecoveryKind.ResumePendingRefund, attemptId));
        Assert.Equal(ReturnsErrorCodes.RecoveryReceivingRequired, refused.Code);

        // Zero effect: no money call, no new key, the SAME attempt stays pending.
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Equal(RefundAttemptStatus.Pending, Assert.Single(await order.Attempts()).Status);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, (await order.Snapshot())!.Status);

        // Real receiving/inspection is the ONLY thing that unblocks the same recorded attempt.
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 2m, ReturnItemCondition.Usable));
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, (await order.Snapshot())!.Recovery!.Phase);

        var recovered = await order.RecoverAsync(ReturnRecoveryKind.ResumePendingRefund, attemptId);
        Assert.Equal(ReturnRequestStatus.Completed, recovered.Status);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Single(order.Gateway.Calls);
        Assert.Equal(2m, Assert.Single(order.Inventory.Restocked).Quantity);
    }

    [Fact]
    public async Task A_settled_physical_history_never_accepts_a_new_receipt()
    {
        await using var order = await SeedAsync(
            requested: 1m, reservationId: Guid.NewGuid(), stage: Staged.MoneyConfirmedWithoutDisposition);

        // A legacy row already carries its own restock disposition: this physical history is durable.
        var item = await order.Db.ReturnItems.AsTracking().SingleAsync();
        item.MarkDisposition(ReturnItemDisposition.SaleableRestock);
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));
        Assert.Equal(ReturnsErrorCodes.ReceiptStateInvalid, refused.Code);
        Assert.Equal(0, await order.ReceiptCount());
    }

    // ------------------------------------------------------------------ fixtures

    private enum Staged
    {
        Requested,
        PendingMoneyWithoutReceipt,
        FailedMoneyWithoutReceipt,
        MoneyConfirmedWithoutDisposition,
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly ReturnsDbContext _db;
        private readonly ReturnDirectory _directory;

        public Harness(
            ReturnsDbContext db,
            ReturnDirectory directory,
            Guid returnRequestId,
            Guid returnItemId,
            ScriptedGateway gateway,
            ScriptedWallet wallet,
            ScriptedInventory inventory)
        {
            _db = db;
            _directory = directory;
            ReturnRequestId = returnRequestId;
            ReturnItemId = returnItemId;
            Gateway = gateway;
            Wallet = wallet;
            Inventory = inventory;
        }

        public ReturnsDbContext Db => _db;

        public Guid ReturnRequestId { get; }

        public Guid ReturnItemId { get; }

        public ScriptedGateway Gateway { get; }

        public ScriptedWallet Wallet { get; }

        public ScriptedInventory Inventory { get; }

        public void ResetRequestScope() => _db.ChangeTracker.Clear();

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

        public Task<ReturnSnapshot> ApproveAsync() =>
            _directory.ApproveAsync(
                new ApproveReturnCommand(ReturnRequestId, Guid.NewGuid(), SellerPartyId),
                CancellationToken.None);

        public Task<ReturnSnapshot> RejectAsync(string? reason) =>
            _directory.RejectAsync(
                new RejectReturnCommand(ReturnRequestId, Guid.NewGuid(), SellerPartyId, reason),
                CancellationToken.None);

        public Task<ReturnSnapshot> RetryAsync() =>
            _directory.RetryRefundAsync(
                new RetryReturnRefundCommand(ReturnRequestId, Guid.NewGuid()),
                CancellationToken.None);

        public Task<ReturnSnapshot> RecoverAsync(ReturnRecoveryKind kind, Guid? expectedAttemptId = null) =>
            _directory.RecoverAsync(
                new RecoverReturnCommand(ReturnRequestId, Guid.NewGuid(), kind, expectedAttemptId),
                CancellationToken.None);

        public Task<ReturnSnapshot?> Snapshot() => _directory.GetAsync(ReturnRequestId, CancellationToken.None);

        public static Guid SellerPartyId { get; } = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

        public Task<int> AttemptCount() =>
            _db.RefundAttempts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> DecisionCount() =>
            _db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public async Task<IReadOnlyList<RefundAttempt>> Attempts() =>
            await _db.RefundAttempts.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public Task<int> ReceiptCount() =>
            _db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> ReceiptLineCount() =>
            _db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public async Task<IReadOnlyList<ReturnDecisionRecord>> Decisions() =>
            await _db.ReturnDecisions.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public async Task<IReadOnlyList<ReturnItem>> Items() =>
            await _db.ReturnItems.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }

    private static async Task<Harness> SeedAsync(
        decimal requested = 1m,
        decimal unitPrice = 1000m,
        Guid? reservationId = null,
        Staged stage = Staged.Requested)
    {
        var db = CreateDb();
        var clock = new MutableClock(Now);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            Harness.SellerPartyId,
            customer,
            $"tb274-{returnRequestId:N}",
            "دلیل مرجوعی",
            "IRR",
            [(lineId, requested, unitPrice, reservationId)],
            Now,
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);

        if (stage == Staged.PendingMoneyWithoutReceipt)
        {
            // A case whose refund answer is UNKNOWN and whose physical side was never recorded: the pre-TB-274 shape.
            db.ReturnDecisions.Add(new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = ReturnRecoveryPolicy.ApprovedDecision,
                Reason = null,
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            });
            var paymentId = Guid.NewGuid();
            request.Approve(paymentId, Now);
            request.MarkRefundProcessing(Now);
            var pendingAttempt = request.BeginRefundAttempt(
                Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
            db.RefundAttempts.Add(pendingAttempt);
        }
        else if (stage == Staged.FailedMoneyWithoutReceipt)
        {
            // The durable shape of a definite refund failure recorded BEFORE TB-274 existed: no receipt, no reason.
            db.ReturnDecisions.Add(new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = ReturnRecoveryPolicy.ApprovedDecision,
                Reason = null,
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            });
            var paymentId = Guid.NewGuid();
            request.Approve(paymentId, Now);
            request.MarkRefundProcessing(Now);
            var attempt = request.BeginRefundAttempt(Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
            attempt.MarkFailed("REFUND_DECLINED", Now);
            request.MarkRefundFailed(Now);
            db.RefundAttempts.Add(attempt);
        }
        else if (stage == Staged.MoneyConfirmedWithoutDisposition)
        {
            // A case whose money was confirmed BEFORE TB-274 existed: completed, no receipt and no disposition.
            db.ReturnDecisions.Add(new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = ReturnRecoveryPolicy.ApprovedDecision,
                Reason = null,
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            });
            var paymentId = Guid.NewGuid();
            request.Approve(paymentId, Now);
            request.MarkRefundProcessing(Now);
            var attempt = request.BeginRefundAttempt(Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
            attempt.MarkSucceeded("ref-274-legacy", Now);
            request.MarkRefundSucceeded(Now);
            db.RefundAttempts.Add(attempt);
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var gateway = new ScriptedGateway();
        var wallet = new ScriptedWallet();
        var inventory = new ScriptedInventory();
        var order = BuildOrder(orderId, customer, lineId, requested, unitPrice, reservationId);
        var orderReader = new FixedOrderReader(order);
        var directory = new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orderReader,
            new ReturnEligibilityEvaluator(
                orderReader,
                new FixedFulfillmentReader(order, lineId),
                db,
                clock),
            new FixedPaymentReader(requested * unitPrice),
            gateway,
            wallet,
            inventory,
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());

        return new Harness(db, directory, returnRequestId, request.Items.Single().ReturnItemId, gateway, wallet, inventory);
    }

    private static async Task<Harness> SeedCourierAsync()
    {
        var db = CreateDb();
        var clock = new MutableClock(Now);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();

        var request = ReturnRequest.CreateCourierOrigin(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            Harness.SellerPartyId,
            customer,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "courier-return:tb274",
            $"tb274-courier-{returnRequestId:N}",
            "بازگشت مرسوله",
            "IRR",
            [(lineId, 1m, 1000m, (Guid?)null, ReturnItemCondition.Usable)],
            Now);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var gateway = new ScriptedGateway();
        var wallet = new ScriptedWallet();
        var inventory = new ScriptedInventory();
        var order = BuildOrder(orderId, customer, lineId, 1m, 1000m, null);
        var orderReader = new FixedOrderReader(order);
        var directory = new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orderReader,
            new ReturnEligibilityEvaluator(
                orderReader,
                new FixedFulfillmentReader(order, lineId),
                db,
                clock),
            new FixedPaymentReader(1000m),
            gateway,
            wallet,
            inventory,
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());

        return new Harness(db, directory, returnRequestId, request.Items.Single().ReturnItemId, gateway, wallet, inventory);
    }

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid customerUserId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        Guid? reservationId) =>
        new(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerUserId,
            true,
            "IRR",
            [new OrderReturnLineSnapshot(lineId, quantity, unitPrice, "IRR", reservationId, true, 7)]);

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            // The owning decision write opens a transaction unconditionally; the in-memory harness declares that a
            // no-op instead of reading the provider warning as a failure.
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ReturnsDbContext(options);
    }

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

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((paymentId, amount, currency, idempotencyKey));
            return Task.FromResult(new GatewayRefundResult(true, $"ref-274-{Calls.Count}", null));
        }
    }

    private sealed class ScriptedWallet : IWalletRefundCreditPort
    {
        public List<(Guid ReturnRequestId, decimal Amount, string IdempotencyKey)> Calls { get; } = [];

        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((returnRequestId, amount, idempotencyKey));
            return Task.FromResult(new WalletRefundCreditResultDto(amount, false));
        }
    }

    private sealed class ScriptedInventory : IReturnInventoryGateway
    {
        public int FailuresRemaining { get; set; }

        public List<(Guid ReservationId, decimal Quantity, string IdempotencyKey)> Restocked { get; } = [];

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            if (FailuresRemaining > 0)
            {
                FailuresRemaining--;
                throw new InvalidOperationException("tb274 simulated inventory restock failure");
            }

            if (!Restocked.Any(x => string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)))
            {
                Restocked.Add((reservationId, quantity, idempotencyKey));
            }

            return Task.CompletedTask;
        }
    }
}
