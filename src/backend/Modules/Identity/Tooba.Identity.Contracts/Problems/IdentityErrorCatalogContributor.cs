using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Identity.Contracts.Problems;

/// <summary>
/// Explicit Identity error catalog. Descriptors pin the exact HTTP semantics of the global
/// authentication boundary so canonical SafeErrorMapper output never drifts from locked behavior.
/// </summary>
public sealed class IdentityErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(IdentityErrorCodes.ValidationFailed, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Validation failed."),
        D(IdentityErrorCodes.ChallengeInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Challenge is invalid."),
        D(IdentityErrorCodes.AuthenticationFailed, ErrorClassification.Platform, StatusCodes.Status401Unauthorized,
            "Authentication failed."),
        D(IdentityErrorCodes.SessionInvalid, ErrorClassification.Platform, StatusCodes.Status401Unauthorized,
            "Session is invalid."),
        D(IdentityErrorCodes.IdentifierConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Identifier already exists."),
        D(IdentityErrorCodes.RateLimited, ErrorClassification.Platform, StatusCodes.Status429TooManyRequests,
            "Too Many Requests."),
        D(IdentityErrorCodes.TenantUntrusted, ErrorClassification.Platform, StatusCodes.Status400BadRequest,
            "Tenant input is not trusted."),
        D(IdentityErrorCodes.OtpDeliveryUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "OTP delivery is unavailable."),
        D(IdentityErrorCodes.PasswordChangeFailed, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Password change was rejected."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
