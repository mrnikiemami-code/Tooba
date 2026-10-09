using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.UserPreference.Contracts.Errors;

/// <summary>
/// UserPreference-owned error catalog. Registers one descriptor per UserPreference-emitted machine
/// code so the canonical <c>SafeErrorMapper</c> classifies them instead of falling back to a generic
/// 400. Classification is by stable machine code only.
/// <para>
/// <c>customer.session.required</c> is deliberately absent: it is declared and owned by Foundation
/// (<c>FoundationErrorCatalogContributor</c>), so UserPreference must not claim its descriptor. The
/// module still consumes the constant at its HTTP boundary.
/// </para>
/// </summary>
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
        D(UserPreferenceErrorCodes.OutboxUnmappedEventType, ErrorClassification.Platform, StatusCodes.Status500InternalServerError,
            "Integration event type is not registered."),
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
