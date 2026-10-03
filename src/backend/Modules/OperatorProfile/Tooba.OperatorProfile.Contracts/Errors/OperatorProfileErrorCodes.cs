namespace Tooba.OperatorProfile.Contracts.Errors;

/// <summary>Stable semantic error codes owned by OperatorProfile.</summary>
public static class OperatorProfileErrorCodes
{
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
