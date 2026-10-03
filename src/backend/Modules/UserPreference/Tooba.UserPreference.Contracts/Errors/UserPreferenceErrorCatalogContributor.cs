using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.UserPreference.Contracts.Errors;

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
        D(UserPreferenceErrorCodes.ActorRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Actor is required."),
        D(UserPreferenceErrorCodes.LocaleRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Locale is required."),
        D(UserPreferenceErrorCodes.UiActorRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Actor is required."),
        D(UserPreferenceErrorCodes.UiKeyRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "UI preference key is required."),
        D(UserPreferenceErrorCodes.UiJsonRequiredValidation, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "UI preference JSON is required."),
        // customer.session.required is owned by FoundationErrorCatalogContributor.
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
