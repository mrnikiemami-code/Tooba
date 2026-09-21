using Microsoft.AspNetCore.Http;

namespace Tooba.BuildingBlocks.Presentation.Errors;

/// <summary>تعاریف صریح خطاهای foundation عمومی.</summary>
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
    ];
}
