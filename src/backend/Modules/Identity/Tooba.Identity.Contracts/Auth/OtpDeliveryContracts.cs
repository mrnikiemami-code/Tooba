namespace Tooba.Identity.Contracts;

/// <summary>
/// Outcome of an OTP delivery attempt without exposing the secret or vendor details.
/// </summary>
public enum OtpDeliveryOutcomeKind
{
    /// <summary>Delivery succeeded.</summary>
    Succeeded = 1,

    /// <summary>Provider is temporarily unavailable.</summary>
    Unavailable = 2,

    /// <summary>Provider rate-limited the delivery.</summary>
    RateLimited = 3,

    /// <summary>Destination is invalid.</summary>
    InvalidDestination = 4,

    /// <summary>Production configuration is incomplete.</summary>
    Misconfigured = 5,
}

/// <summary>
/// Provider-agnostic OTP delivery message.
/// </summary>
public sealed record OtpDeliveryMessage(OtpPurpose Purpose, string Destination, string OneTimeCode);

/// <summary>
/// Provider outcome with an optional correlation id.
/// </summary>
public sealed record OtpDeliveryOutcome(OtpDeliveryOutcomeKind Kind, string? CorrelationId = null);

/// <summary>
/// Production OTP delivery boundary; the domain is not bound to a specific vendor.
/// </summary>
public interface IOtpDeliveryProvider
{
    /// <summary>Sends the OTP to the destination. The OTP code must never be logged.</summary>
    Task<OtpDeliveryOutcome> DeliverAsync(OtpDeliveryMessage message, CancellationToken cancellationToken);
}
