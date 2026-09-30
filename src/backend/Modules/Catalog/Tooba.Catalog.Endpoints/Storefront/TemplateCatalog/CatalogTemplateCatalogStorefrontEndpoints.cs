using MediatR;
using Tooba.Catalog.Application.TemplateCatalog.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.TemplateCatalog;

/// <summary>
/// Template-catalog storefront previews owned by Catalog.
/// Response shapes preserved from Host residual (raw JSON + stable errorCode on 404).
/// </summary>
public static class CatalogTemplateCatalogStorefrontEndpoints
{
    /// <summary>Maps Fashion + Industry template preview routes under /v1/storefront.</summary>
    public static void MapCatalogTemplateCatalogStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/storefront");
        group.MapGet("/template-catalog/fashion/preview", GetFashionTemplatePreviewAsync);
        group.MapGet("/template-catalog/{templateKey}/preview", GetIndustryTemplatePreviewAsync);
    }

    private static async Task<IResult> GetFashionTemplatePreviewAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFashionTemplatePreviewQuery(), cancellationToken);
        if (result.IsFailure)
        {
            var code = result.Errors[0].Code;
            return Results.Json(
                new { title = "Not Found", errorCode = code },
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Json(result.Value);
    }

    private static async Task<IResult> GetIndustryTemplatePreviewAsync(
        string templateKey,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetIndustryTemplatePreviewQuery(templateKey), cancellationToken);
        if (result.IsFailure)
        {
            var code = result.Errors[0].Code;
            return Results.Json(
                new { title = "Not Found", errorCode = code },
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Json(result.Value);
    }
}
