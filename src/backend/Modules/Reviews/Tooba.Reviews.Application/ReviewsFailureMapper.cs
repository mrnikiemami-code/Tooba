using Tooba.BuildingBlocks;
using Tooba.Reviews.Contracts.Errors;

namespace Tooba.Reviews.Application;

/// <summary>Maps known Reviews InvalidOperation failures to stable SemanticError codes.</summary>
public static class ReviewsFailureMapper
{
    /// <summary>Converts an InvalidOperationException into a SemanticException with a stable code.</summary>
    public static SemanticException ToSemantic(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message ?? string.Empty;
        if (message.Contains("قبلاً", StringComparison.Ordinal))
            return new SemanticException(new SemanticError(ReviewsErrorCodes.Duplicate));

        return new SemanticException(new SemanticError(ReviewsErrorCodes.Rejected));
    }

    /// <summary>Converts moderation InvalidOperation failures.</summary>
    public static SemanticException ToModerationSemantic(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new SemanticException(new SemanticError(ReviewsErrorCodes.ModerationRejected));
    }
}
