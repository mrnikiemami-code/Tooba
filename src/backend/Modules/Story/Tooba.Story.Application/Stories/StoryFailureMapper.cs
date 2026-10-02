using Tooba.BuildingBlocks;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;

namespace Tooba.Story.Application.Stories;

/// <summary>Story application helpers for transport parsing (no message-text failure classification).</summary>
public static class StoryFailureMapper
{
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
