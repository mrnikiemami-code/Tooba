using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Reviews.Application.Queries;
using Tooba.Reviews.Contracts.Errors;

namespace Tooba.Reviews.Endpoints.Storefront;

/// <summary>مرز HTTP عمومی Reviews.</summary>
public static class ReviewsStorefrontEndpoints
{
    /// <summary>مسیر عمومی storefront را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/storefront/products/{slug}/reviews", GetPublishedAsync);
    }

    private static async Task<IResult> GetPublishedAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await sender.Send(new GetPublishedReviewsQuery(slug, page, pageSize), cancellationToken);
            return result is null ? Results.NotFound() : Results.Json(result);
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return ReviewsHttpErrors.From(ex, api);
        }
    }
}
