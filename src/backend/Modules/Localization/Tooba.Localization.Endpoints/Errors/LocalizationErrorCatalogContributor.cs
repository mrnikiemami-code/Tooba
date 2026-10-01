using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Localization.Contracts.Errors;

namespace Tooba.Localization.Endpoints.Errors;

/// <summary>کاتالوگ کدهای خطای Localization language admin.</summary>
public sealed class LocalizationErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(LanguageErrorCodes.NotFound, StatusCodes.Status404NotFound, "Language was not found."),
        D(LanguageErrorCodes.CodeDuplicate, StatusCodes.Status400BadRequest, "Language code already exists."),
        D(LanguageErrorCodes.UrlPrefixDuplicate, StatusCodes.Status400BadRequest, "Language URL prefix already exists."),
        D(LanguageErrorCodes.DefaultMustBeActive, StatusCodes.Status400BadRequest, "Default language must be active."),
        D(LanguageErrorCodes.AtLeastOneActive, StatusCodes.Status400BadRequest, "At least one language must remain active."),
        D(LanguageErrorCodes.ExactlyOneDefault, StatusCodes.Status400BadRequest, "Exactly one default language is required."),
        D(LanguageErrorCodes.CodeImmutable, StatusCodes.Status400BadRequest, "Language code is immutable."),
        D(LanguageErrorCodes.UrlPrefixImmutable, StatusCodes.Status400BadRequest, "Language URL prefix is immutable."),
        D(LanguageErrorCodes.CodeInUse, StatusCodes.Status400BadRequest, "Language code is in use."),
        D(LanguageErrorCodes.UrlPrefixInUse, StatusCodes.Status400BadRequest, "Language URL prefix is in use."),
        D(LanguageErrorCodes.Referenced, StatusCodes.Status400BadRequest, "Language is referenced."),
        D(LanguageErrorCodes.InvalidCode, StatusCodes.Status400BadRequest, "Language code is invalid."),
        D(LanguageErrorCodes.InvalidUrlPrefix, StatusCodes.Status400BadRequest, "Language URL prefix is invalid."),
        D(LanguageErrorCodes.InvalidDisplayName, StatusCodes.Status400BadRequest, "Language display name is invalid."),
        D(LanguageErrorCodes.InvalidNativeName, StatusCodes.Status400BadRequest, "Language native name is invalid."),
        D(LanguageErrorCodes.InvalidCulture, StatusCodes.Status400BadRequest, "Language culture is invalid."),
        D(LanguageErrorCodes.InvalidDirection, StatusCodes.Status400BadRequest, "Language direction is invalid."),
        D(LanguageErrorCodes.InvalidCalendar, StatusCodes.Status400BadRequest, "Language calendar is invalid."),
        D(LanguageErrorCodes.Inactive, StatusCodes.Status400BadRequest, "Language is inactive."),
    ];

    private static ErrorDescriptor D(string code, int status, string fallback) =>
        new(
            Code: code,
            Classification: ErrorClassification.Validation,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
