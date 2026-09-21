namespace Tooba.Settlement.Application.Errors;

/// <summary>Stable semantic error codes owned by Settlement HTTP/use-case boundary.</summary>
public static class SettlementErrorCodes
{
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

    /// <summary>Unmapped outbox event.</summary>
    public const string OutboxUnmapped = "settlement.outbox.unmapped_event";
}
