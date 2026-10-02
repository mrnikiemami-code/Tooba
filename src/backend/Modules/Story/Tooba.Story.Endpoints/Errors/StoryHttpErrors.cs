using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Endpoints.Errors;

/// <summary>Canonical Story HTTP helpers (Result pipeline; no message-text classification).</summary>
internal static class StoryHttpErrors
{
    public static Result<Guid> ResolveTenantId(ICurrentTenant tenant)
    {
        try
        {
            return Result.Success(StoryPresentationComposer.RequireTenantId(tenant));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<Guid>(ex.Error);
        }
    }
}
