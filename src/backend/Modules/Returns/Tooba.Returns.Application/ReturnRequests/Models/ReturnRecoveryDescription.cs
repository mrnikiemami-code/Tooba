using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-273: the ONE authority that answers "what can still be recovered for this case?" from STORED facts only.
/// <para>
/// Money and physical work are separate owners' phases: a successful refund does not imply the received units
/// were settled, and a settled stock phase does not imply the money was released. The description therefore
/// reports financial and physical completion as two independent facts and never guesses an action from a status
/// alone.
/// </para>
/// <para>
/// TB-273-R1 (A): the policy is FAIL CLOSED. It is the single evidence rule shared by the admin read projection and
/// the recovery command, so a permissive read can never disagree with a strict action. It requires a recorded
/// durable APPROVE decision, an attempt that binds to THIS case (stored payment, exact amount, currency and a
/// non-empty identity/key), and — for a courier-origin case — coherent source/claim/receipt links plus the owning
/// obligation/stock closure markers. Anything it cannot prove is <see cref="ReturnRecoveryPhase.Blocked"/>, never a
/// guessed resume and never an invented completion.
/// </para>
/// <para>
/// TB-273-R2: the same rule is enforced BEFORE any effectful courier recovery — including unknown (pending) money,
/// not only a confirmed one — and the recorded final marker is only read as completion when the PRIOR durable closure
/// timestamps exist, exactly as the finalization writer requires.
/// </para>
/// </summary>
public enum ReturnRecoveryPhase
{
    /// <summary>No approved financial attempt; there is nothing to recover (requested/rejected/cancelled).</summary>
    None = 0,

    /// <summary>An approval exists and exactly ONE bound attempt is still pending: the money outcome is unknown.</summary>
    PendingMoney = 1,

    /// <summary>A definite financial failure. The existing deliberate new-attempt retry is a DIFFERENT intent.</summary>
    FailedMoney = 2,

    /// <summary>
    /// The money is confirmed and an owning physical step is unfinished. For a courier-origin case this includes the
    /// state where EVERY item disposition is durable but the final closure marker is still missing (TB-273-R1): that
    /// case stays recoverable and the physical intent resumes only the remaining closure work.
    /// </summary>
    PendingDisposition = 3,

    /// <summary>
    /// Money confirmed AND every required durable physical fact (item dispositions plus, for a courier-origin case,
    /// the owning closure) recorded: the recorded completion.
    /// </summary>
    Settled = 4,

    /// <summary>Inconsistent/ambiguous stored binding or missing owning physical evidence: a typed blocker, no guess.</summary>
    Blocked = 5,

    /// <summary>
    /// TB-277: the case is lawfully APPROVED and owns no refund attempt at all. This is the deliberate decision-only
    /// state: the first money execution is a SEPARATE intent (<see cref="ReturnRecoveryKind"/> deliberately does not
    /// name it). It is not "unknown money" and not an unbound attempt — it is the recorded answer to a real step that
    /// simply has not been taken yet. Appended after <see cref="Blocked"/> so every shipped numeric value keeps its
    /// meaning.
    /// </summary>
    AwaitingFirstRefund = 6,
}

/// <summary>
/// TB-273: the exact recovery intent a client may ask for. This is deliberately NOT the existing
/// <c>retry_refund</c> intent (which mints a NEW attempt for a definite failure).
/// </summary>
public enum ReturnRecoveryKind
{
    /// <summary>Repeat the EXISTING still-pending attempt with its recorded key, payment, amount and currency.</summary>
    ResumePendingRefund = 0,

    /// <summary>Settle the physical disposition only: zero refund/Wallet calls, zero new attempts.</summary>
    SettlePhysicalDisposition = 1,
}

