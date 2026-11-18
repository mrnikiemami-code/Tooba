using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-275: the ONE work stage of a case in the operator's return workflow. It is derived from the SAME stored facts
/// the case detail and every action read, so a queue bucket can never disagree with the workspace it opens.
/// <para>
/// The stage answers "what is the next honest piece of work on this case?", never "what label should this status have?":
/// a customer-delivered case with no recorded receipt is <see cref="Receiving"/> before it is a decision, an approved
/// case whose money is still unknown is <see cref="Refund"/>, a definite failure is <see cref="RefundFailed"/>, and
/// money that is already released over unsettled physical goods is <see cref="Disposition"/>.
/// </para>
/// </summary>
public enum ReturnWorkCategory
{
    /// <summary>Decided away, cancelled, or a closed case with nothing executable left.</summary>
    Closed = 0,

    /// <summary>The customer handed goods over; receiving/inspection is the outstanding physical work.</summary>
    Receiving = 1,

    /// <summary>Nothing was approved yet: the case is waiting for an approving or rejecting decision.</summary>
    Decision = 2,

    /// <summary>Approved money whose outcome is still unknown (or not yet started).</summary>
    Refund = 3,

    /// <summary>A durable definite financial failure that needs a deliberate reviewed retry.</summary>
    RefundFailed = 4,

    /// <summary>Money is confirmed and received goods still owe a physical disposition.</summary>
    Disposition = 5,

    /// <summary>Money confirmed and every durable physical fact recorded: the recorded completion.</summary>
    Settled = 6,
}

/// <summary>TB-275: stable wire codes of the work stages (the UI owns the Persian labels).</summary>
public static class ReturnWorkCategoryCodes
{
    /// <summary>Closed.</summary>
    public const string Closed = "closed";

    /// <summary>Receiving/inspection.</summary>
    public const string Receiving = "receiving";

    /// <summary>Awaiting a decision.</summary>
    public const string Decision = "decision";

    /// <summary>Refund in progress/unknown.</summary>
    public const string Refund = "refund";

    /// <summary>Refund definitely failed.</summary>
    public const string RefundFailed = "refund_failed";

    /// <summary>Physical disposition pending.</summary>
    public const string Disposition = "disposition";

    /// <summary>Settled.</summary>
    public const string Settled = "settled";

    /// <summary>The exact wire code of a stage.</summary>
    public static string Of(ReturnWorkCategory category) => category switch
    {
        ReturnWorkCategory.Receiving => Receiving,
        ReturnWorkCategory.Decision => Decision,
        ReturnWorkCategory.Refund => Refund,
        ReturnWorkCategory.RefundFailed => RefundFailed,
        ReturnWorkCategory.Disposition => Disposition,
        ReturnWorkCategory.Settled => Settled,
        _ => Closed,
    };
}

/// <summary>
/// TB-275: stable wire codes of the review actions a case may offer. They reuse the EXISTING published codes
/// (<c>approve_return</c>/<c>reject_return</c>/<c>retry_refund</c>) and the existing recovery kind tokens
/// (<c>resume_pending_refund</c>/<c>settle_physical_disposition</c>), so a queue row and the case workspace cannot
/// name an intent the module does not own.
/// </summary>
public static class ReturnWorkActionCodes
{
    /// <summary>Record one physical customer receipt occurrence.</summary>
    public const string RecordReceipt = "record_receipt";

    /// <summary>Approve the case.</summary>
    public const string Approve = "approve_return";

    /// <summary>Reject the case.</summary>
    public const string Reject = "reject_return";

    /// <summary>Repeat the exact stored pending refund attempt.</summary>
    public const string ResumeRefund = "resume_pending_refund";

    /// <summary>Deliberately retry a definite financial failure.</summary>
    public const string RetryRefund = "retry_refund";

    /// <summary>Settle the physical disposition only.</summary>
    public const string SettlePhysical = "settle_physical_disposition";

    /// <summary>
    /// TB-277: deliberately execute the FIRST refund of an approved-but-not-yet-started case. It is a distinct verb
    /// from <see cref="ResumeRefund"/> (repeat the same unknown attempt) and <see cref="RetryRefund"/> (mint a child
    /// attempt for a definite failure).
    /// </summary>
    public const string StartRefund = "start_refund";

