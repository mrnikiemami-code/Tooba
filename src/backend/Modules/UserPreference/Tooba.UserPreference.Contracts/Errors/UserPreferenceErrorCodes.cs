namespace Tooba.UserPreference.Contracts.Errors;

/// <summary>Stable semantic error codes owned by UserPreference.</summary>
public static class UserPreferenceErrorCodes
{
    /// <summary>Locale preference write rejected by domain rules.</summary>
    public const string PreferenceRejected = "preference.rejected";

    /// <summary>UI preference write rejected by domain rules.</summary>
    public const string UiPreferenceRejected = "ui_preference.rejected";

    /// <summary>Stored UI preference JSON is invalid.</summary>
    public const string UiPreferenceInvalidJson = "ui_preference.invalid_json";

    /// <summary>UI preference JSON body is required.</summary>
    public const string UiPreferenceJsonRequired = "ui_preference.json_required";

    /// <summary>Shared foundation customer session required (do not re-register).</summary>
    public const string SessionRequired = "customer.session.required";
}
