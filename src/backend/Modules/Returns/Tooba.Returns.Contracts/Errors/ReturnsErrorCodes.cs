namespace Tooba.Returns.Contracts.Errors;

/// <summary>
/// Stable Returns semantic error codes for HTTP/use-case outcomes and Domain invariants. Identity is
/// the code itself — never a message string and never localized prose. Values are the machine codes
/// emitted by the Returns Domain/Infrastructure and mapped by the composed error catalog; they must
/// never be renamed or repurposed.
/// <para>
/// This is the single canonical home for Returns stable-code identity. Ownership is unique: the
/// <c>return.*</c> / <c>refund.*</c> keyspace is Returns-owned and localized by
/// <c>ReturnsErrorResourceSet</c>. Returns never re-registers a foreign-owned descriptor — the
/// cross-cutting <c>customer.session.required</c> code belongs to the Foundation and is deliberately
/// not declared here — and no other module registers a Returns code.
/// </para>
/// <para>
/// The surface is split by reachability so the AMSC gates stay honest:
/// <list type="bullet">
/// <item><see cref="IsHttpReachable"/> — codes an HTTP client can observe; each has exactly one
/// descriptor registered by <c>ReturnsErrorCatalogContributor</c>.</item>
/// <item><see cref="IsPlatformFault"/> — the platform-side outbox-translation fault, which is not a
/// client-visible outcome and therefore is deliberately never catalogued.</item>
/// </list>
/// <see cref="IsKnown"/> is the union and is what <c>ReturnsOperation</c> filters on, so a Returns
/// fault maps to <c>Result</c> while a code owned by another module (or an unexpected fault) propagates
/// untouched to the canonical global exception boundary. Classification is by typed code only — never by
/// message text.
/// </para>
/// </summary>
public static class ReturnsErrorCodes
{
    private static readonly HashSet<string> HttpReachableCodes = new(StringComparer.Ordinal)
    {
        Missing,
        Rejected,
        Expired,
        NonReturnable,
        QuantityExceeded,
        QuantityInvalid,
        NotDelivered,
        NotPaid,
        FulfillmentMissing,
        Stale,
        AlreadyApproved,
        AlreadyRejected,
        LineMissing,
        NotOwner,
        IdempotencyRequired,
        IdempotencyConflict,
        DuplicateLine,
        QuantityPrecision,
        RefundDestinationInvalid,
        RefundRetryInvalidState,
        RefundAlreadyStarted,
        RefundAlreadyCompleted,
        RefundPaymentMissing,
        RecoveryKindInvalid,
        RecoveryAttemptMismatch,
        RecoveryStateMismatch,
        RecoveryReceivingRequired,
        RecoveryAttemptUnbound,
        RecoveryGatewayUnavailable,
    };

    private static readonly HashSet<string> PlatformFaultCodes = new(StringComparer.Ordinal)
    {
        OutboxUnmappedEvent,
    };

    /// <summary>The exact set of Returns codes an HTTP client can observe, each singly catalogued.</summary>
    public static IReadOnlyCollection<string> HttpReachable => HttpReachableCodes;

    /// <summary>The exact set of Returns platform-side (non-client-visible) fault codes.</summary>
    public static IReadOnlyCollection<string> PlatformFaults => PlatformFaultCodes;

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>ReturnsOperation</c> so Returns faults map to <c>Result</c>
    /// while codes owned by another module (or an unexpected fault) propagate untouched to the canonical
    /// global exception boundary. Classification is by typed code only — never by message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && (HttpReachableCodes.Contains(code) || PlatformFaultCodes.Contains(code));

    /// <summary>True when <paramref name="code"/> is a Returns code registered as an HTTP descriptor.</summary>
    public static bool IsHttpReachable(string? code) =>
        !string.IsNullOrWhiteSpace(code) && HttpReachableCodes.Contains(code);

    /// <summary>True when <paramref name="code"/> is a Returns platform-side (non-client-visible) fault code.</summary>
    public static bool IsPlatformFault(string? code) =>
        !string.IsNullOrWhiteSpace(code) && PlatformFaultCodes.Contains(code);

    /// <summary>Return request missing.</summary>
    public const string Missing = "return.missing";

    /// <summary>Generic return rejection.</summary>
    public const string Rejected = "return.rejected";

    /// <summary>Return window expired.</summary>
    public const string Expired = "return.expired";

    /// <summary>Non-returnable.</summary>
    public const string NonReturnable = "return.non_returnable";