    /// <summary>
    /// TB-279: append an audited correction that returns an EFFECTIVE decision to review (a new decision version),
    /// before any financial, physical or source effect was recorded. It never erases history.
    /// </summary>
    public const string ReopenForReview = "reopen_for_review";

    /// <summary>
    /// TB-279: publish the reviewed per-item accepted/rejected split of the whole claim. The approved money of the case
    /// is derived from the accepted quantities only.
    /// </summary>
    public const string PublishResolution = "publish_resolution";

    /// <summary>
    /// TB-279: record the ACTUAL handback of rejected received goods to the owning customer. It is a physical fact,
    /// never a courier transition and never a money call.
    /// </summary>
    public const string RecordCustody = "record_custody";
}

/// <summary>
/// TB-275: the additive, transport-neutral description of one case's operator work. Every flag is a POSITIVE stored
/// fact: absent evidence leaves it <c>false</c> (fail closed), and a blocker code names exactly why the stage cannot
/// move instead of the read guessing a permission.
/// </summary>
public sealed record ReturnWorkDescription(
    ReturnWorkCategory Category,
    string? BlockerCode,
    Guid? PendingRefundAttemptId,
    Guid? FailedRefundAttemptId,
    bool CanRecordReceipt,
    bool CanDecide,
    bool CanResumeRefund,
    bool CanRetryRefund,
    bool CanSettlePhysical,
    // TB-277: an approved case that owns no refund attempt may deliberately start its FIRST refund. This is NOT a
    // recovery (nothing is pending) and NOT a retry (nothing failed).
    bool CanStartRefund = false,
    // TB-279: an audited correction may return the EFFECTIVE decision to review (append-only version), only while no
    // money/stock/handback effect was recorded.
    bool CanReopenForReview = false,
    // TB-279: the reviewed per-item accepted/rejected split may be published for this claim.
    bool CanPublishResolution = false,
    // TB-279: rejected received goods of this case still owe an actual handback to the owning customer.
    bool CanRecordCustody = false)
{
    /// <summary>True while the case still owns open work in the queue's active set.</summary>
    public bool IsOpen => Category is not (ReturnWorkCategory.Closed or ReturnWorkCategory.Settled);

    /// <summary>
    /// True for the stages that notify an operator that they owe a decision/action. A pending refund the system bound
    /// is deliberately NOT a notification (it is waiting on the provider, not on the operator); an approval whose
    /// first financial step was deliberately not taken yet IS one, because only an operator can start it.
    /// </summary>
    public bool NeedsOperator => CanStartRefund || CanRecordCustody || Category is ReturnWorkCategory.Receiving
        or ReturnWorkCategory.Decision
        or ReturnWorkCategory.RefundFailed
        or ReturnWorkCategory.Disposition;

    /// <summary>The stable wire code of the stage.</summary>
    public string CategoryCode => ReturnWorkCategoryCodes.Of(Category);

    /// <summary>The exact action codes this case offers, in workflow order; never an invented verb.</summary>
    public IReadOnlyList<string> ActionCodes
    {
        get
        {
            var codes = new List<string>(6);
            if (CanRecordReceipt) codes.Add(ReturnWorkActionCodes.RecordReceipt);
            if (CanPublishResolution) codes.Add(ReturnWorkActionCodes.PublishResolution);
            if (CanDecide)
            {
                codes.Add(ReturnWorkActionCodes.Approve);
                codes.Add(ReturnWorkActionCodes.Reject);
            }

            if (CanReopenForReview) codes.Add(ReturnWorkActionCodes.ReopenForReview);
            if (CanStartRefund) codes.Add(ReturnWorkActionCodes.StartRefund);
            if (CanResumeRefund) codes.Add(ReturnWorkActionCodes.ResumeRefund);
            if (CanRetryRefund) codes.Add(ReturnWorkActionCodes.RetryRefund);
            if (CanRecordCustody) codes.Add(ReturnWorkActionCodes.RecordCustody);
            if (CanSettlePhysical) codes.Add(ReturnWorkActionCodes.SettlePhysical);
            return codes;
        }
    }
}

