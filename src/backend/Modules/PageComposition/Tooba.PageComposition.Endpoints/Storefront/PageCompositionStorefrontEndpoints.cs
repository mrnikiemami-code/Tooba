using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.PageComposition.Application.Queries;

namespace Tooba.PageComposition.Endpoints.Storefront;

/// <summary>مرز HTTP عمومی PageComposition.</summary>
public static class PageCompositionStorefrontEndpoints
{
    /// <summary>مسیر عمومی storefront را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/storefront/home/composition", GetHomeCompositionAsync);
    }

    private static async Task<IResult> GetHomeCompositionAsync(
        ISender sender,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = PageCompositionHttpErrors.RequireTenantId(tenant);
            return Results.Json(await sender.Send(
                new GetHomeCompositionQuery(tenantId, locale), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return PageCompositionHttpErrors.From(ex, api);
        }
    }
}
