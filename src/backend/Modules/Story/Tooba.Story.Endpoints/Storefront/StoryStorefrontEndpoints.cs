using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Story.Application.Stories.Queries.Storefront;
using Tooba.Story.Endpoints.Errors;

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
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        return api.From(await sender.Send(
            new GetPublicStoriesQuery(tenantResult.Value, locale, market), cancellationToken));
    }
}
