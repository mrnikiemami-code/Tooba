namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>
/// FluentValidation machine codes for Identity transport shape.
/// Catalog presentation still collapses through the shared validation mapper;
/// these codes stay stable for guard/inventory ownership.
/// </summary>
public static class IdentityValidationCodes
{
    /// <summary>Identifier kind must be a defined LoginIdentifierKind.</summary>
    public const string IdentifierKindRequired = "identity.validation.identifier_kind_required";

    /// <summary>Identifier value must be supplied.</summary>
    public const string IdentifierRequired = "identity.validation.identifier_required";

    /// <summary>Password must be supplied.</summary>
    public const string PasswordRequired = "identity.validation.password_required";

    /// <summary>Refresh token must be supplied.</summary>
    public const string RefreshTokenRequired = "identity.validation.refresh_token_required";

    /// <summary>Session id must be supplied.</summary>
    public const string SessionIdRequired = "identity.validation.session_id_required";

    /// <summary>Challenge id must be supplied.</summary>
    public const string ChallengeIdRequired = "identity.validation.challenge_id_required";

    /// <summary>One-time secret must be supplied.</summary>
    public const string SecretRequired = "identity.validation.secret_required";

    /// <summary>New password must be supplied.</summary>
    public const string NewPasswordRequired = "identity.validation.new_password_required";

    /// <summary>Current password must be supplied.</summary>
    public const string CurrentPasswordRequired = "identity.validation.current_password_required";
}
