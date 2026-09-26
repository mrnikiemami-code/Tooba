namespace Tooba.Identity.Contracts.Problems;

/// <summary>
/// Stable machine error codes owned by Identity and consumed by the global Host auth boundary.
/// The codes are API contract, not localization keys, and must never change silently.
/// </summary>
public static class IdentityErrorCodes
{
    /// <summary>Transport/credential input shape failed validation. HTTP 400.</summary>
    public const string ValidationFailed = "identity.validation.failed";

    /// <summary>Durable challenge is invalid, expired, or already consumed. HTTP 400.</summary>
    public const string ChallengeInvalid = "identity.challenge.invalid";

    /// <summary>Credentials/account state collapsed to a single public login failure. HTTP 401.</summary>
    public const string AuthenticationFailed = "identity.authentication.failed";

    /// <summary>Session/refresh handle is unknown, expired, or revoked. HTTP 401.</summary>
    public const string SessionInvalid = "identity.session.invalid";

    /// <summary>Normalized identifier already exists in this identity schema. HTTP 409.</summary>
    public const string IdentifierConflict = "identity.identifier.conflict";

    /// <summary>Operation throttled by the Host rate-limit seam. HTTP 429.</summary>
    public const string RateLimited = "identity.rate_limited";

    /// <summary>Client-presented tenant authority is rejected. HTTP 400.</summary>
    public const string TenantUntrusted = "identity.tenant.untrusted";

    /// <summary>OTP delivery provider is unavailable or fails closed. HTTP 400.</summary>
    public const string OtpDeliveryUnavailable = "identity.otp.delivery.unavailable";

    /// <summary>Password change was rejected by policy/current-password proof. HTTP 400.</summary>
    public const string PasswordChangeFailed = "identity.password.change.failed";
}
