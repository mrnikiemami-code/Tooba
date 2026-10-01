using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Reviews.Application;
using Tooba.Reviews.Application.Commands;
using Tooba.Reviews.Contracts.Errors;

namespace Tooba.Reviews.Endpoints.Customer;

/// <summary>مرز HTTP مشتری Reviews.</summary>
public static class ReviewsCustomerEndpoints
{
    /// <summary>مسیر ثبت نظر مشتری را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost("/v1/customer/reviews", SubmitAsync);
    }

    private static async Task<IResult> SubmitAsync(
        SubmitProductReview body,
        ISender sender,
        IReviewsCustomerActorResolver actorResolver,
        HttpContext http,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = actorResolver.ResolveActor(http);
            if (actor is null)
            {
                return api.FromSemanticException(
                    new SemanticException(new SemanticError(ReviewsErrorCodes.SessionRequired)));
            }

            var id = await sender.Send(new SubmitProductReviewCommand(actor.Value, body), cancellationToken);
            return Results.Json(new { reviewId = id, status = "Pending" }, statusCode: StatusCodes.Status201Created);
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return ReviewsHttpErrors.From(ex, api);
        }
    }
}