/// <summary>
/// TB-273: the additive read description of one case's recovery state. Every field is derived from stored
/// case/decision/attempt/item facts; computing it never contacts a provider, retries money, restocks or writes.
/// </summary>
public sealed record ReturnRecoveryDescription(
    ReturnRecoveryPhase Phase,
    ReturnRecoveryKind? RecoveryKind,
    Guid? RefundAttemptId,
    string? BlockerCode,
    bool FinancialComplete,
    bool PhysicalComplete);

/// <summary>
/// TB-273 / TB-273-R1: pure recovery projection over stored facts. The same policy is reused by the recovery
/// command (to decide what is executable) and by every admin read projection (to describe what is stored), so a
/// read and an action can never disagree about the state of one case.
/// </summary>
public static class ReturnRecoveryPolicy
{
    /// <summary>The exact durable decision value the owning approval records.</summary>
    public const string ApprovedDecision = "Approve";

    /// <summary>The exact durable decision value the owning rejection records.</summary>
    public const string RejectedDecision = "Reject";

    /// <summary>Describes the recovery state of the given stored case snapshot.</summary>
    public static ReturnRecoveryDescription Describe(ReturnSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var allItemsDisposed = snapshot.Items.Count > 0
            && snapshot.Items.All(x => x.Disposition != ReturnItemDisposition.None);

        switch (snapshot.Status)
        {
            case ReturnRequestStatus.Requested:
            case ReturnRequestStatus.Rejected:
            case ReturnRequestStatus.Cancelled:
                // A case that was never approved owns no financial attempt. A stored attempt on such a status is a
                // CONTRADICTORY fact, not an executable recovery: it is described as blocked instead of "nothing to
                // recover", so the inconsistency is never hidden behind a quiet read.
                if (snapshot.RefundAttempts.Count > 0)
                {
                    return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: false, allItemsDisposed);
                }

                // TB-274-R1: a receipt whose stored rows contradict the request can never be read as physical evidence.
                if (snapshot.Receiving is { IsCoherent: false })
                {
                    return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: false, physicalComplete: false);
                }

