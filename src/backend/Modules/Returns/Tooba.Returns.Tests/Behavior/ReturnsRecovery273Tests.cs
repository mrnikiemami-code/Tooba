using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
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
/// TB-273 — recovery of ONE already-approved case at its real owning boundary (<see cref="ReturnDirectory"/>).
/// <para>
/// Every case is seeded at the durable state an approval already recorded, so the only thing under test is the
/// recovery intent itself. <c>ResumePendingRefund</c> repeats the EXISTING attempt with its recorded
/// key/payment/amount/currency and never mints a new one; <c>SettlePhysicalDisposition</c> settles stock with zero
/// money calls. A definite failure belongs to the existing new-attempt retry, an unknown answer stays unknown with
/// the SAME attempt, nothing is written before the external answer, and an applied outcome is monotonic.
/// </para>
/// </summary>
public sealed class ReturnsRecovery273Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T08:00:00Z");

    // ------------------------------------------------------------------ A: the read policy is pure and phase-honest

    [Fact]
    public void The_read_policy_separates_money_from_physical_completion()
    {
        // Nothing approved: there is no financial or physical recovery to describe.
        Assert.Equal(ReturnRecoveryPhase.None, Describe(Snap(ReturnRequestStatus.Requested)).Phase);
        Assert.Equal(ReturnRecoveryPhase.None, Describe(Snap(ReturnRequestStatus.Rejected)).Phase);
        Assert.Equal(ReturnRecoveryPhase.None, Describe(Snap(ReturnRequestStatus.Cancelled)).Phase);
        Assert.Null(Describe(Snap(ReturnRequestStatus.Requested)).RecoveryKind);

        // One bound pending attempt: the money answer is UNKNOWN and only that exact attempt may be resumed.
        var attemptId = Guid.NewGuid();
        var pending = Describe(Snap(
            ReturnRequestStatus.RefundProcessing,
            attempts: [(attemptId, RefundAttemptStatus.Pending)],
            receivingComplete: true));
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, pending.Phase);
        Assert.Equal(ReturnRecoveryKind.ResumePendingRefund, pending.RecoveryKind);
        Assert.Equal(attemptId, pending.RefundAttemptId);
        Assert.False(pending.FinancialComplete);
        Assert.Null(pending.BlockerCode);

        // TB-274 (B): the SAME unknown ordinary money over NO recorded physical evidence is a typed blocker — the
        // unknown attempt is never repeated around the receiving/inspection rule that gates every ordinary money path.
        var pendingWithoutReceipt = Describe(Snap(
            ReturnRequestStatus.RefundProcessing,
            attempts: [(attemptId, RefundAttemptStatus.Pending)]));
        Assert.Equal(ReturnRecoveryPhase.Blocked, pendingWithoutReceipt.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryReceivingRequired, pendingWithoutReceipt.BlockerCode);
        Assert.Null(pendingWithoutReceipt.RecoveryKind);

        // Several pending attempts are an ambiguous binding, never a guessed resume.
        var ambiguous = Describe(Snap(
            ReturnRequestStatus.Approved,
            attempts:
            [
                (Guid.NewGuid(), RefundAttemptStatus.Pending),
                (Guid.NewGuid(), RefundAttemptStatus.Pending),
            ]));
        Assert.Equal(ReturnRecoveryPhase.Blocked, ambiguous.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptUnbound, ambiguous.BlockerCode);

        // A definite failure is its own phase and is never reported as unknown money.
        var failed = Describe(Snap(
            ReturnRequestStatus.RefundFailed, attempts: [(Guid.NewGuid(), RefundAttemptStatus.Failed)]));
        Assert.Equal(ReturnRecoveryPhase.FailedMoney, failed.Phase);
        Assert.Null(failed.RecoveryKind);
    }

    [Fact]
    public void Confirmed_money_with_unsettled_receipts_is_described_by_origin_not_by_status()
    {
        var succeeded = (Guid.NewGuid(), RefundAttemptStatus.Succeeded);

        // A courier-origin case OWNS its receipt, so the unfinished physical phase is executable work.
        var courier = Describe(Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [succeeded],
            dispositions: [ReturnItemDisposition.None]));
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, courier.Phase);
        Assert.Equal(ReturnRecoveryKind.SettlePhysicalDisposition, courier.RecoveryKind);
        Assert.True(courier.FinancialComplete);
        Assert.False(courier.PhysicalComplete);

        // An ordinary delivered RMA has NO receiving evidence, so the same stored shape is an explicit blocker.
        var ordinary = Describe(Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CustomerDelivered,
            attempts: [succeeded],
            dispositions: [ReturnItemDisposition.None]));
        Assert.Equal(ReturnRecoveryPhase.Blocked, ordinary.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryReceivingRequired, ordinary.BlockerCode);
        Assert.Null(ordinary.RecoveryKind);

        // Every received line disposed AND the owning final closure recorded is the completion.
        var settled = Describe(Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [succeeded],
            dispositions: [ReturnItemDisposition.SaleableRestock],
            closurePhase: CourierOriginClosurePhase.RefundReleased));
        Assert.Equal(ReturnRecoveryPhase.Settled, settled.Phase);
        Assert.Null(settled.RecoveryKind);
        Assert.True(settled.FinancialComplete);
        Assert.True(settled.PhysicalComplete);

        // TB-273-R1 (A): durable dispositions alone are NOT the completion — the owning final marker is still owed
        // and the same physical intent resumes only that remaining closure work.
        var markerPending = Describe(Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [succeeded],
            dispositions: [ReturnItemDisposition.SaleableRestock],
            closurePhase: CourierOriginClosurePhase.StockSettled));
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, markerPending.Phase);
        Assert.Equal(ReturnRecoveryKind.SettlePhysicalDisposition, markerPending.RecoveryKind);
        Assert.True(markerPending.FinancialComplete);
        Assert.False(markerPending.PhysicalComplete);

        // "Completed" without a durable succeeded attempt is inconsistent, not settled.
        var inconsistent = Describe(Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [(Guid.NewGuid(), RefundAttemptStatus.Pending)],
            dispositions: [ReturnItemDisposition.SaleableRestock]));
        Assert.Equal(ReturnRecoveryPhase.Blocked, inconsistent.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptUnbound, inconsistent.BlockerCode);
    }

    // ------------------------------------------------------------------ B: same-attempt recovery

    [Fact]
    public async Task An_unknown_answer_is_recovered_with_the_same_attempt_and_key()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        order.Gateway.EnqueueSuccess("ref-273-a");

        var attempt = Assert.Single(await order.Attempts());
        Assert.Equal(RefundAttemptStatus.Pending, attempt.Status);
        Assert.Equal($"refund-{order.ReturnRequestId:N}", attempt.IdempotencyKey);
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, (await order.Snapshot())!.Recovery!.Phase);

        var recovered = await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        // One durable financial outcome with the SAME attempt and the SAME key: nothing was minted.
        var after = Assert.Single(await order.Attempts());
        Assert.Equal(attempt.RefundAttemptId, after.RefundAttemptId);
        Assert.Equal(attempt.IdempotencyKey, after.IdempotencyKey);
        Assert.Equal(attempt.PaymentId, after.PaymentId);
        Assert.Equal(attempt.Amount, after.Amount);
        Assert.Equal(RefundAttemptStatus.Succeeded, after.Status);
        Assert.Equal("ref-273-a", after.ProviderReference);
        Assert.Equal(ReturnRequestStatus.Completed, recovered.Status);
        Assert.Equal(1, await order.AttemptCount());

        // Money success is NOT physical completion: the recorded receipt now authorizes the OWNING physical step, and
        // it settles exactly the inspected usable quantity (never the requested default).
        Assert.True(recovered.Recovery!.FinancialComplete);
        Assert.True(recovered.Recovery.PhysicalComplete);
        Assert.Equal(ReturnItemDisposition.SaleableRestock, (await order.Items()).Single().Disposition);
        Assert.Equal(1m, Assert.Single(order.Inventory.Restocked).Quantity);

        // The provider saw exactly the recorded identity, once, and the wallet was never touched.
        var call = Assert.Single(order.Gateway.Calls);
        Assert.Equal(attempt.IdempotencyKey, call.IdempotencyKey);
        Assert.Equal(attempt.PaymentId, call.PaymentId);
        Assert.Equal(attempt.Amount, call.Amount);
        Assert.Empty(order.Wallet.Calls);

        // A recorded terminal outcome is answered from the row: no second money call, nothing downgraded.
        await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);
        Assert.Single(order.Gateway.Calls);
        Assert.Equal("ref-273-a", (await order.Attempts()).Single().ProviderReference);
        Assert.Equal(1, await order.AttemptCount());
    }

    [Fact]
    public async Task A_repeated_transport_failure_stays_pending_without_minting_a_second_attempt()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        order.Gateway.EnqueueUnknown();
        order.Gateway.EnqueueUnknown();
        var attempt = Assert.Single(await order.Attempts());

        // Two further unknown answers are still UNKNOWN: not a failure and not a new attempt.
        var first = await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);
        var second = await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        var after = Assert.Single(await order.Attempts());
        Assert.Equal(RefundAttemptStatus.Pending, after.Status);
        Assert.Null(after.ProviderReference);
        Assert.Null(after.CompletedAt);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, first.Status);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, second.Status);
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, second.Recovery!.Phase);
        Assert.Equal(attempt.RefundAttemptId, second.Recovery.RefundAttemptId);
        Assert.Equal(2, order.Gateway.Calls.Count);
        Assert.Single(order.Gateway.Calls.Select(x => x.IdempotencyKey).Distinct());
        Assert.Equal(1, await order.AttemptCount());
    }

    [Fact]
    public async Task A_definite_failure_is_not_resumable_and_never_mints_the_new_attempt_retry()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.FailedMoney);
        var attempt = Assert.Single(await order.Attempts());
        Assert.Equal(RefundAttemptStatus.Failed, attempt.Status);
        Assert.Equal(ReturnRecoveryPhase.FailedMoney, (await order.Snapshot())!.Recovery!.Phase);

        // The recovery intent answers the recorded attempt and matches nothing: no money call, no new key.
        var answered = await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);
        Assert.Equal(ReturnRequestStatus.RefundFailed, answered.Status);
        Assert.Empty(order.Gateway.Calls);
        var after = Assert.Single(await order.Attempts());
        Assert.Equal(attempt.RefundAttemptId, after.RefundAttemptId);
        Assert.Equal(attempt.IdempotencyKey, after.IdempotencyKey);
        Assert.Equal("REFUND_DECLINED", after.FailureCode);

        // And a failed money case is never "repaired" by moving stock.
        var stockRefused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal(ReturnsErrorCodes.RecoveryStateMismatch, stockRefused.Code);
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Inventory.Restocked);
    }

    [Fact]
    public async Task A_wrong_or_missing_attempt_identity_is_refused_before_any_money_call()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        var attempt = Assert.Single(await order.Attempts());

        var wrong = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, Guid.NewGuid()));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, wrong.Code);

        var missing = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, expectedAttemptId: null));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, missing.Code);

        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(RefundAttemptStatus.Pending, (await order.Attempts()).Single().Status);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, (await order.Snapshot())!.Status);
    }

    [Fact]
    public async Task Wallet_destination_credits_once_with_the_case_key_and_never_calls_the_psp()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney, RefundDestination.Wallet);
        var attempt = Assert.Single(await order.Attempts());

        // An unknown Wallet answer retains the pending attempt exactly like a gateway timeout.
        order.Wallet.EnqueueUnknown();
        order.Wallet.EnqueueSuccess();
        var afterUnknown = await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);
        Assert.Equal(RefundAttemptStatus.Pending, (await order.Attempts()).Single().Status);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, afterUnknown.Status);

        await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        // The ledger key is the CASE key, so the repeated intent can only ever credit once, and no PSP is involved.
        Assert.Equal(2, order.Wallet.Calls.Count);
        Assert.All(order.Wallet.Calls, x =>
            Assert.Equal($"wallet-refund-credit:{order.ReturnRequestId:D}", x.IdempotencyKey));
        Assert.All(order.Wallet.Calls, x => Assert.Equal(order.ReturnRequestId, x.ReturnRequestId));
        Assert.Single(order.Wallet.Calls.Select(x => x.IdempotencyKey).Distinct());
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(RefundAttemptStatus.Succeeded, (await order.Attempts()).Single().Status);
        Assert.Equal(ReturnRequestStatus.Completed, (await order.Snapshot())!.Status);
        Assert.Equal(1, await order.AttemptCount());
    }

    // ------------------------------------------------------------------ C: physical disposition is its own phase

    [Fact]
    public async Task Confirmed_money_with_a_failed_restock_recovers_stock_only_with_the_same_item_key()
    {
        await using var order = await SeedCourierCaseAsync();
        order.Inventory.FailuresRemaining = 1;

        // The physical phase is executable, but the owning restock refuses: the line stays explicitly pending.
        await Assert.ThrowsAsync<InvalidOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal(
            ReturnItemDisposition.None,
            (await order.Items()).Single(x => x.Condition == ReturnItemCondition.Usable).Disposition);
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, (await order.Snapshot())!.Recovery!.Phase);
        Assert.Empty(order.Inventory.Restocked);

        // The retry is disposition-only: zero refund/Wallet calls and zero new attempts.
        var settled = await order.Recover(ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null);

        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Equal(1, await order.AttemptCount());
        var restock = Assert.Single(order.Inventory.Restocked);
        Assert.Equal(
            $"return-restock-{order.ReturnRequestId:N}-{settled.Items.Single(x => x.Condition == ReturnItemCondition.Usable).ReturnItemId:N}",
            restock.IdempotencyKey);

        Assert.All(settled.Items, x => Assert.NotEqual(ReturnItemDisposition.None, x.Disposition));
        Assert.Equal(
            ReturnItemDisposition.DamagedNonSaleable,
            settled.Items.Single(x => x.Condition == ReturnItemCondition.Damaged).Disposition);
        Assert.Equal(ReturnRecoveryPhase.Settled, settled.Recovery!.Phase);
        Assert.Equal(CourierOriginClosurePhase.RefundReleased, settled.ClosurePhase);

        // A second identical intent is answered from the stored completion: no further restock, no money.
        var replay = await order.Recover(ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null);
        Assert.Single(order.Inventory.Restocked);
        Assert.Equal(ReturnRecoveryPhase.Settled, replay.Recovery!.Phase);
        Assert.Equal(1, await order.AttemptCount());
    }

    [Fact]
    public async Task An_ordinary_case_without_receiving_evidence_is_blocked_with_zero_restock_and_money_kept()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.MoneyConfirmedNoDisposition, receipt: false);
        Assert.Equal(ReturnsErrorCodes.RecoveryReceivingRequired, (await order.Snapshot())!.Recovery!.BlockerCode);

        var blocked = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal(ReturnsErrorCodes.RecoveryReceivingRequired, blocked.Code);

        // Nothing is invented: no restock, no second attempt, and the money stays confirmed.
        Assert.Empty(order.Inventory.Restocked);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(1, await order.AttemptCount());
        var stored = await order.Snapshot();
        Assert.Equal(ReturnRequestStatus.Completed, stored!.Status);
        Assert.True(stored.Recovery!.FinancialComplete);
        Assert.False(stored.Recovery.PhysicalComplete);
        Assert.Equal(ReturnItemDisposition.None, (await order.Items()).Single().Disposition);
    }

    [Fact]
    public async Task A_requested_case_can_never_gain_a_recovery_action()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.Requested);

        var money = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, Guid.NewGuid()));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, money.Code);

        var stock = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal(ReturnsErrorCodes.RecoveryStateMismatch, stock.Code);

        Assert.Equal(0, await order.AttemptCount());
        var snapshot = await order.Snapshot();
        Assert.Equal(ReturnRequestStatus.Requested, snapshot!.Status);
        Assert.Equal(ReturnRecoveryPhase.None, snapshot.Recovery!.Phase);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Empty(order.Gateway.Calls);
    }

    // ------------------------------------------------------------------ D: nothing is written before the answer

    [Fact]
    public async Task An_explicitly_unconfigured_adapter_is_a_typed_unavailable_that_keeps_the_pending_attempt()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        var attempt = Assert.Single(await order.Attempts());
        order.Gateway.EnqueueUnconfigured();

        // A definite "no adapter" is neither a refund nor an invented provider rejection, and nothing is written.
        var unavailable = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId));
        Assert.Equal(ReturnsErrorCodes.RecoveryGatewayUnavailable, unavailable.Code);

        var after = Assert.Single(await order.Attempts());
        Assert.Equal(RefundAttemptStatus.Pending, after.Status);
        Assert.Null(after.ProviderReference);
        Assert.Null(after.FailureCode);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, (await order.Snapshot())!.Status);
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, (await order.Snapshot())!.Recovery!.Phase);
        Assert.Single(order.Gateway.Calls);

        // Once the adapter is configured, the SAME recorded attempt/key settles exactly once.
        order.Gateway.EnqueueSuccess("ref-273-configured");
        await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        var settled = Assert.Single(await order.Attempts());
        Assert.Equal(attempt.RefundAttemptId, settled.RefundAttemptId);
        Assert.Equal("ref-273-configured", settled.ProviderReference);
        Assert.Equal(1, await order.AttemptCount());
        Assert.All(order.Gateway.Calls, x => Assert.Equal(attempt.IdempotencyKey, x.IdempotencyKey));
    }

    [Fact]
    public async Task A_local_save_failure_after_an_external_success_is_never_answered_as_durable()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        var attempt = Assert.Single(await order.Attempts());
        order.Gateway.EnqueueSuccess("ref-273-lost-save");

        // The provider said yes, but the local write fails: the caller must NOT receive an undurable success.
        order.SaveInterceptor.FailuresRemaining = 1;
        var failure = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId));
        Assert.Contains("local persistence failure", failure.Message, StringComparison.Ordinal);

        // Durable truth is still the ORIGINAL pending attempt with no provider reference.
        using (var probe = CreateDb(order.DbName))
        {
            var stored = await probe.RefundAttempts.AsNoTracking().SingleAsync();
            Assert.Equal(RefundAttemptStatus.Pending, stored.Status);
            Assert.Null(stored.ProviderReference);
        }

        // A fresh request scope re-reads the DURABLE attempt and reconciles THAT, not the unsaved tracked answer.
        order.ResetRequestScope();
        order.Gateway.EnqueueSuccess("ref-273-lost-save");
        await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        var settled = Assert.Single(await order.Attempts());
        Assert.Equal(attempt.RefundAttemptId, settled.RefundAttemptId);
        Assert.Equal(attempt.IdempotencyKey, settled.IdempotencyKey);
        Assert.Equal(RefundAttemptStatus.Succeeded, settled.Status);
        Assert.Equal("ref-273-lost-save", settled.ProviderReference);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Equal(2, order.Gateway.Calls.Count);
        Assert.Single(order.Gateway.Calls.Select(x => x.IdempotencyKey).Distinct());
    }

    [Fact]
    public async Task A_closed_use_case_guard_refuses_recovery_before_any_effect()
    {
        await using var order = await SeedOrdinaryCaseAsync(
            StoredState.UnknownMoney, guard: new ClosedGuard());
        var attempt = Assert.Single(await order.Attempts());

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId));
        Assert.Equal("platform.use_case.closed", refused.Code);

        var stockRefused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal("platform.use_case.closed", stockRefused.Code);

        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(RefundAttemptStatus.Pending, (await order.Attempts()).Single().Status);
        Assert.Equal(ReturnRequestStatus.RefundProcessing, (await order.Snapshot())!.Status);
    }

    [Fact]
    public async Task A_rejected_case_can_never_gain_a_recovery_action()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.Rejected);

        var money = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, Guid.NewGuid()));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptMismatch, money.Code);

        var stock = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal(ReturnsErrorCodes.RecoveryStateMismatch, stock.Code);

        Assert.Equal(0, await order.AttemptCount());
        var snapshot = await order.Snapshot();
        Assert.Equal(ReturnRequestStatus.Rejected, snapshot!.Status);
        Assert.Equal(ReturnRecoveryPhase.None, snapshot.Recovery!.Phase);
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Inventory.Restocked);
    }

    [Fact]
    public async Task Nothing_is_held_or_written_while_the_money_answer_is_outstanding()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        order.Gateway.EnqueueSuccess("ref-273-tx");

        // The probe reads the DURABLE attempt through an independent context at the exact instant of the call.
        order.Gateway.Probe = () =>
        {
            using var probe = CreateDb(order.DbName);
            return (
                Transaction: probe.Database.CurrentTransaction is not null,
                Status: probe.RefundAttempts.AsNoTracking().Single(x => x.ReturnRequestId == order.ReturnRequestId).Status);
        };

        var attempt = Assert.Single(await order.Attempts());
        await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        var call = Assert.Single(order.Gateway.Calls);
        Assert.False(call.InsideTransaction);
        Assert.False(call.TransactionOpen);
        Assert.Equal(RefundAttemptStatus.Pending, call.StatusWhileCalling);
        Assert.Equal(RefundAttemptStatus.Succeeded, (await order.Attempts()).Single().Status);
    }

    // ------------------------------------------------------------------ TB-273-R1 (A): fail-closed evidence

    [Fact]
    public async Task A_case_without_a_recorded_approval_is_blocked_in_read_and_refused_with_zero_effect()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney, seedDecision: false);
        var attempt = Assert.Single(await order.Attempts());

        // The SAME read describes the blocker an operator must not be able to click past.
        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, read.BlockerCode);
        Assert.False(read.FinancialComplete);
        Assert.Null(read.RecoveryKind);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId));
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, refused.Code);

        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(RefundAttemptStatus.Pending, (await order.Attempts()).Single().Status);
        Assert.Equal(0, await order.DecisionCount());
    }

    [Fact]
    public async Task An_opposite_recorded_decision_is_blocked_and_never_refunded()
    {
        await using var order = await SeedOrdinaryCaseAsync(
            StoredState.UnknownMoney, decision: ReturnRecoveryPolicy.RejectedDecision);
        var attempt = Assert.Single(await order.Attempts());

        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.AlreadyRejected, read.BlockerCode);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId));
        Assert.Equal(ReturnsErrorCodes.AlreadyRejected, refused.Code);

        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(RefundAttemptStatus.Pending, (await order.Attempts()).Single().Status);
        Assert.Equal(1, await order.DecisionCount());
    }

    [Fact]
    public async Task A_terminal_status_holding_a_live_attempt_is_blocked_not_quietly_empty()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        var pending = await order.Db.RefundAttempts.SingleAsync();
        var request = await order.Db.ReturnRequests.SingleAsync();

        // Contradictory stored facts: a case that claims it was never approved while a live attempt exists.
        order.Db.Entry(request).Property(x => x.Status).CurrentValue = ReturnRequestStatus.Requested;
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, read.BlockerCode);
        Assert.Null(read.RecoveryKind);

        var stock = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, stock.Code);

        Assert.Empty(order.Inventory.Restocked);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(pending.RefundAttemptId, (await order.Attempts()).Single().RefundAttemptId);
    }

    [Fact]
    public async Task A_succeeded_attempt_that_does_not_bind_to_the_case_never_authorizes_money_or_stock()
    {
        await using var order = await SeedCourierCaseAsync();
        var succeeded = await order.Db.RefundAttempts.SingleAsync();

        // The stored success answers for ANOTHER payment: it can never be this case's proven money.
        order.Db.Entry(succeeded).Property(x => x.PaymentId).CurrentValue = Guid.NewGuid();
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptUnbound, read.BlockerCode);
        Assert.False(read.FinancialComplete);
        Assert.Null(read.RecoveryKind);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptUnbound, refused.Code);

        Assert.Empty(order.Inventory.Restocked);
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Equal(
            ReturnItemDisposition.None,
            (await order.Items()).Single(x => x.Condition == ReturnItemCondition.Usable).Disposition);
    }

    [Fact]
    public async Task A_pending_attempt_with_a_mismatched_amount_is_blocked_in_read_and_action()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        var pending = await order.Db.RefundAttempts.SingleAsync();

        order.Db.Entry(pending).Property(x => x.Amount).CurrentValue = pending.Amount + 1m;
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptUnbound, read.BlockerCode);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, pending.RefundAttemptId));
        Assert.Equal(ReturnsErrorCodes.RecoveryAttemptUnbound, refused.Code);

        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(RefundAttemptStatus.Pending, (await order.Attempts()).Single().Status);
    }

    // ------------------------------------------------------------------ TB-273-R1 (A/C): the last closure step

    [Fact]
    public async Task Every_item_disposed_with_only_the_final_marker_pending_is_recoverable_with_zero_money()
    {
        await using var order = await SeedCourierCaseAsync();

        // Every item disposition is DURABLE, but the final owning marker save never landed. This must NOT read as
        // Settled, and the physical intent must be able to finish just the marker.
        var usable = await order.Db.ReturnItems.SingleAsync(x => x.Condition == ReturnItemCondition.Usable);
        usable.MarkDisposition(ReturnItemDisposition.SaleableRestock);
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, read.Phase);
        Assert.Equal(ReturnRecoveryKind.SettlePhysicalDisposition, read.RecoveryKind);
        Assert.True(read.FinancialComplete);
        Assert.False(read.PhysicalComplete);

        var finished = await order.Recover(ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null);

        Assert.Equal(ReturnRecoveryPhase.Settled, finished.Recovery!.Phase);
        Assert.True(finished.Recovery.PhysicalComplete);
        Assert.Equal(CourierOriginClosurePhase.RefundReleased, finished.ClosurePhase);

        // Finalization only: no money call, no second restock and no new attempt.
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(1, await order.AttemptCount());

        // A settled replay stays zero-effect.
        var replay = await order.Recover(ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null);
        Assert.Equal(ReturnRecoveryPhase.Settled, replay.Recovery!.Phase);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Empty(order.Gateway.Calls);
    }

    [Fact]
    public async Task A_lost_item_save_never_promotes_an_unsaved_disposition_or_double_restocks()
    {
        await using var order = await SeedCourierCaseAsync();
        order.SaveInterceptor.FailuresRemaining = 1;

        var failure = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Contains("local persistence failure", failure.Message, StringComparison.Ordinal);

        // Durable truth is unchanged: the line is still pending and the case did not reach its final marker.
        using (var probe = CreateDb(order.DbName))
        {
            var storedItem = await probe.ReturnItems.AsNoTracking()
                .SingleAsync(x => x.Condition == ReturnItemCondition.Usable);
            Assert.Equal(ReturnItemDisposition.None, storedItem.Disposition);
            var storedRequest = await probe.ReturnRequests.AsNoTracking().SingleAsync();
            Assert.Equal(CourierOriginClosurePhase.StockSettled, storedRequest.ClosurePhase);
        }

        // The SAME request scope retries: it must use the reloaded durable line, not an unsaved SaleableRestock.
        var settled = await order.Recover(ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null);

        Assert.Equal(ReturnRecoveryPhase.Settled, settled.Recovery!.Phase);
        var restock = Assert.Single(order.Inventory.Restocked);
        Assert.Equal(
            $"return-restock-{order.ReturnRequestId:N}-{settled.Items.Single(x => x.Condition == ReturnItemCondition.Usable).ReturnItemId:N}",
            restock.IdempotencyKey);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Empty(order.Gateway.Calls);
    }

    [Fact]
    public async Task A_lost_final_marker_save_never_reports_settled_and_the_same_scope_retry_completes_it()
    {
        await using var order = await SeedCourierCaseAsync();

        // Both received lines are already durable and the owning StockSettled marker is durable: only the final
        // RefundReleased save is still owed, so every earlier physical step is a no-op on this pass.
        foreach (var item in await order.Db.ReturnItems.ToListAsync())
        {
            item.MarkDisposition(item.Condition == ReturnItemCondition.Damaged
                ? ReturnItemDisposition.DamagedNonSaleable
                : ReturnItemDisposition.SaleableRestock);
        }

        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, read.Phase);
        Assert.True(read.FinancialComplete);
        Assert.False(read.PhysicalComplete);

        // The ONE remaining save (the final marker) fails locally.
        order.SaveInterceptor.FailuresRemaining = 1;
        var failure = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => order.Recover(
            ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
        Assert.Contains("local persistence failure", failure.Message, StringComparison.Ordinal);

        // Durable truth: the lines stay disposed, but the case did NOT reach its final marker.
        using (var probe = CreateDb(order.DbName))
        {
            var storedRequest = await probe.ReturnRequests.AsNoTracking().SingleAsync();
            Assert.Equal(CourierOriginClosurePhase.StockSettled, storedRequest.ClosurePhase);
            var storedItems = await probe.ReturnItems.AsNoTracking().ToListAsync();
            Assert.All(storedItems, x => Assert.NotEqual(ReturnItemDisposition.None, x.Disposition));
        }

        // The SAME request scope retries: the unsaved marker is never trusted, and only the marker is written.
        var settled = await order.Recover(ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null);
        Assert.Equal(ReturnRecoveryPhase.Settled, settled.Recovery!.Phase);
        Assert.True(settled.Recovery.PhysicalComplete);
        Assert.Equal(CourierOriginClosurePhase.RefundReleased, settled.ClosurePhase);
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(1, await order.AttemptCount());

        using (var probe = CreateDb(order.DbName))
        {
            var stored = await probe.ReturnRequests.AsNoTracking().SingleAsync();
            Assert.Equal(CourierOriginClosurePhase.RefundReleased, stored.ClosurePhase);
        }
    }

    // ------------------------------------------------------------------ TB-273-R1 (C): the truthful reference

    [Fact]
    public async Task A_blank_provider_reference_is_stored_truthfully_and_never_invented()
    {
        await using var order = await SeedOrdinaryCaseAsync(StoredState.UnknownMoney);
        var attempt = Assert.Single(await order.Attempts());
        order.Gateway.EnqueueSuccess(reference: null);

        var recovered = await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        var after = Assert.Single(await order.Attempts());
        Assert.Equal(RefundAttemptStatus.Succeeded, after.Status);
        Assert.Null(after.ProviderReference);
        Assert.Equal(ReturnRequestStatus.Completed, recovered.Status);

        // The confirmed money fact is preserved; the recorded receipt now authorizes the owning physical settlement,
        // which restocks exactly the inspected usable quantity with the same per-item identity.
        Assert.True(recovered.Recovery!.FinancialComplete);
        Assert.Equal(ReturnRecoveryPhase.Settled, recovered.Recovery.Phase);
        Assert.Null(recovered.Recovery.BlockerCode);
        Assert.Equal(ReturnItemDisposition.SaleableRestock, (await order.Items()).Single().Disposition);
        Assert.Equal(1m, Assert.Single(order.Inventory.Restocked).Quantity);

        // A repeat answers the stored outcome: no fresh money and still no invented reference.
        await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);
        Assert.Single(order.Gateway.Calls);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Null((await order.Attempts()).Single().ProviderReference);
    }

    // ------------------------------------------------------------------ TB-273-R2 (A): source before pending money

    [Fact]
    public void Incoherent_courier_source_blocks_pending_money_in_the_same_policy_the_command_uses()
    {
        // A stored approved courier case with ONE bound PENDING attempt is genuinely unknown money...
        var coherent = Snap(
            ReturnRequestStatus.RefundProcessing,
            ReturnOrigin.CourierOrigin,
            attempts: [(Guid.NewGuid(), RefundAttemptStatus.Pending)]);
        var valid = Describe(coherent);
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, valid.Phase);
        Assert.Equal(ReturnRecoveryKind.ResumePendingRefund, valid.RecoveryKind);
        Assert.Null(valid.BlockerCode);

        // ...but ONLY while the SAME courier source/claim/receipt/condition evidence is coherent. Each broken shape
        // below must be a typed blocker with NO executable kind, exactly like a broken physical phase.
        var item = coherent.Items[0];
        var broken = new[]
        {
            coherent with { SourceClaimId = null },
            coherent with { SourceFulfillmentId = Guid.Empty },
            coherent with { SourceReceiptId = null },
            coherent with { SourceOccurrenceKey = " " },
            coherent with { Items = [item with { SourceReceiptId = Guid.NewGuid() }] },
            coherent with { Items = [item with { SourceShipmentId = Guid.NewGuid() }] },
            coherent with { Items = [item with { Disposition = ReturnItemDisposition.DamagedNonSaleable }] },
            coherent with
            {
                Items =
                [
                    item with
                    {
                        Condition = ReturnItemCondition.Damaged,
                        Disposition = ReturnItemDisposition.SaleableRestock,
                    },
                ],
            },
        };

        foreach (var snapshot in broken)
        {
            var read = Describe(snapshot);
            Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
            Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, read.BlockerCode);
            Assert.Null(read.RecoveryKind);
            Assert.False(read.FinancialComplete);
            Assert.False(read.PhysicalComplete);
        }

        // A bound SUCCEEDED money fact is never discarded by the same source inconsistency: it stays confirmed and
        // blocked, so the money is neither re-sent nor used to authorize stock.
        var confirmed = Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [(Guid.NewGuid(), RefundAttemptStatus.Succeeded)],
            dispositions: [ReturnItemDisposition.SaleableRestock],
            closurePhase: CourierOriginClosurePhase.RefundReleased) with
        {
            SourceClaimId = null,
        };
        var confirmedRead = Describe(confirmed);
        Assert.Equal(ReturnRecoveryPhase.Blocked, confirmedRead.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, confirmedRead.BlockerCode);
        Assert.True(confirmedRead.FinancialComplete);
        Assert.Null(confirmedRead.RecoveryKind);
    }

    [Fact]
    public async Task Pending_courier_money_with_a_broken_source_is_refused_before_any_money_or_owner_call()
    {
        // 1) The case lost its source claim.
        await AssertPendingCourierSourceRefusedAsync(static (db, requestId) =>
        {
            var request = db.ReturnRequests.Single(x => x.ReturnRequestId == requestId);
            db.Entry(request).Property(x => x.SourceClaimId).CurrentValue = null;
            return Task.CompletedTask;
        });

        // 2) A received line points at ANOTHER receipt/parcel.
        await AssertPendingCourierSourceRefusedAsync(static (db, requestId) =>
        {
            var item = db.ReturnItems.First(x => x.ReturnRequestId == requestId);
            db.Entry(item).Property(x => x.SourceReceiptId).CurrentValue = Guid.NewGuid();
            db.Entry(item).Property(x => x.SourceShipmentId).CurrentValue = Guid.NewGuid();
            return Task.CompletedTask;
        });

        // 3) The recorded condition/disposition pair is illegal (a usable unit marked non-saleable).
        await AssertPendingCourierSourceRefusedAsync(static (db, requestId) =>
        {
            var item = db.ReturnItems.Single(x => x.ReturnRequestId == requestId && x.Condition == ReturnItemCondition.Usable);
            db.Entry(item).Property(x => x.Disposition).CurrentValue = ReturnItemDisposition.DamagedNonSaleable;
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task A_coherent_pending_courier_case_resumes_the_same_bound_attempt_and_key()
    {
        await using var order = await SeedCourierCaseAsync(pendingMoney: true);
        order.Gateway.EnqueueSuccess("ref-273r2-courier");
        var attempt = Assert.Single(await order.Attempts());
        Assert.Equal(RefundAttemptStatus.Pending, attempt.Status);
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, (await order.Snapshot())!.Recovery!.Phase);

        var recovered = await order.Recover(ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId);

        // The SAME attempt and key answered: nothing was re-keyed, re-minted or re-approved.
        var after = Assert.Single(await order.Attempts());
        Assert.Equal(attempt.RefundAttemptId, after.RefundAttemptId);
        Assert.Equal(attempt.IdempotencyKey, after.IdempotencyKey);
        Assert.Equal(RefundAttemptStatus.Succeeded, after.Status);
        Assert.Equal("ref-273r2-courier", after.ProviderReference);
        Assert.Equal(1, await order.AttemptCount());

        var call = Assert.Single(order.Gateway.Calls);
        Assert.Equal(attempt.IdempotencyKey, call.IdempotencyKey);
        Assert.Equal(attempt.PaymentId, call.PaymentId);
        Assert.Equal(attempt.Amount, call.Amount);
        Assert.Empty(order.Wallet.Calls);

        // Money success is not physical completion; the existing owning closure stays the recorded authority.
        Assert.True(recovered.Recovery!.FinancialComplete);
        Assert.Equal(1, await order.DecisionCount());
    }

    // ------------------------------------------------------------------ TB-273-R2 (B): the prior closure evidence

    [Fact]
    public void A_recorded_final_marker_without_its_prior_closure_timestamps_is_inconsistent_not_settled()
    {
        var attempt = (Guid.NewGuid(), RefundAttemptStatus.Succeeded);
        var complete = Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [attempt],
            dispositions: [ReturnItemDisposition.SaleableRestock],
            closurePhase: CourierOriginClosurePhase.RefundReleased);

        // Coherent final facts: the completion.
        var settled = Describe(complete);
        Assert.Equal(ReturnRecoveryPhase.Settled, settled.Phase);
        Assert.True(settled.FinancialComplete);
        Assert.True(settled.PhysicalComplete);

        // The finalization writer itself requires both prior timestamps, so a marker missing either one (or both) is
        // an inconsistent stored shape: typed conflict, no kind, confirmed money preserved, never Settled.
        var inconsistent = new[]
        {
            complete with { ObligationClosedAt = null },
            complete with { StockSettledAt = null },
            complete with { ObligationClosedAt = null, StockSettledAt = null },
        };

        foreach (var snapshot in inconsistent)
        {
            var read = Describe(snapshot);
            Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
            Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, read.BlockerCode);
            Assert.Null(read.RecoveryKind);
            Assert.True(read.FinancialComplete);
            Assert.False(read.PhysicalComplete);
        }

        // The legitimate marker-only state (both timestamps durable, final marker still owed) stays recoverable.
        var markerPending = Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [attempt],
            dispositions: [ReturnItemDisposition.SaleableRestock],
            closurePhase: CourierOriginClosurePhase.StockSettled);
        var pending = Describe(markerPending);
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, pending.Phase);
        Assert.Equal(ReturnRecoveryKind.SettlePhysicalDisposition, pending.RecoveryKind);
    }

    [Fact]
    public async Task A_final_marker_missing_its_prior_evidence_refuses_physical_recovery_and_repairs_nothing()
    {
        var variants = new[] { "obligation", "stock", "both" };
        foreach (var variant in variants)
        {
            await using var order = await SeedCourierCaseAsync();

            // Every item is durable, the case looks like its final marker landed — but the prior evidence did not.
            foreach (var item in await order.Db.ReturnItems.ToListAsync())
            {
                item.MarkDisposition(item.Condition == ReturnItemCondition.Damaged
                    ? ReturnItemDisposition.DamagedNonSaleable
                    : ReturnItemDisposition.SaleableRestock);
            }

            var request = await order.Db.ReturnRequests.SingleAsync();
            order.Db.Entry(request).Property(x => x.ClosurePhase).CurrentValue = CourierOriginClosurePhase.RefundReleased;
            if (variant is "obligation" or "both")
            {
                order.Db.Entry(request).Property(x => x.ObligationClosedAt).CurrentValue = null;
            }

            if (variant is "stock" or "both")
            {
                order.Db.Entry(request).Property(x => x.StockSettledAt).CurrentValue = null;
            }

            await order.Db.SaveChangesAsync();
            order.ResetRequestScope();

            var read = (await order.Snapshot())!.Recovery!;
            Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
            Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, read.BlockerCode);
            Assert.Null(read.RecoveryKind);
            Assert.True(read.FinancialComplete);
            Assert.False(read.PhysicalComplete);

            var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
                ReturnRecoveryKind.SettlePhysicalDisposition, expectedAttemptId: null));
            Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, refused.Code);

            // Zero effect: no money, no stock, no backfill of the missing timestamps and no new attempt.
            Assert.Empty(order.Gateway.Calls);
            Assert.Empty(order.Wallet.Calls);
            Assert.Empty(order.Inventory.Restocked);
            Assert.Equal(1, await order.AttemptCount());
            Assert.Equal("ref-273-courier", (await order.Attempts()).Single().ProviderReference);
            var stored = await order.Snapshot();
            Assert.Equal(CourierOriginClosurePhase.RefundReleased, stored!.ClosurePhase);
            Assert.Equal(
                variant is "obligation" or "both" ? (DateTimeOffset?)null : Now,
                (await order.Db.ReturnRequests.AsNoTracking().SingleAsync()).ObligationClosedAt);
            Assert.Equal(
                variant is "stock" or "both" ? (DateTimeOffset?)null : Now,
                (await order.Db.ReturnRequests.AsNoTracking().SingleAsync()).StockSettledAt);
        }
    }

    // ------------------------------------------------------------------ fixtures

    private static ReturnRecoveryDescription Describe(ReturnSnapshot snapshot) =>
        ReturnRecoveryPolicy.Describe(snapshot);

    private static ReturnSnapshot Snap(
        ReturnRequestStatus status,
        ReturnOrigin origin = ReturnOrigin.CustomerDelivered,
        (Guid Id, RefundAttemptStatus Status)[]? attempts = null,
        ReturnItemDisposition[]? dispositions = null,
        decimal refundAmount = 1000m,
        string? recordedDecision = ReturnRecoveryPolicy.ApprovedDecision,
        bool bindAttempts = true,
        CourierOriginClosurePhase closurePhase = CourierOriginClosurePhase.None,
        bool priorObligationClosedAt = true,
        bool priorStockSettledAt = true,
        bool receivingComplete = false)
    {
        var effective = dispositions ?? [ReturnItemDisposition.None];
        var approved = status is not (ReturnRequestStatus.Requested
            or ReturnRequestStatus.Rejected
            or ReturnRequestStatus.Cancelled);
        var paymentId = approved ? Guid.NewGuid() : (Guid?)null;
        var courier = origin == ReturnOrigin.CourierOrigin;
        var receiptId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var items = effective
            .Select(disposition => new ReturnItemSnapshot(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1m,
                1000m,
                "IRR",
                null,
                ReturnItemCondition.Usable,
                disposition,
                courier ? shipmentId : null,
                courier ? receiptId : null))
            .ToArray();
        // TB-274 (B): an ordinary case's receiving/inspection evidence is a real stored fact, so the pure policy
        // fixture states it explicitly instead of implying it from the line's default condition.
        var receiving = !courier && receivingComplete
            ? ReturnReceivingSummary.Describe(
                items.Select(x => (x.ReturnItemId, x.Quantity)),
                items.SelectMany(x => new[]
                {
                    (x.ReturnItemId, x.Quantity, ReturnItemCondition.Usable),
                }))
            : null;

        return new ReturnSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "دلیل مرجوعی",
            "IRR",
            refundAmount,
            paymentId,
            RefundDestination.OriginalPayment,
            Now,
            Now,
            items,
            (attempts ?? [])
                .Select(x => new RefundAttemptSnapshot(
                    x.Id,
                    bindAttempts ? paymentId ?? Guid.NewGuid() : Guid.NewGuid(),
                    bindAttempts ? refundAmount : refundAmount + 1m,
                    "IRR",
                    x.Status,
                    "refund-key",
                    x.Status == RefundAttemptStatus.Succeeded ? "ref" : null,
                    x.Status == RefundAttemptStatus.Failed ? "DECLINED" : null,
                    Now,
                    x.Status == RefundAttemptStatus.Pending ? null : Now))
                .ToArray(),
            origin,
            SourceFulfillmentId: courier ? Guid.NewGuid() : null,
            SourceShipmentId: courier ? shipmentId : null,
            SourceReceiptId: courier ? receiptId : null,
            SourceClaimId: courier ? Guid.NewGuid() : null,
            SourceOccurrenceKey: courier ? "courier-return:tb273" : null,
            ClosurePhase: closurePhase,
            // TB-273-R2 (B): the prior durable closure timestamps the finalization writer requires. A marker phase
            // without them is an inconsistent stored shape, so the fixture can omit either/both on purpose.
            ObligationClosedAt: courier
                && closurePhase >= CourierOriginClosurePhase.ObligationClosed
                && priorObligationClosedAt
                ? (DateTimeOffset?)Now
                : null,
            StockSettledAt: courier
                && closurePhase >= CourierOriginClosurePhase.StockSettled
                && priorStockSettledAt
                ? (DateTimeOffset?)Now
                : null,
            RecordedDecision: recordedDecision,
            Receiving: receiving);
    }

    private enum StoredState
    {
        Requested,
        Rejected,
        UnknownMoney,
        FailedMoney,
        MoneyConfirmedNoDisposition,
    }

    private sealed class CaseHarness : IAsyncDisposable
    {
        private readonly ReturnsDbContext _db;
        private readonly ReturnDirectory _directory;

        public CaseHarness(
            ReturnsDbContext db,
            string dbName,
            ReturnDirectory directory,
            Guid returnRequestId,
            ScriptedGateway gateway,
            ScriptedWallet wallet,
            ScriptedInventory inventory,
            FailingSaveInterceptor saveInterceptor)
        {
            _db = db;
            DbName = dbName;
            _directory = directory;
            ReturnRequestId = returnRequestId;
            Gateway = gateway;
            Wallet = wallet;
            Inventory = inventory;
            SaveInterceptor = saveInterceptor;

            // Every money call is probed at the exact instant it is made.
            Func<bool> probe = () => _db.Database.CurrentTransaction is not null;
            gateway.TransactionProbe = probe;
            wallet.TransactionProbe = probe;
        }

        public string DbName { get; }

        /// <summary>TB-273-R1: direct access so a fact can stage the exact contradictory stored row it needs.</summary>
        public ReturnsDbContext Db => _db;

        public Guid ReturnRequestId { get; }

        public ScriptedGateway Gateway { get; }

        public ScriptedWallet Wallet { get; }

        public ScriptedInventory Inventory { get; }

        public FailingSaveInterceptor SaveInterceptor { get; }

        /// <summary>Simulates the end of a request: the next call re-reads durable truth instead of tracked state.</summary>
        public void ResetRequestScope() => _db.ChangeTracker.Clear();

        public Task<ReturnSnapshot> Recover(ReturnRecoveryKind kind, Guid? expectedAttemptId) =>
            _directory.RecoverAsync(
                new RecoverReturnCommand(ReturnRequestId, Guid.NewGuid(), kind, expectedAttemptId),
                CancellationToken.None);

        public Task<ReturnSnapshot?> Snapshot() => _directory.GetAsync(ReturnRequestId, CancellationToken.None);

        public async Task<IReadOnlyList<RefundAttempt>> Attempts() =>
            await _db.RefundAttempts.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public Task<int> AttemptCount() =>
            _db.RefundAttempts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> DecisionCount() =>
            _db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public async Task<IReadOnlyList<ReturnItem>> Items() =>
            await _db.ReturnItems.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }

    private static async Task<CaseHarness> SeedOrdinaryCaseAsync(
        StoredState state = StoredState.UnknownMoney,
        RefundDestination destination = RefundDestination.OriginalPayment,
        IReturnUseCaseGuard? guard = null,
        bool seedDecision = true,
        string decision = ReturnRecoveryPolicy.ApprovedDecision,
        bool receipt = true)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var saveInterceptor = new FailingSaveInterceptor();
        var db = CreateDb(dbName, saveInterceptor);
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
            Guid.NewGuid(),
            customer,
            "tb273-ordinary",
            "دلیل مرجوعی",
            "IRR",
            [(lineId, 1m, 1000m, (Guid?)Guid.NewGuid())],
            Now,
            destination);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);

        if (state == StoredState.Rejected)
        {
            // A durable rejection before any money: no attempt may ever exist for it.
            request.Reject(Now);
        }
        else if (state != StoredState.Requested)
        {
            var paymentId = Guid.NewGuid();
            request.Approve(paymentId, Now);
            request.MarkRefundProcessing(Now);
            var attempt = request.BeginRefundAttempt(
                Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
            db.RefundAttempts.Add(attempt);

            if (state == StoredState.FailedMoney)
            {
                attempt.MarkFailed("REFUND_DECLINED", Now);
                request.MarkRefundFailed(Now);
            }
            else if (state == StoredState.MoneyConfirmedNoDisposition)
            {
                attempt.MarkSucceeded("ref-273-ordinary", Now);
                request.MarkRefundSucceeded(Now);
            }
        }

        if (seedDecision && state != StoredState.Requested)
        {
            // TB-273-R1 (A): the durable decision is part of the stored evidence. Positive fixtures carry the REAL
            // recorded approval; a fixture that omits it proves the fail-closed read/action blocker.
            db.ReturnDecisions.Add(new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = decision,
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            });
        }

        if (receipt && state is not (StoredState.Requested or StoredState.Rejected))
        {
            // TB-274 (B): an ordinary case that already progressed financially carries the REAL customer receipt the
            // unknown-money/failed-money/confirmed-money phases are now gated on. It is recorded evidence, not a
            // bypass flag: the same rows every 274 gate reads.
            var returnItem = request.Items.Single();
            var seededReceipt = ReturnReceipt.Create(
                Guid.NewGuid(),
                () => Guid.NewGuid(),
                returnRequestId,
                $"tb273-receipt-{returnRequestId:N}",
                Guid.NewGuid(),
                Now,
                [(returnItem.ReturnItemId, returnItem.Quantity, ReturnItemCondition.Usable)]);
            db.ReturnReceipts.Add(seededReceipt);
            db.ReturnReceiptLines.AddRange(seededReceipt.Lines);
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var gateway = new ScriptedGateway();
        var wallet = new ScriptedWallet();
        var inventory = new ScriptedInventory();
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(orderId, customer, [lineId], Guid.NewGuid())],
            gateway,
            wallet,
            inventory,
            guard);
        return new CaseHarness(db, dbName, directory, returnRequestId, gateway, wallet, inventory, saveInterceptor);
    }

    /// <summary>
    /// TB-273-R2 (A): stages a coherent pending-courier case, breaks exactly ONE stored source fact and proves the
    /// READ blocker and the ACTION refusal are the SAME typed conflict with zero money/owner call and no state change.
    /// </summary>
    private static async Task AssertPendingCourierSourceRefusedAsync(
        Func<ReturnsDbContext, Guid, Task> breakStoredEvidence)
    {
        await using var order = await SeedCourierCaseAsync(pendingMoney: true);
        await breakStoredEvidence(order.Db, order.ReturnRequestId);
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        var attempt = Assert.Single(await order.Attempts());
        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, read.BlockerCode);
        Assert.Null(read.RecoveryKind);
        Assert.False(read.FinancialComplete);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.Recover(
            ReturnRecoveryKind.ResumePendingRefund, attempt.RefundAttemptId));
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, refused.Code);

        // Zero effect: no money, no owner call, no re-key and no closure backfill.
        Assert.Empty(order.Gateway.Calls);
        Assert.Empty(order.Wallet.Calls);
        Assert.Empty(order.Inventory.Restocked);
        var after = Assert.Single(await order.Attempts());
        Assert.Equal(attempt.RefundAttemptId, after.RefundAttemptId);
        Assert.Equal(attempt.IdempotencyKey, after.IdempotencyKey);
        Assert.Equal(RefundAttemptStatus.Pending, after.Status);
        Assert.Null(after.ProviderReference);
        Assert.Equal(1, await order.AttemptCount());

        var stored = await order.Snapshot();
        Assert.Equal(ReturnRequestStatus.RefundProcessing, stored!.Status);
        Assert.Equal(CourierOriginClosurePhase.StockSettled, stored.ClosurePhase);
    }

    private static async Task<CaseHarness> SeedCourierCaseAsync(bool pendingMoney = false)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var saveInterceptor = new FailingSaveInterceptor();
        var db = CreateDb(dbName, saveInterceptor);
        var clock = new MutableClock(Now);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var usableLine = Guid.NewGuid();
        var damagedLine = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        // A courier receipt is already inspected: the damaged unit is non-saleable from the moment it is recorded.
        var request = ReturnRequest.CreateCourierOrigin(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "courier-return:tb273",
            "tb273-courier",
            "بازگشت مرسوله",
            "IRR",
            [
                (usableLine, 1m, 1000m, (Guid?)Guid.NewGuid(), ReturnItemCondition.Usable),
                (damagedLine, 1m, 1000m, (Guid?)null, ReturnItemCondition.Damaged),
            ],
            Now);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);

        request.Approve(paymentId, Now);
        request.MarkRefundProcessing(Now);
        var attempt = request.BeginRefundAttempt(Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
        db.RefundAttempts.Add(attempt);
        if (!pendingMoney)
        {
            attempt.MarkSucceeded("ref-273-courier", Now);
            request.MarkRefundSucceeded(Now);
        }

        // The owning closure phases were recorded BEFORE the money by the approval: recovery must not redo them.
        request.MarkObligationClosed(Now);
        request.MarkStockSettled(1m, Now);

        db.ReturnDecisions.Add(new ReturnDecisionRecord
        {
            ReturnRequestId = returnRequestId,
            Decision = ReturnRecoveryPolicy.ApprovedDecision,
            DecidedByUserId = Guid.NewGuid(),
            DecidedAt = Now,
        });

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var gateway = new ScriptedGateway();
        var wallet = new ScriptedWallet();
        var inventory = new ScriptedInventory();
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(orderId, customer, [usableLine, damagedLine], null)],
            gateway,
            wallet,
            inventory);
        return new CaseHarness(db, dbName, directory, returnRequestId, gateway, wallet, inventory, saveInterceptor);
    }

    private static ReturnDirectory BuildDirectory(
        ReturnsDbContext db,
        MutableClock clock,
        IReadOnlyList<OrderReturnContextSnapshot> orders,
        ScriptedGateway gateway,
        ScriptedWallet wallet,
        ScriptedInventory inventory,
        IReturnUseCaseGuard? guard = null)
    {
        var orderReader = new MapOrderReader(orders);
        var eligibility = new ReturnEligibilityEvaluator(
            orderReader,
            new MapFulfillmentReader(orders),
            db,
            clock);
        return new ReturnDirectory(
            db,
            guard ?? new OpenReturnUseCaseGuard(),
            orderReader,
            eligibility,
            new FixedPaymentReader(),
            gateway,
            wallet,
            inventory,
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());
    }

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid customerUserId,
        Guid[] lineIds,
        Guid? reservationId) =>
        new(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerUserId,
            true,
            "IRR",
            lineIds
                .Select(x => new OrderReturnLineSnapshot(x, 1m, 1000m, "IRR", reservationId, true, 7))
                .ToArray());

    private static ReturnsDbContext CreateDb(string? name = null, FailingSaveInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString("N"))
            // The provider cannot carry a transaction. The owning decision write opens one unconditionally, so
            // the harness declares its transaction a no-op instead of reading a provider warning as a failure.
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new ReturnsDbContext(builder.Options);
    }

    /// <summary>
    /// TB-273: a switchable local persistence failure. It lets a fact prove that an external success whose write did
    /// not land is reported as a failure and never as durable money.
    /// </summary>
    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public int FailuresRemaining { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (FailuresRemaining > 0)
            {
                FailuresRemaining--;
                throw new InvalidOperationException("tb273 simulated local persistence failure");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>A closed platform seam: the owning use case refuses before any money, stock or attempt effect.</summary>
    private sealed class ClosedGuard : IReturnUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) =>
            throw new ContractOperationException("platform.use_case.closed");
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class MapOrderReader(IReadOnlyList<OrderReturnContextSnapshot> orders) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(orders.FirstOrDefault(x => x.SellerOrderId == sellerOrderId));
    }

    private sealed class MapFulfillmentReader(IReadOnlyList<OrderReturnContextSnapshot> orders)
        : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken)
        {
            var order = orders.FirstOrDefault(x => x.SellerOrderId == sellerOrderId);
            if (order is null)
            {
                return Task.FromResult<FulfillmentReturnEligibilitySnapshot?>(null);
            }

            return Task.FromResult<FulfillmentReturnEligibilitySnapshot?>(new FulfillmentReturnEligibilitySnapshot(
                sellerOrderId,
                order.Lines.ToDictionary(x => x.OrderLineId, x => x.Quantity),
                Now,
                order.Lines.ToDictionary(x => x.OrderLineId, _ => Now),
                order.Lines.Select(x => new LineDeliverySlice(x.OrderLineId, x.Quantity, Now)).ToArray()));
        }

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, DateTimeOffset?> map = orders
                .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
                .ToDictionary(x => x.SellerOrderId, _ => (DateTimeOffset?)Now);
            return Task.FromResult(map);
        }
    }

    private sealed class FixedPaymentReader : IPaymentReturnReader
    {
        private readonly Guid _paymentId = Guid.NewGuid();

        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid payment,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(
                    payment == Guid.Empty ? _paymentId : payment, Guid.NewGuid(), 1000m, "IRR", "Succeeded"));

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(_paymentId, checkoutId, 1000m, "IRR", "Succeeded"));
    }

    private sealed class ScriptedGateway : IPaymentRefundGateway
    {
        private readonly Queue<Func<GatewayRefundResult>> _script = new();

        public List<MoneyCall> Calls { get; } = [];

        public Func<bool>? TransactionProbe { get; set; }

        public Func<(bool Transaction, RefundAttemptStatus Status)>? Probe { get; set; }

        public void EnqueueSuccess(string? reference) =>
            _script.Enqueue(() => new GatewayRefundResult(true, reference, null));

        public void EnqueueProviderRefusal(string failureCode) =>
            _script.Enqueue(() => new GatewayRefundResult(false, null, failureCode));

        public void EnqueueUnknown() => _script.Enqueue(() => throw new TimeoutException("refund outcome unknown"));

        /// <summary>TB-273 B: the owning Payment contract's exact "no refund adapter is configured" code.</summary>
        public void EnqueueUnconfigured() => _script.Enqueue(() =>
            throw new ContractOperationException("payment.refund.gateway.unconfigured"));

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var observed = Probe?.Invoke();
            Calls.Add(new MoneyCall(
                paymentId,
                amount,
                idempotencyKey,
                TransactionProbe?.Invoke() ?? false,
                observed?.Transaction ?? false,
                observed?.Status ?? RefundAttemptStatus.Pending));

            var next = _script.Count > 0
                ? _script.Dequeue()
                : () => new GatewayRefundResult(true, $"ref-{Calls.Count}", null);
            return Task.FromResult(next());
        }
    }

    private sealed record MoneyCall(
        Guid PaymentId,
        decimal Amount,
        string IdempotencyKey,
        bool InsideTransaction,
        bool TransactionOpen,
        RefundAttemptStatus StatusWhileCalling);

    private sealed class ScriptedWallet : IWalletRefundCreditPort
    {
        private readonly Queue<bool> _unknown = new();

        public List<(Guid CustomerActorUserId, decimal Amount, Guid ReturnRequestId, string IdempotencyKey)>
            Calls
        { get; } = [];

        public Func<bool>? TransactionProbe { get; set; }

        public void EnqueueSuccess() => _unknown.Enqueue(false);

        public void EnqueueUnknown() => _unknown.Enqueue(true);

        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            _ = TransactionProbe?.Invoke();
            Calls.Add((customerActorUserId, amount, returnRequestId, idempotencyKey));
            if (_unknown.Count > 0 && _unknown.Dequeue())
            {
                throw new TimeoutException("wallet outcome unknown");
            }

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
                throw new InvalidOperationException("inventory restock failed");
            }

            // The owning Inventory contract is idempotent by key: a repeated retry is ONE physical restock.
            if (!Restocked.Any(x => string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)))
            {
                Restocked.Add((reservationId, quantity, idempotencyKey));
            }

            return Task.CompletedTask;
        }
    }
}