/// <summary>
/// TB-275: the pure stage/action policy over stored facts. It deliberately DELEGATES the financial and physical
/// completion rules to <see cref="ReturnRecoveryPolicy"/> instead of re-deriving them: one case therefore has exactly
/// one recovery answer, and the queue, the detail workspace and the actions read that same answer.
/// </summary>
public static class ReturnWorkPolicy
{
    /// <summary>Describes the work stage of one stored case snapshot.</summary>
    public static ReturnWorkDescription Describe(ReturnSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var recovery = snapshot.Recovery ?? ReturnRecoveryPolicy.Describe(snapshot);
        var customerDelivered = snapshot.Origin != ReturnOrigin.CourierOrigin;
        var anyDisposition = snapshot.Items.Any(x => x.Disposition != ReturnItemDisposition.None);
        var receiptCoherent = snapshot.Receiving is not { IsCoherent: false };
        var receiptComplete = !customerDelivered || snapshot.Receiving is { IsComplete: true };

        // A further receipt occurrence is lawful only while the stored evidence is coherent, nothing was disposed yet
        // and the cumulative coverage is still incomplete — the same precondition the owning directory enforces
        // (TB-275 C1), so the workspace never offers a recording the directory would refuse.
        var canRecordReceipt = customerDelivered
            && snapshot.Status is ReturnRequestStatus.Requested
                or ReturnRequestStatus.Approved
                or ReturnRequestStatus.RefundProcessing
                or ReturnRequestStatus.RefundFailed
            && !anyDisposition
            && receiptCoherent
            && snapshot.Receiving is not { IsComplete: true };

        var canDecide = snapshot.Status == ReturnRequestStatus.Requested
            // TB-279: the EFFECTIVE decision is the authority. A corrected (reopened) case keeps its baseline row as
            // audit history but is back in review, so a RecordedDecision-only check would leave it undecidable forever.
            && snapshot.EffectiveDecision is null
            && receiptComplete
            && receiptCoherent
            && recovery.Phase != ReturnRecoveryPhase.Blocked;

        var canResumeRefund = recovery.Phase == ReturnRecoveryPhase.PendingMoney;
        var canRetryRefund = recovery.Phase == ReturnRecoveryPhase.FailedMoney;
        var canSettlePhysical = recovery.Phase == ReturnRecoveryPhase.PendingDisposition
            && recovery.RecoveryKind == ReturnRecoveryKind.SettlePhysicalDisposition;
        // TB-277: only the policy's own "lawfully approved, nothing started" phase authorizes the first money step,
        // so a read can never advertise an execution the command would refuse.
        var canStartRefund = recovery.Phase == ReturnRecoveryPhase.AwaitingFirstRefund;

        // TB-279: the correction availability comes from the ONE stored-fact assessment, never from a UI guess.
        var correction = snapshot.Correction ?? ReturnCorrectionPolicy.Describe(snapshot);
        var canReopenForReview = correction.CanReopenForReview;

        // TB-279: publishing the reviewed split is decision work offered exactly when the case's EFFECTIVE resolution
        // cannot name the accepted money (the legacy/partial claim). A fully received claim keeps the whole-accept
        // baseline, so the operator is never asked to re-type what the stored evidence already states.
        var effectiveResolution = ReturnResolutionPolicy.Resolve(snapshot);
        var canPublishResolution = customerDelivered
            && snapshot.Status == ReturnRequestStatus.Requested
            && snapshot.EffectiveDecision is null
            && receiptCoherent
            && snapshot.Receiving is { IsCoherent: true, Lines.Count: > 0 }
            && !effectiveResolution.IsComplete;

        // TB-279: an actual handback is owed only for rejected RECEIVED goods, and every accepted portion must already
        // own its durable physical fact before the remaining custody work is offered.
        var canRecordCustody = customerDelivered
            && snapshot.EffectiveDecision is not null
            && effectiveResolution.IsComplete
            && snapshot.Items.All(x => x.Disposition != ReturnItemDisposition.None
                || effectiveResolution.Lines.Any(line => line.ReturnItemId == x.ReturnItemId && line.AcceptedUsable <= 0m))
            && effectiveResolution.Lines.Any(line => ReturnResolutionPolicy.OwedCustody(line, snapshot.Custody) > 0m);

        var (category, blocker, pending, failed) = Classify(snapshot, recovery, receiptComplete);

        return new ReturnWorkDescription(
            category,
            blocker,
            pending,
            failed,
            canRecordReceipt,
            canDecide,
            canResumeRefund,
            canRetryRefund,
            canSettlePhysical,
            canStartRefund,
            canReopenForReview,
            canPublishResolution,
            canRecordCustody);
    }

