using Microsoft.AspNetCore.Http;

namespace Tooba.BuildingBlocks.Presentation.Errors;

/// <summary>تعاریف صریح خطاهای foundation عمومی و کدهای عرضی مشترک.</summary>
public sealed class FoundationErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        new(
            Code: "validation.failed",
            Classification: ErrorClassification.Validation,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: "validation.failed",
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Validation failed."),
        new(
            Code: "platform.unexpected",
            Classification: ErrorClassification.Unexpected,
            HttpStatus: StatusCodes.Status500InternalServerError,
            LocalizationKey: "platform.unexpected",
            Severity: ErrorSeverity.Error,
            SafeTitleFallback: "An unexpected error occurred."),
        new(
            Code: "platform.error",
            Classification: ErrorClassification.Platform,
            HttpStatus: StatusCodes.Status500InternalServerError,
            LocalizationKey: "platform.error",
            Severity: ErrorSeverity.Error,
            SafeTitleFallback: "Request failed."),

        // Cross-cutting customer-session / role-authorization codes. These are consumed by many
        // module endpoints but own exactly one canonical descriptor here (shared, neutral owner).
        D(FoundationErrorCodes.CustomerSessionRequired, ErrorClassification.Forbidden,
            StatusCodes.Status401Unauthorized, "Unauthorized"),
        D(FoundationErrorCodes.CheckoutAuthenticationRequired, ErrorClassification.Forbidden,
            StatusCodes.Status401Unauthorized, "Sign in to continue."),
        D(FoundationErrorCodes.SellerAuthorizationDenied, ErrorClassification.Forbidden,
            StatusCodes.Status403Forbidden, "Forbidden"),
        D(FoundationErrorCodes.AdminAuthorizationDenied, ErrorClassification.Forbidden,
            StatusCodes.Status403Forbidden, "Forbidden"),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(
            Code: code,
            Classification: classification,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
