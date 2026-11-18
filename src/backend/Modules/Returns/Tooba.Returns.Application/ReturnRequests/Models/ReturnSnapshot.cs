using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;


/// <summary>
/// snapshot خواندنی درخواست مرجوعی.
/// </summary>
public sealed record ReturnSnapshot(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    Guid RequestedByUserId,
    ReturnRequestStatus Status,
    string? Reason,
    string Currency,
    decimal RefundAmount,
    Guid? PaymentId,
    RefundDestination RefundDestination,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ReturnItemSnapshot> Items,
    IReadOnlyList<RefundAttemptSnapshot> RefundAttempts,
    ReturnOrigin Origin = ReturnOrigin.CustomerDelivered,
    Guid? SourceFulfillmentId = null,
    Guid? SourceShipmentId = null,
    Guid? SourceReceiptId = null,
    Guid? SourceClaimId = null,
    string? SourceOccurrenceKey = null,
    CourierOriginClosurePhase ClosurePhase = CourierOriginClosurePhase.None,
    DateTimeOffset? ObligationClosedAt = null,
    DateTimeOffset? StockSettledAt = null,
    decimal ReleasedHeldQuantity = 0,
    ReturnRecoveryDescription? Recovery = null,
    // TB-273-R1: the durable approve/reject decision recorded for this case ("Approve"/"Reject") or null when no
    // decision row exists. The recovery policy treats a missing row as a blocker instead of inventing an approval;
    // no other decision metadata is exposed.
    string? RecordedDecision = null,
    // TB-274: the receiving/inspection summary of a delivered-customer case, computed only from recorded receipts.
    // Null when no receipt was ever recorded. Approval, money and restock are gated on this exact fact.
    ReturnReceivingSummary? Receiving = null,
    // TB-274: the reason recorded with the winning rejection decision (null for an approval or a pre-274 rejection).
    string? DecisionReason = null,
    // TB-275: every recorded customer receipt occurrence, oldest first. A courier-origin case's receipt belongs to
    // Fulfillment, so this stays empty for it (never an invented occurrence).
    IReadOnlyList<ReturnReceiptOccurrenceSnapshot>? Receipts = null,
    // TB-275: the ONE work stage/action description shared by the queue and the detail workspace.
    ReturnWorkDescription? Work = null,
    // TB-279: the EFFECTIVE decision after the append-only revision history ("Approve"/"Reject"/null when a correction
    // returned the case to review). <see cref="RecordedDecision"/> stays the immutable legacy baseline row.
    string? EffectiveDecision = null,
    // TB-279: the monotonic reviewed version of the effective decision (0 = only the legacy baseline exists).
    int DecisionVersion = 0,
    // TB-279: every appended decision/correction fact, oldest first (the legacy baseline row is not repeated here).
    IReadOnlyList<ReturnDecisionRevisionSnapshot>? DecisionHistory = null,
    // TB-279: the reviewed per-item resolution plan and the money derived from its accepted quantities only.
    ReturnResolutionSnapshot? Resolution = null,
    // TB-279: the frozen approved money (null until approved/frozen; a case that already started money keeps RefundAmount).
    decimal? ApprovedAmount = null,
    // TB-279: the recorded ACTUAL handbacks of rejected received goods to the owning customer.
    IReadOnlyList<ReturnCustodySnapshot>? Custody = null,
    // TB-279: whether an audited correction is available and the exact typed reason when it is not.
    ReturnCorrectionAssessment? Correction = null);
