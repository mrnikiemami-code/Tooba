namespace Tooba.Settlement.Contracts.Errors;

/// <summary>
/// Stable Settlement semantic error codes for HTTP/use-case outcomes and Domain invariants. Identity is
/// the code itself — never a message string and never localized prose. Values are the machine codes
/// emitted by the Settlement Domain/Infrastructure and mapped by the composed error catalog; they must
/// never be renamed or repurposed.
/// <para>
/// This is the single canonical home for Settlement stable-code identity. Ownership is unique: the
/// <c>settlement.*</c> / <c>payout.*</c> keyspace is Settlement-owned and localized by
/// <c>SettlementErrorResourceSet</c>. Settlement never re-registers a foreign-owned descriptor — the
/// cross-cutting <c>customer.session.required</c> / <c>seller.authorization.denied</c> /
/// <c>admin.authorization.denied</c> codes belong to the Foundation and are deliberately not declared
/// here — and no other module registers a Settlement code.
/// </para>
/// <para>
/// The surface is split by reachability so the AMSC gates stay honest:
/// <list type="bullet">
/// <item><see cref="IsHttpReachable"/> — codes an HTTP client can observe; each has exactly one
/// descriptor registered by <c>SettlementErrorCatalogContributor</c>.</item>
/// <item><see cref="IsPlatformFault"/> — the platform-side outbox-translation fault, which is not a
/// client-visible outcome and therefore is deliberately never catalogued.</item>
/// </list>
/// <see cref="IsKnown"/> is the union and is what <c>SettlementOperation</c> filters on, so a Settlement
/// fault maps to <c>Result</c> while a code owned by another module (or an unexpected fault) propagates
/// untouched to the canonical global exception boundary. Classification is by typed code only — never by
/// message text.
/// </para>
/// </summary>
public static class SettlementErrorCodes
{
    private static readonly HashSet<string> HttpReachableCodes = new(StringComparer.Ordinal)
    {
        AccountMissing,
        PayoutRejected,
        PayoutInvalidAmount,
        PayoutMissing,
        PayoutInvalidState,
        IdempotencyRequired,
        AmountInvalid,
        AccrualPaymentMissing,
        AccrualPaymentNotSucceeded,
        AccrualOrderMissing,
        AccrualOrderNotPaid,
        RefundMissing,
        RefundMismatch,
        GatewayUnconfigured,
        UnconfirmPayoutCompleted,
        CancelPayoutCompleted,
        RestorePayoutCompleted,
    };

    private static readonly HashSet<string> PlatformFaultCodes = new(StringComparer.Ordinal)
    {
        OutboxUnmapped,
    };

    /// <summary>The exact set of Settlement codes an HTTP client can observe, each singly catalogued.</summary>
    public static IReadOnlyCollection<string> HttpReachable => HttpReachableCodes;

    /// <summary>The exact set of Settlement platform-side (non-client-visible) fault codes.</summary>
    public static IReadOnlyCollection<string> PlatformFaults => PlatformFaultCodes;

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>SettlementOperation</c> so Settlement faults map to
    /// <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched to
    /// the canonical global exception boundary. Classification is by typed code only — never by message
    /// text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && (HttpReachableCodes.Contains(code) || PlatformFaultCodes.Contains(code));

    /// <summary>True when <paramref name="code"/> is a Settlement code registered as an HTTP descriptor.</summary>
    public static bool IsHttpReachable(string? code) =>
        !string.IsNullOrWhiteSpace(code) && HttpReachableCodes.Contains(code);

    /// <summary>True when <paramref name="code"/> is a Settlement platform-side (non-client-visible) fault code.</summary>
    public static bool IsPlatformFault(string? code) =>
        !string.IsNullOrWhiteSpace(code) && PlatformFaultCodes.Contains(code);

    /// <summary>Settlement account missing for seller.</summary>
    public const string AccountMissing = "settlement.account.missing";

    /// <summary>Payout rejected by business rules.</summary>
    public const string PayoutRejected = "settlement.payout.rejected";

    /// <summary>Payout amount invalid or exceeds available balance.</summary>
    public const string PayoutInvalidAmount = "settlement.payout.invalid_amount";

    /// <summary>Payout request missing.</summary>
    public const string PayoutMissing = "settlement.payout.missing";

    /// <summary>Payout already succeeded / invalid state for mutation.</summary>
    public const string PayoutInvalidState = "settlement.payout.invalid_state";

    /// <summary>Idempotency key required or conflict.</summary>
    public const string IdempotencyRequired = "settlement.idempotency.required";

    /// <summary>Domain amount must be positive.</summary>
    public const string AmountInvalid = "settlement.amount.invalid";

    /// <summary>Accrual payment missing.</summary>
    public const string AccrualPaymentMissing = "settlement.accrual.payment_missing";

    /// <summary>Accrual payment not succeeded.</summary>
    public const string AccrualPaymentNotSucceeded = "settlement.accrual.payment_not_succeeded";

    /// <summary>Accrual order missing.</summary>
    public const string AccrualOrderMissing = "settlement.accrual.order_missing";

    /// <summary>Accrual order not paid.</summary>
    public const string AccrualOrderNotPaid = "settlement.accrual.order_not_paid";

    /// <summary>Refund snapshot missing.</summary>
    public const string RefundMissing = "settlement.refund.missing";

    /// <summary>Refund snapshot mismatch.</summary>
    public const string RefundMismatch = "settlement.refund.mismatch";

    /// <summary>Payout gateway unconfigured.</summary>
    public const string GatewayUnconfigured = "payout.gateway.unconfigured";

    /// <summary>Cannot unconfirm accrual after completed payout.</summary>
    public const string UnconfirmPayoutCompleted = "settlement.unconfirm.payout_completed";

    /// <summary>Cannot cancel accrual after completed payout.</summary>
    public const string CancelPayoutCompleted = "settlement.cancel.payout_completed";

    /// <summary>Cannot restore after completed payout.</summary>
    public const string RestorePayoutCompleted = "settlement.restore.payout_completed";

    /// <summary>Outbox registration has no mapping for the given integration event type (platform fault).</summary>
    public const string OutboxUnmapped = "settlement.outbox.unmapped_event";
}
