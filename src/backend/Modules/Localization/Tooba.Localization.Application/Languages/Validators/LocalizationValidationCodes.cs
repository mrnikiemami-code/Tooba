namespace Tooba.Localization.Application.Languages.Validators;

/// <summary>
/// Stable machine-readable codes for Localization FluentValidation transport-shape failures.
/// These are transport identity codes only; they are never localized and never classify business state.
/// They are deliberately NOT registered as error-catalog descriptors: the canonical
/// <c>ValidationBehavior</c> pipeline maps them through the foundation <c>validation.failed</c>
/// descriptor, matching the certified AccessControl/Content/Cart/Offer precedent.
/// </summary>
public static class LocalizationValidationCodes
{
    /// <summary>Language code must be supplied in the request payload.</summary>
    public const string LanguageCodeRequired = "localization.validation.language_code_required";

    /// <summary>Language URL prefix must be supplied in the request payload.</summary>
    public const string LanguageUrlPrefixRequired = "localization.validation.language_url_prefix_required";

    /// <summary>Language display name must be supplied in the request payload.</summary>
    public const string LanguageDisplayNameRequired = "localization.validation.language_display_name_required";

    /// <summary>Language native name must be supplied in the request payload.</summary>
    public const string LanguageNativeNameRequired = "localization.validation.language_native_name_required";

    /// <summary>Language direction must be supplied in the request payload.</summary>
    public const string LanguageDirectionRequired = "localization.validation.language_direction_required";

    /// <summary>Language culture must be supplied in the request payload.</summary>
    public const string LanguageCultureRequired = "localization.validation.language_culture_required";

    /// <summary>Language calendar display policy must be supplied in the request payload.</summary>
    public const string LanguageCalendarRequired = "localization.validation.language_calendar_required";
}
