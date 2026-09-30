using Tooba.BuildingBlocks;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain;

namespace Tooba.Story.Application;

/// <summary>Maps known Story domain/application InvalidOperation failures to stable SemanticError codes.</summary>
public static class StoryFailureMapper
{
    /// <summary>Converts an InvalidOperationException into a SemanticException with a stable code.</summary>
    public static SemanticException ToSemantic(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message ?? string.Empty;
        if (message.Contains("Tenant", StringComparison.OrdinalIgnoreCase)
            || message.Contains("resolve", StringComparison.OrdinalIgnoreCase))
        {
            return new SemanticException(new SemanticError(StoryErrorCodes.TenantMissing));
        }

        if (message.Contains("یافت نشد", StringComparison.Ordinal))
            return new SemanticException(new SemanticError(StoryErrorCodes.Missing));

        if (message.Contains("ناامن", StringComparison.Ordinal))
            return new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

        return new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
    }

    /// <summary>Parses transport review-status text into Domain enum without Endpoints touching Domain.</summary>
    public static bool TryParseReviewStatus(string? raw, out StoryReviewStatus? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!Enum.TryParse<StoryReviewStatus>(raw, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return false;

        value = parsed;
        return true;
    }

    /// <summary>Throws SemanticException when review-status transport value is invalid.</summary>
    public static StoryReviewStatus? RequireReviewStatus(string? raw)
    {
        if (!TryParseReviewStatus(raw, out var value))
            throw new SemanticException(new SemanticError(StoryErrorCodes.ReviewStatusInvalid));
        return value;
    }
}
