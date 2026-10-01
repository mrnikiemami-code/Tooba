using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.UserPreference.Contracts.Errors;

namespace Tooba.UserPreference.Endpoints.Errors;

/// <summary>کاتالوگ کدهای خطای UserPreference.</summary>
public sealed class UserPreferenceErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(UserPreferenceErrorCodes.PreferenceRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Preference was rejected."),
        D(UserPreferenceErrorCodes.UiPreferenceRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "UI preference was rejected."),
        D(UserPreferenceErrorCodes.UiPreferenceInvalidJson, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "UI preference JSON is invalid."),
        D(UserPreferenceErrorCodes.UiPreferenceJsonRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "UI preference JSON is required."),
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
