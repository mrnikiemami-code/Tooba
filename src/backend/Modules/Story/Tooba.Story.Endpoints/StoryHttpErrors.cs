using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Endpoints;

/// <summary>Canonical Story HTTP error presentation helpers (no message-text classification).</summary>
internal static class StoryHttpErrors
{
    public static IResult From(Exception exception, ApiResponseFactory api) =>
        exception switch
        {
            SemanticException semantic => api.FromSemanticException(semantic),
            PlatformHttpException platform => api.FromPlatformException(platform),
            _ => throw exception,
        };

    public static Guid RequireTenantId(ICurrentTenant tenant) =>
        StoryPresentationComposer.RequireTenantId(tenant);
}
