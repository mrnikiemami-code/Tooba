using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.PageComposition.Application.Presentation;

namespace Tooba.PageComposition.Endpoints;

/// <summary>Canonical PageComposition HTTP error presentation helpers (no message-text classification).</summary>
internal static class PageCompositionHttpErrors
{
    public static IResult From(Exception exception, ApiResponseFactory api) =>
        exception switch
        {
            SemanticException semantic => api.FromSemanticException(semantic),
            PlatformHttpException platform => api.FromPlatformException(platform),
            _ => throw exception,
        };

    public static Guid RequireTenantId(ICurrentTenant tenant) =>
        PageCompositionPresentationComposer.RequireTenantId(tenant);
}
