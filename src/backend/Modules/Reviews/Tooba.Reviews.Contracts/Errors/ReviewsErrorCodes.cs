namespace Tooba.Reviews.Contracts.Errors;

/// <summary>Stable semantic error codes owned by Reviews.</summary>
public static class ReviewsErrorCodes
{
    /// <summary>Duplicate review for the same product/actor.</summary>
    public const string Duplicate = "reviews.duplicate";

    /// <summary>Review submission was rejected by domain rules.</summary>
    public const string Rejected = "reviews.rejected";

    /// <summary>Moderation action was rejected (wrong state / missing).</summary>
    public const string ModerationRejected = "reviews.moderation.rejected";

    /// <summary>Shared foundation customer session required (do not re-register descriptor).</summary>
    public const string SessionRequired = "customer.session.required";
}
