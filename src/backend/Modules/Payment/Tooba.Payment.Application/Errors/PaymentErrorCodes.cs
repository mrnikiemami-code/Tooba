namespace Tooba.Payment.Application.Errors;

/// <summary>Stable Payment semantic error codes for HTTP/use-case outcomes.</summary>
public static class PaymentErrorCodes
{
    public const string Missing = "payment.missing";
    public const string Rejected = "payment.rejected";
    public const string AccessDenied = "payment.access.denied";
    public const string GuestInvalid = "payment.guest.invalid";
    public const string AlreadySucceeded = "payment.already_succeeded";
    public const string WalletMixedDeferred = "payment.wallet.mixed_deferred";
    public const string MethodUnavailable = "payment.method.unavailable";
    public const string TrackingRequired = "payment.tracking.required";
    public const string ProofRequired = "payment.proof.required";
    public const string ProofForeign = "payment.proof.foreign";
    public const string SandboxUnavailable = "payment.sandbox.unavailable";
    public const string UnpaidSupplyUnavailable = "payment.unpaid.supply_unavailable";
    public const string UnpaidRetryInvalid = "payment.unpaid.retry.invalid";
    public const string WebhookUnauthorized = "payment.webhook.unauthorized";
    public const string WebhookInvalidPayload = "payment.webhook.invalid_payload";
    public const string WebhookAmountMismatch = "payment.webhook.amount_mismatch";
    public const string WebhookAttemptMismatch = "payment.webhook.attempt_mismatch";
    public const string WebhookProviderMismatch = "payment.webhook.provider_mismatch";
    public const string WebhookRejected = "payment.webhook.rejected";
    public const string AdminAuthorizationDenied = "admin.authorization.denied";
    public const string CustomerSessionRequired = "customer.session.required";
}
