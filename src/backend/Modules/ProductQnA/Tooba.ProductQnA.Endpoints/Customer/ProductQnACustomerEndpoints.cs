using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.ProductQnA.Application.Customer.Commands;
using Tooba.ProductQnA.Application.Models;
using Tooba.ProductQnA.Contracts.Errors;

namespace Tooba.ProductQnA.Endpoints.Customer;

/// <summary>مرز HTTP مشتری ProductQnA.</summary>
public static class ProductQnACustomerEndpoints
{
    /// <summary>مسیر ثبت پرسش مشتری را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost("/v1/customer/product-questions", SubmitAsync);
    }

    private static async Task<IResult> SubmitAsync(
        SubmitProductQuestion body,
        ISender sender,
        IProductQnACustomerActorResolver actorResolver,
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
                    new SemanticException(new SemanticError(ProductQnAErrorCodes.SessionRequired)));
            }

            var id = await sender.Send(new SubmitProductQuestionCommand(actor.Value, body), cancellationToken);
            return Results.Json(new { questionId = id, status = "Pending" }, statusCode: StatusCodes.Status201Created);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
    }
}
