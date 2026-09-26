namespace Tooba.Identity.Contracts;

/// <summary>
/// Internal authentication outcome. Some values must never reach the public surface verbatim.
/// </summary>
public enum AuthenticationOutcome
{
    /// <summary>Authentication succeeded.</summary>
    Succeeded = 0,

    /// <summary>Wrong identifier/password, or user not found.</summary>
    InvalidCredentials = 1,

    /// <summary>Account is disabled.</summary>
    Disabled = 2,

    /// <summary>Account is locked.</summary>
    Locked = 3,

    /// <summary>Identifier-verification policy applies and the identifier is not verified.</summary>
    IdentifierNotVerified = 4,

    /// <summary>Session is revoked or expired.</summary>
    RevokedSession = 5,

    /// <summary>Previous refresh secret was reused after rotation.</summary>
    RefreshReuse = 6,
}

/// <summary>
/// Collapsed public authentication error to prevent account enumeration.
/// </summary>
public enum PublicAuthenticationError
{
    /// <summary>Generic login failure; account details are not disclosed.</summary>
    InvalidCredentials = 1,
}

/// <summary>
/// Session/token boundary. No custom JWT is minted here.
/// </summary>
public sealed record AuthenticationTicket
{
    /// <summary>Stable principal handed to the next authorization layer.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Internal session handle; cookie/access/refresh ride on this boundary later.</summary>
    public required Guid SessionHandle { get; init; }

    /// <summary>Raw refresh secret only at issue/rotate boundary. Not persisted and not a JWT.</summary>
    public string? RefreshToken { get; init; }

    /// <summary>Authentication time.</summary>
    public required DateTimeOffset AuthenticatedAt { get; init; }
}

/// <summary>
/// Authentication result separating internal reason from public projection.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>Whether a ticket was issued.</summary>
    public bool Succeeded => Ticket is not null;

    /// <summary>Ticket on success.</summary>
    public AuthenticationTicket? Ticket { get; init; }

    /// <summary>Internal reason for audit and tests.</summary>
    public AuthenticationOutcome Outcome { get; init; }

    /// <summary>Collapsed public projection.</summary>
    public PublicAuthenticationError? PublicError { get; init; }

    /// <summary>Success with ticket.</summary>
    public static AuthenticationResult Success(AuthenticationTicket ticket) => new()
    {
        Ticket = ticket,
        Outcome = AuthenticationOutcome.Succeeded,
    };

    /// <summary>Failure with internal reason; public surface usually collapses to InvalidCredentials.</summary>
    public static AuthenticationResult Fail(AuthenticationOutcome outcome, bool collapsePublicly = true) => new()
    {
        Outcome = outcome,
        PublicError = collapsePublicly
            ? PublicAuthenticationError.InvalidCredentials
            : outcome switch
            {
                AuthenticationOutcome.Disabled => PublicAuthenticationError.InvalidCredentials,
                AuthenticationOutcome.Locked => PublicAuthenticationError.InvalidCredentials,
                _ => PublicAuthenticationError.InvalidCredentials,
            },
    };
}

/// <summary>
/// Minimal user registration command.
/// </summary>
public sealed class RegisterUserCommand
{
    /// <summary>Primary identifier kind.</summary>
    public required LoginIdentifierKind IdentifierKind { get; init; }

    /// <summary>Raw identifier value.</summary>
    public required string Identifier { get; init; }

    /// <summary>Plaintext password only in request memory; never persisted.</summary>
    public required string Password { get; init; }
}

/// <summary>
/// Registration result without exposing the EF entity.
/// </summary>
public sealed class RegisterUserResult
{
    /// <summary>Stable user id.</summary>
    public required Guid UserId { get; init; }
}

/// <summary>
/// Authentication use cases. Never returns EF entities.
/// </summary>
public interface IIdentityAuthenticationService
{
    /// <summary>Creates the user and password credential inside the Identity module transaction.</summary>
    Task<RegisterUserResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken);

    /// <summary>Password login with identifier + password. Host does not parse it.</summary>
    Task<AuthenticationResult> AuthenticateWithPasswordAsync(LoginIdentifierKind kind, string identifier, string password, CancellationToken cancellationToken);

    /// <summary>Finds a user by normalized identifier in the same database schema.</summary>
    Task<Guid?> FindUserIdByIdentifierAsync(LoginIdentifierKind kind, string identifier, CancellationToken cancellationToken);

    /// <summary>Disables the account so login cannot succeed.</summary>
    Task DisableAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Locks the account so login cannot succeed.</summary>
    Task LockAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Changes the password after current-password proof, bumps the security stamp, and revokes sessions.</summary>
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>Rotates refresh with the raw secret. The previous secret becomes invalid.</summary>
    Task<AuthenticationResult> RefreshSessionAsync(Guid sessionId, string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes a single session.</summary>
    Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken);

    /// <summary>Revokes every session for the user.</summary>
    Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken);

    /// <summary>Issues a session for an active user after OTP proof. No password is required.</summary>
    Task<AuthenticationResult> EstablishSessionForUserAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Customer OTP login over the durable Identity challenge. The storefront does not mint a separate credential.
