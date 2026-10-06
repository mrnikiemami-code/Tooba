namespace Tooba.OperatorProfile.Contracts.Errors;

/// <summary>
/// Stable semantic error codes owned by OperatorProfile. Values are the machine codes emitted by the
/// OperatorProfile Domain/Application/Infrastructure and mapped by the canonical composed error catalog;
/// they must never be renamed or repurposed.
/// </summary>
public static class OperatorProfileErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        ProfileRejected,
        ActorRequired,
        InvalidDisplayName,
        InvalidFirstName,
        InvalidLastName,
        InvalidBio,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code declared by this OperatorProfile catalog.
    /// Used by the module composition seam so OperatorProfile faults map to <c>Result</c> while codes
    /// owned by another module propagate untouched to the canonical global exception boundary.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>Operator profile write rejected by domain or actor rules.</summary>
    public const string ProfileRejected = "operator.profile.rejected";

    /// <summary>Actor user id missing for an operator-profile request.</summary>
    public const string ActorRequired = "operator.profile.validation.actor_required";

    /// <summary>Display name failed transport-shape validation.</summary>
    public const string InvalidDisplayName = "operator.profile.validation.display_name";

    /// <summary>First name failed transport-shape validation.</summary>
    public const string InvalidFirstName = "operator.profile.validation.first_name";

    /// <summary>Last name failed transport-shape validation.</summary>
    public const string InvalidLastName = "operator.profile.validation.last_name";

    /// <summary>Bio failed transport-shape validation.</summary>
    public const string InvalidBio = "operator.profile.validation.bio";
}