                // TB-274-R1 (C): a case that was rejected/cancelled AFTER goods were physically received still owes an
                // honest remaining physical resolution. The fact is reported with NO recovery kind (there is no
                // executable stock intent for it), the recorded receipt history stays visible, and nothing is
                // restocked, refunded or shipped back automatically.
                // The blocker is deliberately limited to a case that was DECIDED AWAY: a still-Requested case awaiting
                // its remaining deliveries has no owed resolution yet, so it must not be reported as blocked.
                if (snapshot.Status is ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled
                    && allItemsDisposed is false
                    && HasUnresolvedReceivedGoods(snapshot))
                {
                    return Blocked(
                        ReturnsErrorCodes.RecoveryPhysicalResolutionRequired,
                        financialComplete: false,
                        physicalComplete: false);
                }

                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.None, null, null, null, FinancialComplete: false, allItemsDisposed);

            case ReturnRequestStatus.Approved:
            case ReturnRequestStatus.RefundProcessing:
            {
                var money = ReadMoney(snapshot);
                if (money.Complete)
                {
                    // A bound success is the ONE coherent current outcome, even when the case status row lags it.
                    return Physical(snapshot, money, allItemsDisposed);
                }

                if (money.Absent)
                {
                    // TB-277: approval is a DECISION and the first financial step is its own deliberate intent. An
                    // Approved case that owns no attempt at all is therefore the honest "awaiting first refund"
                    // state — not "unknown money" and not an unbound attempt. It is only offered when every
                    // lawful precondition of the money step is already a stored fact; anything it cannot prove is
                    // the same typed blocker the action itself raises, with no executable kind.
                    if (snapshot.Status != ReturnRequestStatus.Approved)
                    {
                        // A RefundProcessing case with no attempt contradicts itself: the status is only written
                        // together with the durable attempt, so it is never read as "ready to start".
                        return Blocked(ReturnsErrorCodes.RecoveryAttemptUnbound, financialComplete: false, allItemsDisposed);
                    }

                    if (snapshot.Origin != ReturnOrigin.CourierOrigin)
                    {
                        if (snapshot.Receiving is { IsCoherent: false })
                        {
                            return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: false, allItemsDisposed);
                        }

                        // TB-279 (C): money may only start on a resolution that actually names the accepted money.
                        // A stored reviewed plan that is incomplete/incoherent blocks the first refund; a case with
                        // no plan at all keeps the legacy approved amount (the pre-279 case), and complete receiving
                        // already derives a complete baseline plan.
                        if (ReturnResolutionPolicy.Resolve(snapshot) is { IsComplete: false, FromReviewedPlan: true } incomplete)
                        {
                            return Blocked(
                                incomplete.BlockerCode ?? ReturnsErrorCodes.ResolutionRequired,
                                financialComplete: false,
                                allItemsDisposed);
                        }
                    }
                    else if (!HasCoherentCourierSource(snapshot))
                    {
                        return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: false, physicalComplete: false);
                    }

                    if (snapshot.PaymentId is not { } firstPaymentId || firstPaymentId == Guid.Empty)
                    {
                        // The money step needs the case's FIXED payment binding; without it no first attempt may
                        // name an amount/destination, so the read reports it as a typed blocker.
                        return Blocked(ReturnsErrorCodes.RefundPaymentMissing, financialComplete: false, allItemsDisposed);
                    }

                    return new ReturnRecoveryDescription(
                        ReturnRecoveryPhase.AwaitingFirstRefund,
                        RecoveryKind: null,
                        RefundAttemptId: null,
                        BlockerCode: null,
                        FinancialComplete: false,
                        allItemsDisposed);
                }

                if (money.IsPending && money.AttemptId is { } pendingId)
                {
                    // TB-273-R2 (A): unknown courier money is an EFFECTFUL recovery, so the SAME courier source
                    // coherence rule is required BEFORE offering it — not only after the money succeeded. Otherwise a
                    // malformed receipt/source/condition would be advertised as resumable and could reach the money
                    // phase (and the owning closure helper) without any source validation. The blocker is typed and
                    // carries no RecoveryKind, so neither a read nor an action can offer an execution.
                    if (snapshot.Origin == ReturnOrigin.CourierOrigin && !HasCoherentCourierSource(snapshot))
                    {
                        return Blocked(
                            ReturnsErrorCodes.RecoveryEvidenceConflict,
                            financialComplete: false,
                            physicalComplete: false);
                    }

                    // TB-274 (B): an ordinary delivered RMA may only continue its unknown money over its OWN recorded
                    // physical evidence. A missing/incomplete receipt is a typed blocker (no kind, no new key); an
                    // incoherent receipt is the same evidence conflict every other ordinary phase reports. The actual
                    // refusal lives in the owning directory, so the read can never advertise an execution it refuses.
                    if (snapshot.Origin != ReturnOrigin.CourierOrigin)
                    {
                        if (snapshot.Receiving is { IsCoherent: false })
                        {
                            return Blocked(
                                ReturnsErrorCodes.RecoveryEvidenceConflict,
                                financialComplete: false,
                                physicalComplete: false,
                                attemptId: money.AttemptId);
                        }

                        if (snapshot.Receiving is not { IsComplete: true })
                        {
                            return Blocked(
                                ReturnsErrorCodes.RecoveryReceivingRequired,
                                financialComplete: false,
                                physicalComplete: false,
                                attemptId: money.AttemptId);
                        }
                    }

                    return new ReturnRecoveryDescription(
                        ReturnRecoveryPhase.PendingMoney,
                        ReturnRecoveryKind.ResumePendingRefund,
                        pendingId,
                        null,
                        FinancialComplete: false,
                        allItemsDisposed);
                }

                return Blocked(
                    money.BlockerCode ?? ReturnsErrorCodes.RecoveryAttemptUnbound,
                    financialComplete: false,
                    allItemsDisposed);
            }

            case ReturnRequestStatus.RefundFailed:
            {
                var money = ReadMoney(snapshot);
                if (money.Complete)
                {
                    // A succeeded bound attempt on an explicitly failed case contradicts itself. The money FACT is
                    // preserved so it is never re-sent, but it never authorizes stock work either.
                    return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: true, allItemsDisposed);
                }

                if (money.IsPending)
                {
                    // A live pending attempt on a failed case is an interrupted retry, not a definite failure.
                    return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: false, allItemsDisposed);
                }

                if (money.AttemptId is { } failedId && money.BlockerCode is null)
                {
                    // The definite failure is its own phase; the new-attempt retry is a DIFFERENT intent.
                    return new ReturnRecoveryDescription(
                        ReturnRecoveryPhase.FailedMoney, null, failedId, null, FinancialComplete: false, allItemsDisposed);
                }

                return Blocked(
                    money.BlockerCode ?? ReturnsErrorCodes.RecoveryAttemptUnbound,
                    financialComplete: false,
                    allItemsDisposed);
            }

            case ReturnRequestStatus.Completed:
            {
                var money = ReadMoney(snapshot);
                if (!money.Complete)
                {
                    // A completed case whose money row cannot be positively bound is inconsistent, not settled.
                    return Blocked(
                        money.BlockerCode ?? ReturnsErrorCodes.RecoveryAttemptUnbound,
                        financialComplete: false,
                        allItemsDisposed);
                }

                return Physical(snapshot, money, allItemsDisposed);
            }

            default:
                return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: false, allItemsDisposed);
        }
    }

    /// <summary>
    /// TB-273-R1 (A): the single financial evidence rule. Exactly ONE coherent current outcome is required: a bound
    /// attempt whose stored payment, exact amount, currency and non-empty identity/key all match this very case. A
    /// foreign, ambiguous or contradictory live row is a blocker — never a "first/latest" guess — while older FAILED
    /// history is preserved without being confused with a pending attempt.
    /// </summary>
    private static MoneyEvidence ReadMoney(ReturnSnapshot snapshot)
    {
        var effectiveDecision = EffectiveDecisionOf(snapshot);
        if (string.Equals(effectiveDecision, RejectedDecision, StringComparison.Ordinal))
        {
            // The durable rejection is the winning decision: no recovery may invent an approval for it.
            return new MoneyEvidence(false, null, false, ReturnsErrorCodes.AlreadyRejected);
        }

        if (!string.Equals(effectiveDecision, ApprovedDecision, StringComparison.Ordinal))
        {
            // Effectful recovery requires a RECORDED approval. A missing row is not repaired from a status or a caller.
            return new MoneyEvidence(false, null, false, ReturnsErrorCodes.RecoveryEvidenceConflict);
        }

        var attempts = snapshot.RefundAttempts;
        if (attempts.Count == 0)
        {
            // TB-277: an approved case that never started money owns NO attempt. That is a truthful stored fact, not
            // an ambiguous/unbound one, so it is reported distinctly instead of as attempt_unbound.
            return new MoneyEvidence(false, null, false, null, Absent: true);
        }

        var boundSucceeded = attempts
            .Where(x => x.Status == RefundAttemptStatus.Succeeded && IsBoundToCase(snapshot, x))
            .ToArray();
        var boundPending = attempts
            .Where(x => x.Status == RefundAttemptStatus.Pending && IsBoundToCase(snapshot, x))
            .ToArray();
        var boundFailed = attempts
            .Where(x => x.Status == RefundAttemptStatus.Failed && IsBoundToCase(snapshot, x))
            .ToArray();
        var foreignLive = attempts
            .Where(x => x.Status is RefundAttemptStatus.Pending or RefundAttemptStatus.Succeeded
                        && !IsBoundToCase(snapshot, x))
            .ToArray();

        if (foreignLive.Length > 0)
        {
            // A live attempt that answers for another payment/amount/currency can never authorize this case.
            return new MoneyEvidence(false, null, false, ReturnsErrorCodes.RecoveryAttemptUnbound);
        }

        if (boundSucceeded.Length > 1 || boundPending.Length > 1
            || (boundSucceeded.Length == 1 && boundPending.Length == 1))
        {
            // Ambiguous: more than one live row could be "the" outcome. No resume is guessed from it.
            return new MoneyEvidence(false, null, false, ReturnsErrorCodes.RecoveryAttemptUnbound);
        }

        if (boundSucceeded.Length == 1)
        {
            return new MoneyEvidence(true, boundSucceeded[0].RefundAttemptId, false, null);
        }

        if (boundPending.Length == 1)
        {
            return new MoneyEvidence(false, boundPending[0].RefundAttemptId, true, null);
        }

        // No live bound row. Only a definite failure may answer from bound FAILED history — and only the CURRENT one:
        // a failure that already produced its derived child is history, so the child's own failure is what a NEW
        // deliberate review answers for. Ambiguous history is never a first/latest guess.
        var currentFailure = boundFailed.Length == 1
            ? boundFailed[0].RefundAttemptId
            : ReturnRetryIdentity.CurrentReviewableFailureId(snapshot.ReturnRequestId, boundFailed);
        return currentFailure is { } failed
            ? new MoneyEvidence(false, failed, false, null)
            : new MoneyEvidence(false, null, false, ReturnsErrorCodes.RecoveryAttemptUnbound);
    }

    /// <summary>
    /// TB-273-R1 (A): an attempt is "bound" only when EVERY stored identity fact of this case matches it. No
    /// fallback to the newest/oldest row and no tolerance for a blank key or a zero id.
    /// </summary>
    private static bool IsBoundToCase(ReturnSnapshot snapshot, RefundAttemptSnapshot attempt) =>
        attempt.RefundAttemptId != Guid.Empty
        && !string.IsNullOrWhiteSpace(attempt.IdempotencyKey)
        && snapshot.PaymentId is { } paymentId
        && paymentId != Guid.Empty
        && attempt.PaymentId == paymentId
        // TB-279 (C): the bound money is the REVIEWED approved amount (accepted quantities × stored unit prices), not
        // the original claim of a partially resolved case.
        && attempt.Amount == ReturnResolutionPolicy.MoneyOf(snapshot)
        && string.Equals(attempt.Currency.Trim(), snapshot.Currency.Trim(), StringComparison.Ordinal);

    /// <summary>
    /// TB-279 (B): the effective decision of a snapshot. A snapshot produced by the owning read always carries it; a
    /// hand-built snapshot falls back to the legacy baseline only when no revision exists.
    /// </summary>
    private static string? EffectiveDecisionOf(ReturnSnapshot snapshot) =>
        snapshot.EffectiveDecision
        ?? (snapshot.DecisionVersion > 0 ? null : snapshot.RecordedDecision);

    /// <summary>
    /// TB-273-R1 (A/C): the physical phase from durable facts. Money is already confirmed here. A courier-origin case
    /// owns its receipt/claim/closure, so its completion requires all three; every item disposition being durable
    /// while the FINAL closure marker is still missing is explicitly <see cref="ReturnRecoveryPhase.PendingDisposition"/>
    /// (recoverable, zero money), never a silent <see cref="ReturnRecoveryPhase.Settled"/>.
    /// </summary>
    private static ReturnRecoveryDescription Physical(
        ReturnSnapshot snapshot,
        MoneyEvidence money,
        bool allItemsDisposed)
    {
        if (snapshot.Items.Count == 0)
        {
            // No received line means no provable physical phase at all.
            return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: true, physicalComplete: false, money.AttemptId);
        }

        if (snapshot.Origin != ReturnOrigin.CourierOrigin)
        {
            // TB-274-R1 (C): an already-durable disposition stays the recorded history, but a line that restocked a
            // usable quantity WITHOUT any owning stock binding was never confirmed by Inventory, so it can never make
            // the case "Settled". The check runs before the completion answer for that reason.
            if (HasMissingStockBinding(snapshot))
            {
                return Blocked(ReturnsErrorCodes.RecoveryStockBindingRequired, financialComplete: true, physicalComplete: false, money.AttemptId);
            }

            // TB-279 (C): ordinary completion is the RESUMED per-portion fact — every accepted usable portion really
            // restocked, every accepted damaged portion recorded non-saleable, and every rejected received portion
            // really handed back. A single enum on the item can never express a mixed line, so a received portion that
            // is still owed to the owning customer keeps the case open instead of reading as settled.
            if (ReturnResolutionPolicy.IsPhysicalComplete(snapshot))
            {
                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.Settled, null, money.AttemptId, null, FinancialComplete: true, PhysicalComplete: true);
            }

            // TB-274: a delivered-customer case owns its recorded customer receipt. A complete, coherent receipt makes
            // the physical phase real, executable work over the INSPECTED usable quantity; an incoherent receipt is a
            // typed evidence conflict; a missing/incomplete receipt keeps the explicit blocker and never restocks the
            // requested quantity as if it had arrived usable.
            if (snapshot.Receiving is { IsCoherent: false })
            {
                return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: true, physicalComplete: false, money.AttemptId);
            }

            if (snapshot.Receiving is { IsComplete: true } && ReturnResolutionPolicy.Resolve(snapshot) is { IsComplete: true })
            {
                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.PendingDisposition,
                    ReturnRecoveryKind.SettlePhysicalDisposition,
                    money.AttemptId,
                    null,
                    FinancialComplete: true,
                    PhysicalComplete: false);
            }

            // The physical gate keeps its EXISTING precedence (TB-274): a missing/incomplete customer receipt is
            // receiving work, never a resolution problem. Only a complete receipt with an unresolved plan names the
            // resolution blocker.
            if (snapshot.Receiving is not { IsComplete: true })
            {
                return Blocked(
                    ReturnsErrorCodes.RecoveryReceivingRequired,
                    financialComplete: true,
                    physicalComplete: false,
                    money.AttemptId);
            }

            return Blocked(
                ReturnResolutionPolicy.Resolve(snapshot).BlockerCode ?? ReturnsErrorCodes.ResolutionRequired,
                financialComplete: true,
                physicalComplete: false,
                money.AttemptId);
        }

        if (!HasCoherentCourierSource(snapshot))
        {
            // Fail closed: a case whose source/claim/receipt/line or condition/disposition facts contradict each
            // other can never be used to move stock, and the money fact is preserved rather than re-sent.
            return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: true, physicalComplete: false, money.AttemptId);
        }

        if (snapshot.ClosurePhase == CourierOriginClosurePhase.RefundReleased && !allItemsDisposed)
        {
            // The final marker can only be truthful once every received line recorded its disposition.
            return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: true, physicalComplete: false, money.AttemptId);
        }

        if (snapshot.ClosurePhase == CourierOriginClosurePhase.RefundReleased
            && (snapshot.ObligationClosedAt is null || snapshot.StockSettledAt is null))
        {
            // TB-273-R2 (B): a recorded final marker is only Settled when the PRIOR durable closure evidence the
            // finalization writer itself requires is also durable. A forged/incomplete Phase 3 marker whose
            // obligation or stock timestamps never landed is INCONSISTENT: the same typed conflict is reported, the
            // confirmed money fact is preserved, and nothing is repaired, backfilled or completed automatically.
            return Blocked(ReturnsErrorCodes.RecoveryEvidenceConflict, financialComplete: true, physicalComplete: false, money.AttemptId);
        }

        if (allItemsDisposed && snapshot.ClosurePhase == CourierOriginClosurePhase.RefundReleased)
        {
            return new ReturnRecoveryDescription(
                ReturnRecoveryPhase.Settled, null, money.AttemptId, null, FinancialComplete: true, PhysicalComplete: true);
        }

        // Either an item disposition, or the owning closure/final marker, is still pending. Both are the SAME
        // physical intent and neither may call money again; when only the marker is missing the intent resumes just it.
        return new ReturnRecoveryDescription(
            ReturnRecoveryPhase.PendingDisposition,
            ReturnRecoveryKind.SettlePhysicalDisposition,
            money.AttemptId,
            null,
            FinancialComplete: true,
            PhysicalComplete: false);
    }

    /// <summary>
    /// TB-273-R1 (A): the durable courier-origin source evidence. The case must name its fulfillment unit, parcel,
    /// receipt and exclusive claim; every received line must point at that SAME receipt/parcel; and each recorded
    /// condition must carry the disposition that condition can legally produce.
    /// </summary>
    private static bool HasCoherentCourierSource(ReturnSnapshot snapshot)
    {
        if (snapshot.SourceFulfillmentId is not { } fulfillmentId || fulfillmentId == Guid.Empty
            || snapshot.SourceShipmentId is not { } shipmentId || shipmentId == Guid.Empty
            || snapshot.SourceReceiptId is not { } receiptId || receiptId == Guid.Empty
            || snapshot.SourceClaimId is not { } claimId || claimId == Guid.Empty
            || string.IsNullOrWhiteSpace(snapshot.SourceOccurrenceKey))
        {
            return false;
        }

        foreach (var item in snapshot.Items)
        {
            if (item.SourceReceiptId != receiptId || item.SourceShipmentId != shipmentId)
            {
                return false;
            }

            var coherent = item.Condition == ReturnItemCondition.Damaged
                ? item.Disposition is ReturnItemDisposition.None or ReturnItemDisposition.DamagedNonSaleable
                : item.Disposition is ReturnItemDisposition.None or ReturnItemDisposition.SaleableRestock;

            if (!coherent)
            {
                return false;
            }
        }

        return true;
    }

    private static ReturnRecoveryDescription Blocked(
        string code,
        bool financialComplete,
        bool physicalComplete,
        Guid? attemptId = null) =>
        new(ReturnRecoveryPhase.Blocked, null, attemptId, code, financialComplete, physicalComplete);

    /// <summary>
    /// TB-274-R1 (C): true when at least one requested line physically received goods but carries no recorded
    /// disposition yet — the honest remaining physical work of a case that was decided away.
    /// </summary>
    private static bool HasUnresolvedReceivedGoods(ReturnSnapshot snapshot)
    {
        if (snapshot.Receiving is not { IsCoherent: true } receiving)
        {
            return false;
        }

        return snapshot.Items.Any(item =>
            receiving.WasReceived(item.ReturnItemId) && item.Disposition == ReturnItemDisposition.None);
    }

    /// <summary>
    /// TB-274-R1 (C): true when a COMPLETE, coherent receipt inspected a positive usable quantity for a line that has no
    /// owning stock binding. Such a line can never be restocked (there is no Inventory reservation to confirm), so the
    /// physical phase is a typed pending blocker instead of a claimed completion. Nothing is guessed, reserved or
    /// created here.
    /// </summary>
    private static bool HasMissingStockBinding(ReturnSnapshot snapshot)
    {
        if (snapshot.Receiving is not { IsCoherent: true, IsComplete: true } receiving)
        {
            return false;
        }

        foreach (var line in receiving.Lines)
        {
            if (line.Usable <= 0m)
            {
                continue;
            }

            var item = snapshot.Items.FirstOrDefault(x => x.ReturnItemId == line.ReturnItemId);
            if (item is null || item.ReservationId is not { } reservationId || reservationId == Guid.Empty)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The exact financial answer of ONE case: complete money, a single pending unknown, a typed blocker, or — for a
    /// case that never started money — the explicit <paramref name="Absent"/> fact.
    /// </summary>
    private readonly record struct MoneyEvidence(
        bool Complete,
        Guid? AttemptId,
        bool IsPending,
        string? BlockerCode,
        bool Absent = false);
}