/// </summary>
public interface IIdentityOtpLoginService
{
    /// <summary>Creates a login challenge. Account existence is not disclosed publicly.</summary>
    Task<OtpChallengeHandle> RequestLoginAsync(string mobile, CancellationToken cancellationToken);

    /// <summary>Verifies the OTP, finds or creates the user, and issues the current session.</summary>
    Task<AuthenticationResult> CompleteLoginAsync(string mobile, Guid challengeId, string oneTimeCode, CancellationToken cancellationToken);
}

/// <summary>
/// Authenticated principal after session validation. Business authorization is not resolved here.
/// </summary>
public sealed record AuthenticatedIdentity(
    Guid UserId,
    Guid SessionId,
    string Edition,
    string? TenantId);

/// <summary>
/// Reads the session for the HTTP boundary. Host must not read EF directly.
/// </summary>
public interface IIdentitySessionResolver
{
    /// <summary>Maps a live session to a principal. Revoked/expired/stamp-mismatch/disabled resolves to null.</summary>
    Task<AuthenticatedIdentity?> ResolveAsync(Guid sessionId, CancellationToken cancellationToken);
}

/// <summary>
/// Challenge handle without exposing the secret.
/// </summary>
public sealed class OtpChallengeHandle
{
    /// <summary>Challenge id.</summary>
    public required Guid ChallengeId { get; init; }

    /// <summary>Challenge purpose.</summary>
    public required OtpPurpose Purpose { get; init; }
}

/// <summary>
/// Development fixed-OTP fixture, only applied when the Host environment is Development/Testing.
/// Never read from Production appsettings.
/// </summary>
public sealed class DevelopmentOtpLoginFixtureOptions
{
    /// <summary>Development/Testing only.</summary>
    public bool Enabled { get; set; }

    /// <summary>Fixed development mobile.</summary>
    public string Mobile { get; set; } = "09111111111";

    /// <summary>Fixed development code; not applied in Production.</summary>
    public string OneTimeCode { get; set; } = "123456";
}

/// <summary>
/// Internal challenge-consumption outcome. The public surface does not leak account enumeration.
/// </summary>
public enum ChallengeConsumeOutcome
{
    /// <summary>Secret was correct and the challenge was consumed.</summary>
    Succeeded = 0,

    /// <summary>Wrong secret, expired, or missing challenge.</summary>
    InvalidOrExpired = 1,

    /// <summary>Challenge was already consumed (single-use).</summary>
    Consumed = 2,

    /// <summary>Attempt limit exhausted and the challenge is locked.</summary>
    TooManyAttempts = 3,
}

/// <summary>
/// Password reset / identifier verification over the durable PostgreSQL challenge. Not a real email/SMS provider.
/// </summary>
public interface IIdentityCredentialLifecycle
{
    /// <summary>Reset request. Known and unknown identifiers share the same public response to avoid enumeration.</summary>
    Task<PasswordResetRequestResult> RequestPasswordResetAsync(LoginIdentifierKind kind, string identifier, CancellationToken cancellationToken);

    /// <summary>Completes the reset with a one-time secret and revokes sessions.</summary>
    Task<ChallengeConsumeOutcome> CompletePasswordResetAsync(Guid challengeId, string secret, string newPassword, CancellationToken cancellationToken);

    /// <summary>Creates an email/phone verification challenge. Issuing a code does not mark the identifier verified.</summary>
    Task<OtpChallengeHandle> IssueIdentifierVerificationAsync(Guid userId, LoginIdentifierKind kind, string identifier, CancellationToken cancellationToken);

    /// <summary>Consumes the verification code and marks the identifier verified on success.</summary>
    Task<ChallengeConsumeOutcome> CompleteIdentifierVerificationAsync(Guid challengeId, string secret, CancellationToken cancellationToken);
}

/// <summary>
/// Public password-reset request response; always shown as accepted.
/// </summary>
public sealed class PasswordResetRequestResult
{
    /// <summary>Public acceptance without disclosing account existence.</summary>
    public bool Accepted { get; init; } = true;

    /// <summary>Challenge id only when an account was found; for internal tests, not the public surface.</summary>
    public Guid? ChallengeId { get; init; }
}

/// <summary>Read-only projection of Identity contact identifiers for the profile UI.</summary>
public sealed record IdentityContactSnapshot(string? Email, string? Mobile);

/// <summary>
/// Narrow lookup for displaying email/mobile in the profile. Mutation happens through the OTP/Auth path.
/// </summary>
public interface IIdentityContactLookup
{
    /// <summary>Returns the user's verified display email and mobile.</summary>
    Task<IdentityContactSnapshot> GetContactAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Returns contact projections for several users in one bounded query (no N+1).</summary>
    Task<IReadOnlyDictionary<Guid, IdentityContactSnapshot>> GetContactsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}
