namespace Tooba.Identity.Contracts;

/// <summary>
/// Neutral login identifier kind used by the global authentication boundary.
/// Owned by Identity; Host never sees Identity credentials or persistence internals.
/// </summary>
public enum LoginIdentifierKind
{
    /// <summary>User-chosen username inside the identity schema.</summary>
    Username = 1,

    /// <summary>Email address after email-specific normalization.</summary>
    Email = 2,

    /// <summary>Phone number after digit-based normalization without an Iran assumption.</summary>
    Phone = 3,

    /// <summary>Reserved national-id kind; not authenticated in this task.</summary>
    NationalId = 4,

    /// <summary>Stable external provider identifier (issuer+subject stored separately).</summary>
    ExternalProvider = 5,
}

/// <summary>
/// Purpose of an OTP challenge so login/verify/reset/MFA stay separate.
/// </summary>
public enum OtpPurpose
{
    /// <summary>Passwordless login or login completion.</summary>
    Login = 1,

    /// <summary>Identifier ownership proof.</summary>
    IdentifierVerification = 2,

    /// <summary>Password recovery.</summary>
    PasswordReset = 3,

    /// <summary>Second MFA factor.</summary>
    Mfa = 4,
}
