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
        var actor = actorResolver.ResolveActor(http);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(ProductQnAErrorCodes.SessionRequired));
        }

        var result = await sender.Send(new SubmitProductQuestionCommand(actor.Value, body), cancellationToken);
        var location = result.IsSuccess
            ? $"/v1/customer/product-questions/{result.Value.QuestionId}"
            : "/v1/customer/product-questions";
        return api.Created(location, result);
    }
}
