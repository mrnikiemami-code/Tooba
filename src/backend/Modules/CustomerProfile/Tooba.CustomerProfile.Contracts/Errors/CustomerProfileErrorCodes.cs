namespace Tooba.CustomerProfile.Contracts.Errors;

/// <summary>
/// Stable CustomerProfile semantic error codes — the single catalog owner for this module's
/// descriptive-profile faults. Codes are machine-stable; user-facing text lives only in the
/// Endpoints error resources.
/// </summary>
public static class CustomerProfileErrorCodes
{
    /// <summary>
    /// A trusted customer identity is required. The descriptor and both-culture resources for this
    /// cross-cutting session code are owned by the Foundation error catalog; CustomerProfile consumes
    /// the code without re-registering a descriptor.
    /// </summary>
    public const string SessionRequired = "customer.session.required";

    /// <summary>The server-trusted actor identity supplied by the boundary is not usable.</summary>
    public const string ActorRequired = "customer.profile.actor_required";

    /// <summary>Display name must be present and within the bounded length rule.</summary>
    public const string DisplayNameInvalid = "customer.profile.display_name_invalid";

    /// <summary>First name exceeds the bounded length rule.</summary>
    public const string FirstNameInvalid = "customer.profile.first_name_invalid";

    /// <summary>Last name exceeds the bounded length rule.</summary>
    public const string LastNameInvalid = "customer.profile.last_name_invalid";

    /// <summary>Birth date exceeds the bounded length rule.</summary>
    public const string BirthDateInvalid = "customer.profile.birth_date_invalid";

    /// <summary>Bio exceeds the bounded length rule.</summary>
    public const string BioInvalid = "customer.profile.bio_invalid";
}
