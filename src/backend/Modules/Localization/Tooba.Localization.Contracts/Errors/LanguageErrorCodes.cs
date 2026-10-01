namespace Tooba.Localization.Contracts.Errors;

/// <summary>کدهای خطای پایدار زبان (Contracts).</summary>
public static class LanguageErrorCodes
{
    public const string NotFound = "localization.language.not_found";
    public const string CodeDuplicate = "localization.language.code_duplicate";
    public const string UrlPrefixDuplicate = "localization.language.url_prefix_duplicate";
    public const string DefaultMustBeActive = "localization.language.default_must_be_active";
    public const string AtLeastOneActive = "localization.language.at_least_one_active";
    public const string ExactlyOneDefault = "localization.language.exactly_one_default";
    public const string CodeImmutable = "localization.language.code_immutable";
    public const string UrlPrefixImmutable = "localization.language.url_prefix_immutable";
    public const string CodeInUse = "localization.language.code.in_use";
    public const string UrlPrefixInUse = "localization.language.url_prefix.in_use";
    public const string Referenced = "localization.language.referenced";
    public const string InvalidCode = "localization.language.invalid_code";
    public const string InvalidUrlPrefix = "localization.language.invalid_url_prefix";
    public const string InvalidDisplayName = "localization.language.invalid_display_name";
    public const string InvalidNativeName = "localization.language.invalid_native_name";
    public const string InvalidCulture = "localization.language.invalid_culture";
    public const string InvalidDirection = "localization.language.invalid_direction";
    public const string InvalidCalendar = "localization.language.invalid_calendar";
    public const string Inactive = "localization.language.inactive";
}
