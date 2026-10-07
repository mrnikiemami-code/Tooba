namespace Tooba.Payment.Contracts.Errors;

/// <summary>
/// Stable Payment semantic error codes for HTTP/use-case outcomes. Identity is the code itself —
/// never a message string and never localized prose. Values are the machine codes emitted by the
/// Payment Domain/Application/Infrastructure and mapped by the composed error catalog; they must
/// never be renamed or repurposed.
/// <para>
/// Ownership split (descriptor ownership is unique and must not be duplicated):
/// <list type="bullet">
/// <item>Payment-owned: registered by <c>PaymentErrorCatalogContributor</c> and localized by
/// <c>PaymentErrorResourceSet</c>.</item>
/// <item><see cref="ReservationRetryLimit"/>: declared and owned by Order
/// (<c>ReservationCycleErrors.RetryLimitReached</c>) — Payment only consumes the code.</item>
/// <item><see cref="AdminAuthorizationDenied"/> and <see cref="CheckoutAuthenticationRequired"/>:
/// cross-cutting codes owned by the Foundation contributor — Payment only consumes them.</item>
/// <item><see cref="SupplyUnavailable"/>: owned by Inventory (<c>InventoryErrorCodes.SupplyUnavailable</c>)
/// and reached through the <c>IOrderUnpaidRetrySupplyPort</c> contract seam; declared here so the
/// module's declared-code guard recognises it, but never re-registered by Payment.</item>
/// </list>
/// </para>
/// </summary>
public static class PaymentErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        AlreadySucceeded,
        Missing,
        AttemptMissing,
        AccessDenied,
        GuestInvalid,
        WalletMixedDeferred,
        MethodUnavailable,
        TrackingRequired,
        ProofRequired,
        ProofForeign,
        SandboxUnavailable,
        UnpaidSupplyUnavailable,
        UnpaidRetryInvalid,
        ReservationRetryLimit,
        Rejected,
        WebhookInvalidSignature,
        WebhookInvalidPayload,
        WebhookAmountMismatch,
        WebhookAttemptMismatch,
        WebhookProviderMismatch,
        GatewayUnconfigured,
        AdminPaymentMissing,
        MethodNotManual,
        ConfirmInvalidState,
        RejectInvalidState,
        CheckoutAuthenticationRequired,
        SupplyUnavailable,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>PaymentOperation</c> so Payment faults map to
    /// <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched to
    /// the canonical global exception boundary. Classification is by typed code only — never by message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>A payment for this checkout has already succeeded; new initiation is terminal.</summary>
    public const string AlreadySucceeded = "payment.already_succeeded";

    /// <summary>Payment record was not found.</summary>
    public const string Missing = "payment.missing";

    /// <summary>Payment attempt was not found.</summary>
    public const string AttemptMissing = "payment.attempt.missing";

    /// <summary>Actor is not allowed to see this payment.</summary>
    public const string AccessDenied = "payment.access.denied";

    /// <summary>Guest secret is invalid or the guest no longer owns the payment result.</summary>
    public const string GuestInvalid = "payment.guest.invalid";

    /// <summary>Mixed wallet+deferred tender is not offered yet.</summary>
    public const string WalletMixedDeferred = "payment.wallet.mixed_deferred";

    /// <summary>Requested payment method is not available.</summary>
    public const string MethodUnavailable = "payment.method.unavailable";

    /// <summary>Card-to-card transfer reference is required before manual confirmation.</summary>
    public const string TrackingRequired = "payment.tracking.required";

    /// <summary>Manual payment proof is required by policy.</summary>
    public const string ProofRequired = "payment.proof.required";

    /// <summary>Submitted proof asset belongs to another payment.</summary>
    public const string ProofForeign = "payment.proof.foreign";

    /// <summary>Sandbox simulator is not enabled.</summary>
    public const string SandboxUnavailable = "payment.sandbox.unavailable";

    /// <summary>Unpaid retry cannot be supplied by the order/inventory boundary.</summary>
    public const string UnpaidSupplyUnavailable = "payment.unpaid.supply_unavailable";

    /// <summary>Unpaid retry was requested from a state that does not allow it.</summary>
    public const string UnpaidRetryInvalid = "payment.unpaid.retry.invalid";

    /// <summary>Consumed from Order: reservation retry limit reached (Order-owned descriptor).</summary>
    public const string ReservationRetryLimit = "inventory.reservation.retry_limit_reached";

    /// <summary>Generic rejected payment operation.</summary>
    public const string Rejected = "payment.rejected";

    /// <summary>Webhook signature did not validate.</summary>
    public const string WebhookInvalidSignature = "payment.webhook.invalid_signature";

    /// <summary>Webhook payload was malformed or incomplete.</summary>
    public const string WebhookInvalidPayload = "payment.webhook.invalid_payload";

    /// <summary>Webhook amount did not match the payment.</summary>
    public const string WebhookAmountMismatch = "payment.webhook.amount_mismatch";

    /// <summary>Webhook attempt did not match the payment.</summary>
    public const string WebhookAttemptMismatch = "payment.webhook.attempt_mismatch";

    /// <summary>Webhook provider did not match the payment provider.</summary>
    public const string WebhookProviderMismatch = "payment.webhook.provider_mismatch";

    /// <summary>Online gateway is not configured; fail-closed.</summary>
    public const string GatewayUnconfigured = "payment.gateway.unconfigured";

    /// <summary>Admin payment detail was not found.</summary>
    public const string AdminPaymentMissing = "admin.payment.missing";

    /// <summary>Payment provider is not a manual card-to-card method.</summary>
    public const string MethodNotManual = "payment.method.not_manual";

    /// <summary>Manual deposit confirm requested from an invalid state.</summary>
    public const string ConfirmInvalidState = "payment.confirm.invalid_state";

    /// <summary>Manual deposit reject requested from an invalid state.</summary>
    public const string RejectInvalidState = "payment.reject.invalid_state";

    /// <summary>Consumed from Foundation: admin authorization denied (Foundation-owned descriptor).</summary>
    public const string AdminAuthorizationDenied = "admin.authorization.denied";

    /// <summary>Consumed from Foundation: checkout identity required (Foundation-owned descriptor).</summary>
    public const string CheckoutAuthenticationRequired = "checkout.authentication_required";

    /// <summary>Consumed from Inventory through the order supply port (Inventory-owned descriptor).</summary>
    public const string SupplyUnavailable = "inventory.supply.unavailable";
}
