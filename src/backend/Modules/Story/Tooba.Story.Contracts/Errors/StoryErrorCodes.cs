namespace Tooba.Story.Contracts.Errors;

/// <summary>Stable semantic error codes owned by Story.</summary>
public static class StoryErrorCodes
{
    /// <summary>Story entity was not found.</summary>
    public const string Missing = "story.missing";

    /// <summary>CTA target was rejected as unsafe.</summary>
    public const string CtaRejected = "story.cta.rejected";

    /// <summary>Mutation was rejected by domain rules.</summary>
    public const string MutationRejected = "story.mutation.rejected";

    /// <summary>Tenant could not be resolved for Story.</summary>
    public const string TenantMissing = "story.tenant.missing";

    /// <summary>Review status query value is invalid.</summary>
    public const string ReviewStatusInvalid = "story.reviewStatus.invalid";
}