    private static (ReturnWorkCategory Category, string? Blocker, Guid? Pending, Guid? Failed) Classify(
        ReturnSnapshot snapshot,
        ReturnRecoveryDescription recovery,
        bool receiptComplete) => recovery.Phase switch
        {
            ReturnRecoveryPhase.AwaitingFirstRefund => (
                // TB-277: an approved case whose money was deliberately not started is ACTIVE refund work, not a
                // closed/settled case and not an unknown outcome.
                ReturnWorkCategory.Refund, null, null, null),
            ReturnRecoveryPhase.PendingMoney => (
                ReturnWorkCategory.Refund, null, recovery.RefundAttemptId, null),
            ReturnRecoveryPhase.FailedMoney => (
                ReturnWorkCategory.RefundFailed, null, null, recovery.RefundAttemptId),
            ReturnRecoveryPhase.PendingDisposition => (
                ReturnWorkCategory.Disposition, null, null, null),
            ReturnRecoveryPhase.Settled => (
                ReturnWorkCategory.Settled, null, null, null),
            ReturnRecoveryPhase.Blocked => Blocked(snapshot, recovery, receiptComplete),
            _ => Unapproved(snapshot, receiptComplete),
        };

    /// <summary>An unapproved case: receiving first, then the decision, otherwise its decided-away terminal stage.</summary>
    private static (ReturnWorkCategory, string?, Guid?, Guid?) Unapproved(ReturnSnapshot snapshot, bool receiptComplete) =>
        snapshot.Status switch
        {
            ReturnRequestStatus.Requested => (
                receiptComplete ? ReturnWorkCategory.Decision : ReturnWorkCategory.Receiving, null, null, null),
            _ => (ReturnWorkCategory.Closed, null, null, null),
        };

    /// <summary>
    /// A typed blocker is reported on the stage the operator must look at: a missing receipt is receiving work, a
    /// missing owning stock binding is disposition work, a case decided away with received goods is closed, and any
    /// contradictory evidence is attached to the money stage when the money is not yet confirmed.
    /// </summary>
    private static (ReturnWorkCategory, string?, Guid?, Guid?) Blocked(
        ReturnSnapshot snapshot,
        ReturnRecoveryDescription recovery,
        bool receiptComplete)
    {
        var blocker = recovery.BlockerCode;

        if (string.Equals(blocker, ReturnsErrorCodes.RecoveryReceivingRequired, StringComparison.Ordinal))
        {
            return (ReturnWorkCategory.Receiving, blocker, null, null);
        }

        if (string.Equals(blocker, ReturnsErrorCodes.RecoveryStockBindingRequired, StringComparison.Ordinal))
        {
            return (ReturnWorkCategory.Disposition, blocker, null, null);
        }

        if (string.Equals(blocker, ReturnsErrorCodes.RecoveryPhysicalResolutionRequired, StringComparison.Ordinal))
        {
            return (ReturnWorkCategory.Closed, blocker, null, null);
        }

        if (recovery.FinancialComplete)
        {
            return (ReturnWorkCategory.Disposition, blocker, null, null);
        }

        return snapshot.Status switch
        {
            ReturnRequestStatus.Requested => (
                receiptComplete ? ReturnWorkCategory.Decision : ReturnWorkCategory.Receiving, blocker, null, null),
            ReturnRequestStatus.RefundFailed => (ReturnWorkCategory.RefundFailed, blocker, null, null),
            ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled => (
                ReturnWorkCategory.Closed, blocker, null, null),
            _ => (ReturnWorkCategory.Refund, blocker, null, null),
        };
    }
}