    /// <summary>Quantity exceeded.</summary>
    public const string QuantityExceeded = "return.quantity_exceeded";

    /// <summary>Quantity invalid.</summary>
    public const string QuantityInvalid = "return.quantity_invalid";

    /// <summary>Not delivered.</summary>
    public const string NotDelivered = "return.not_delivered";

    /// <summary>Not paid.</summary>
    public const string NotPaid = "return.not_paid";

    /// <summary>Fulfillment missing for return.</summary>
    public const string FulfillmentMissing = "return.fulfillment_missing";

    /// <summary>Stale transition.</summary>
    public const string Stale = "return.stale";

    /// <summary>Already approved.</summary>
    public const string AlreadyApproved = "return.already_approved";

    /// <summary>Already rejected.</summary>
    public const string AlreadyRejected = "return.already_rejected";

    /// <summary>Line missing.</summary>
    public const string LineMissing = "return.line_missing";

    /// <summary>Not owner.</summary>
    public const string NotOwner = "return.not_owner";

    /// <summary>Idempotency required.</summary>
    public const string IdempotencyRequired = "return.idempotency_required";

    /// <summary>
    /// TB-272: the submitted creation key is already stored for a DIFFERENT creation intent (another seller order,
    /// customer, line set/quantity, refund destination or reason). A typed conflict — never a borrowed snapshot.
    /// </summary>
    public const string IdempotencyConflict = "return.idempotency_conflict";

    /// <summary>
    /// TB-272: an ordinary creation named the same order line more than once. The duplicate is refused instead of
    /// being silently merged or refunded twice.
    /// </summary>
    public const string DuplicateLine = "return.duplicate_line";

    /// <summary>
    /// TB-272-R1: the submitted quantity is not exactly representable at the persisted six-decimal scale of
    /// <c>returns.return_items.quantity</c>. It is refused rather than rounded, so the stored number of physical
    /// units is always the number the caller submitted.
    /// </summary>
    public const string QuantityPrecision = "return.quantity_precision";

    /// <summary>Invalid refund destination.</summary>
    public const string RefundDestinationInvalid = "refund.destination.invalid";

    /// <summary>Retry invalid state.</summary>
    public const string RefundRetryInvalidState = "refund.retry.invalid_state";

    /// <summary>Refund already started.</summary>
    public const string RefundAlreadyStarted = "refund.already_started";

    /// <summary>Refund already completed.</summary>
    public const string RefundAlreadyCompleted = "refund.already_completed";

    /// <summary>Payment missing for refund.</summary>
    public const string RefundPaymentMissing = "refund.payment_missing";

    /// <summary>
    /// TB-273: the requested recovery kind token is absent or unknown. A recovery kind is never defaulted, because
    /// guessing one could repeat a payment or invent a physical action.
    /// </summary>
    public const string RecoveryKindInvalid = "return.recovery.kind_invalid";

    /// <summary>
    /// TB-273: the expected stored refund-attempt identity does not match the case's single bound attempt (stale,
    /// wrong, missing or ambiguous). Refused before any financial or physical effect.
    /// </summary>
    public const string RecoveryAttemptMismatch = "return.recovery.attempt_mismatch";

    /// <summary>
    /// TB-273: the requested recovery intent does not apply to the stored state (for example a money resume on a
    /// definite failure, or a physical settlement before the money is confirmed).
    /// </summary>
    public const string RecoveryStateMismatch = "return.recovery.state_mismatch";

    /// <summary>
    /// TB-273: a delivered-customer case has no recorded receipt/inspection fact, so its physical disposition cannot
    /// be executed from the default condition. The confirmed money is kept and the blocker is explicit.
    /// </summary>
    public const string RecoveryReceivingRequired = "return.recovery.receiving_required";

    /// <summary>
    /// TB-273: a money phase carries no single exact bound refund attempt, so no recovery can be named without
    /// guessing. Reported as a read blocker and refused as an action.
    /// </summary>
    public const string RecoveryAttemptUnbound = "return.recovery.attempt_unbound";

    /// <summary>
    /// TB-273: the owning refund adapter is explicitly not configured, so NO send was attempted. This is a
    /// truthful "cannot send now" answer with the pending attempt retained — never a successful refund, never an
    /// invented terminal provider rejection, and never a fresh attempt or key.
    /// </summary>
    public const string RecoveryGatewayUnavailable = "return.recovery.gateway_unavailable";

    /// <summary>Outbox registration has no mapping for the given integration event type (platform fault).</summary>
    public const string OutboxUnmappedEvent = "return.outbox.unmapped_event";
}

