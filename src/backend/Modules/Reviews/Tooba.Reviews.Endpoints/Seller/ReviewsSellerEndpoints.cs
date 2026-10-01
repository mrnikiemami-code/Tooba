using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Reviews.Application.Queries;

namespace Tooba.Reviews.Endpoints.Seller;

/// <summary>مرز HTTP فروشنده Reviews.</summary>
public static class ReviewsSellerEndpoints
{
    /// <summary>مسیر فهرست فروشنده را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/seller/reviews", SellerListAsync);
    }

    private static async Task<IResult> SellerListAsync(
        ISender sender,
        IReviewsSellerAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, cancellationToken);
            return Results.Json(await sender.Send(
                new ListSellerReviewsQuery(sellerPartyId, status, page, pageSize), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return ReviewsHttpErrors.From(ex, api);
        }
    }
}
