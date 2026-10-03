using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.ProductQnA.Application.Storefront.Queries;

namespace Tooba.ProductQnA.Endpoints.Storefront;

/// <summary>مرز HTTP عمومی ProductQnA.</summary>
public static class ProductQnAStorefrontEndpoints
{
    /// <summary>مسیر عمومی پرسش‌های Published را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/storefront/products/{slug}/questions", GetPublishedAsync);
    }

    private static async Task<IResult> GetPublishedAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetPublishedQuestionsQuery(slug, page, pageSize), cancellationToken);
        return api.From(result);
    }
}
