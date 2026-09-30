using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.Story.Application.Queries.GetPublicStories;

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
        catch (InvalidOperationException ex)
        {
            return StoryHttpErrors.TenantMissing(ex);
        }
    }
}
