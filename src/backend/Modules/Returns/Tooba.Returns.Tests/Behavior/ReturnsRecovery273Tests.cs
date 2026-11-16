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
            ReturnRequestStatus.RefundProcessing, attempts: [(attemptId, RefundAttemptStatus.Pending)]));
        Assert.Equal(ReturnRecoveryPhase.PendingMoney, pending.Phase);
        Assert.Equal(ReturnRecoveryKind.ResumePendingRefund, pending.RecoveryKind);
        Assert.Equal(attemptId, pending.RefundAttemptId);
        Assert.False(pending.FinancialComplete);
        Assert.Null(pending.BlockerCode);

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

        // Every received line disposed is the recorded completion.
        var settled = Describe(Snap(
            ReturnRequestStatus.Completed,
            ReturnOrigin.CourierOrigin,
            attempts: [succeeded],
            dispositions: [ReturnItemDisposition.SaleableRestock]));
        Assert.Equal(ReturnRecoveryPhase.Settled, settled.Phase);
        Assert.Null(settled.RecoveryKind);
        Assert.True(settled.FinancialComplete);
        Assert.True(settled.PhysicalComplete);

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

        // Money success is NOT physical completion: the separate disposition stays honestly pending.
        Assert.True(recovered.Recovery!.FinancialComplete);
        Assert.False(recovered.Recovery.PhysicalComplete);
        Assert.Equal(ReturnItemDisposition.None, (await order.Items()).Single().Disposition);

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
        await using var order = await SeedOrdinaryCaseAsync(StoredState.MoneyConfirmedNoDisposition);
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

    // ------------------------------------------------------------------ fixtures

    private static ReturnRecoveryDescription Describe(ReturnSnapshot snapshot) =>
        ReturnRecoveryPolicy.Describe(snapshot);

    private static ReturnSnapshot Snap(
        ReturnRequestStatus status,
        ReturnOrigin origin = ReturnOrigin.CustomerDelivered,
        (Guid Id, RefundAttemptStatus Status)[]? attempts = null,
        ReturnItemDisposition[]? dispositions = null,
        decimal refundAmount = 1000m)
    {
        var effective = dispositions ?? [ReturnItemDisposition.None];
        var approved = status is not (ReturnRequestStatus.Requested
            or ReturnRequestStatus.Rejected
            or ReturnRequestStatus.Cancelled);
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
            approved ? Guid.NewGuid() : null,
            RefundDestination.OriginalPayment,
            Now,
            Now,
            effective
                .Select(disposition => new ReturnItemSnapshot(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1m,
                    1000m,
                    "IRR",
                    null,
                    ReturnItemCondition.Usable,
                    disposition,
                    null,
                    null))
                .ToArray(),
            (attempts ?? [])
                .Select(x => new RefundAttemptSnapshot(
                    x.Id,
                    Guid.NewGuid(),
                    refundAmount,
                    "IRR",
                    x.Status,
                    "refund-key",
                    x.Status == RefundAttemptStatus.Succeeded ? "ref" : null,
                    x.Status == RefundAttemptStatus.Failed ? "DECLINED" : null,
                    Now,
                    x.Status == RefundAttemptStatus.Pending ? null : Now))
                .ToArray(),
            origin);
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

        public async Task<IReadOnlyList<ReturnItem>> Items() =>
            await _db.ReturnItems.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }

    private static async Task<CaseHarness> SeedOrdinaryCaseAsync(
        StoredState state = StoredState.UnknownMoney,
        RefundDestination destination = RefundDestination.OriginalPayment,
        IReturnUseCaseGuard? guard = null)
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

    private static async Task<CaseHarness> SeedCourierCaseAsync()
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
        attempt.MarkSucceeded("ref-273-courier", Now);
        request.MarkRefundSucceeded(Now);

        // The owning closure phases were recorded BEFORE the money by the approval: recovery must not redo them.
        request.MarkObligationClosed(Now);
        request.MarkStockSettled(1m, Now);
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

        public void EnqueueSuccess(string reference) =>
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

            Restocked.Add((reservationId, quantity, idempotencyKey));
            return Task.CompletedTask;
        }
    }
}
