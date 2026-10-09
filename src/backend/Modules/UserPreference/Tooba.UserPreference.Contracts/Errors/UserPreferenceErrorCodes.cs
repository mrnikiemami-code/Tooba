namespace Tooba.UserPreference.Contracts.Errors;

/// <summary>
/// Stable UserPreference semantic error codes. Identity is the code itself — never a message string
/// and never localized prose. Values are the machine codes emitted by the UserPreference
/// Domain/Application/Infrastructure and mapped by the composed error catalog; they must never be
/// renamed or repurposed.
/// <para>
/// This is the single canonical home for UserPreference stable-code identity. Ownership is unique:
/// every code in <see cref="KnownCodes"/> is UserPreference-owned, registered exactly once by
/// <c>UserPreferenceErrorCatalogContributor</c> and localized by <c>UserPreferenceErrorResourceSet</c>.
/// The module never re-registers a foreign-owned descriptor and no other module registers a
/// UserPreference code.
/// </para>
/// <para>
/// <c>customer.session.required</c> (<see cref="SessionRequired"/>) is a shared Foundation-owned
/// cross-cutting code: its descriptor and both-culture resources belong to
/// <c>FoundationErrorCatalogContributor</c>. The constant stays here so the module's HTTP boundary can
/// consume it, but it is deliberately excluded from <see cref="KnownCodes"/> because it is not a
/// UserPreference use-case fault.
/// </para>
/// </summary>
public static class UserPreferenceErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        PreferenceRejected,
        UiPreferenceRejected,
        UiPreferenceInvalidJson,
        UiPreferenceJsonRequired,
        ActorRequired,
        LocaleRequired,
        UiActorRequired,
        UiKeyRequired,
        UiJsonRequiredValidation,
        OutboxUnmappedEventType,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>UserPreferenceOperation</c> so UserPreference faults map
    /// to <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched
    /// to the canonical global exception boundary. Classification is by typed code only — never by
    /// message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>Locale preference write rejected by domain rules.</summary>
    public const string PreferenceRejected = "preference.rejected";

    /// <summary>UI preference write rejected by domain rules.</summary>
    public const string UiPreferenceRejected = "ui_preference.rejected";

    /// <summary>Stored UI preference JSON is invalid.</summary>
    public const string UiPreferenceInvalidJson = "ui_preference.invalid_json";

    /// <summary>UI preference JSON body is required.</summary>
    public const string UiPreferenceJsonRequired = "ui_preference.json_required";

    /// <summary>
    /// Shared Foundation-owned customer session code (descriptor and resources are Foundation-owned;
    /// consume it, never re-register it). Deliberately excluded from <see cref="KnownCodes"/>.
    /// </summary>
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

    /// <summary>The outbox registration has no mapping for the given integration event type.</summary>
    public const string OutboxUnmappedEventType = "user_preference.outbox.unmapped_event_type";
}
