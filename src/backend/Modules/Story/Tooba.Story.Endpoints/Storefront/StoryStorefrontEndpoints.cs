using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Story.Application.Stories.Queries.Storefront;

namespace Tooba.Story.Endpoints.Storefront;

/// <summary>مرز HTTP عمومی Story.</summary>
public static class StoryStorefrontEndpoints
{
    /// <summary>مسیر عمومی storefront را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/storefront/stories", GetPublicStoriesAsync);
    }

    private static async Task<IResult> GetPublicStoriesAsync(
        ISender sender,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        string? market = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            return Results.Json(await sender.Send(
                new GetPublicStoriesQuery(tenantId, locale, market), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return StoryHttpErrors.From(ex, api);
        }
    }
}
