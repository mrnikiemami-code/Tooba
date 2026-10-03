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

    /// <summary>Actor id is required on preference write/read envelope.</summary>
    public const string ActorRequired = "preference.validation.actor_required";

    /// <summary>Locale is required.</summary>
    public const string LocaleRequired = "preference.validation.locale_required";

    /// <summary>Actor id is required on UI preference envelope.</summary>
    public const string UiActorRequired = "ui_preference.validation.actor_required";

    /// <summary>UI preference key is required.</summary>
    public const string UiKeyRequired = "ui_preference.validation.key_required";

    /// <summary>UI preference JSON payload is required (validation).</summary>
    public const string UiJsonRequiredValidation = "ui_preference.validation.json_required";
}
