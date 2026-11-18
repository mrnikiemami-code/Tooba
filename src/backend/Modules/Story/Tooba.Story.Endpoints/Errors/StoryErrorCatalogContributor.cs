using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Endpoints.Errors;

/// <summary>کاتالوگ کدهای خطای Story.</summary>
public sealed class StoryErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(StoryErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Story was not found."),
        D(StoryErrorCodes.CtaRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story CTA was rejected."),
        D(StoryErrorCodes.MutationRejected, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story mutation was rejected."),
        D(StoryErrorCodes.TenantMissing, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story tenant was missing."),
        D(StoryErrorCodes.ReviewStatusInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story review status is invalid."),
        D(StoryErrorCodes.ShopInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story shop is invalid."),
        D(StoryErrorCodes.ContentNotReady, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story content is not ready."),
        D(StoryErrorCodes.VersionRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story reviewed version is required."),
        D(StoryErrorCodes.VersionStale, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Story reviewed version is stale."),
        D(StoryErrorCodes.ScheduleInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Story schedule is invalid."),
        D(StoryErrorCodes.MediaForbidden, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Story media is forbidden."),
        D(StoryErrorCodes.OccurrenceConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Story occurrence conflicts."),
    ];

    private static ErrorDescriptor D(string code, ErrorClassification classification, int status, string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
