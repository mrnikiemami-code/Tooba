using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Reviews.Application.Commands;
using Tooba.Reviews.Application.Models;
using Tooba.Reviews.Application.Queries;

namespace Tooba.Reviews.Endpoints.Admin;

/// <summary>مرز HTTP مدیریتی Reviews.</summary>
public static class ReviewsAdminEndpoints
{
    /// <summary>مسیرهای Admin را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin/reviews");
        admin.MapGet("", PendingAsync);
        admin.MapPost("/query", QueryPendingGridAsync);
        admin.MapPost("/{reviewId:guid}/publish", PublishAsync);
        admin.MapPost("/{reviewId:guid}/reject", RejectAsync);
    }

    private static async Task<IResult> PendingAsync(
        ISender sender,
        IReviewsAdminAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            return Results.Json(await sender.Send(new ListPendingAdminReviewsQuery(page, pageSize), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return ReviewsHttpErrors.From(ex, api);
        }
    }

    private static async Task<IResult> QueryPendingGridAsync(
        GridQueryRequest body,
        ISender sender,
        IReviewsAdminAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            return Results.Json(await sender.Send(new QueryPendingAdminReviewGridQuery(body), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return ReviewsHttpErrors.From(ex, api);
        }
    }

    private static async Task<IResult> PublishAsync(
        Guid reviewId,
        ISender sender,
        IReviewsAdminAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await auth.RequireAuthorizedAsync(http, cancellationToken);
            await sender.Send(new PublishAdminReviewCommand(reviewId, actor), cancellationToken);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return ReviewsHttpErrors.From(ex, api);
        }
    }

    private static async Task<IResult> RejectAsync(
        Guid reviewId,
        ISender sender,
        IReviewsAdminAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        RejectReviewRequest? body = null)
    {
        try
        {
            var actor = await auth.RequireAuthorizedAsync(http, cancellationToken);
            await sender.Send(
                new RejectAdminReviewCommand(reviewId, actor, body?.Reason ?? "رد توسط مدیر"),
                cancellationToken);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return ReviewsHttpErrors.From(ex, api);
        }
    }
}
