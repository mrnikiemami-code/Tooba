using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.TemplateCatalog.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.TemplateCatalog;

/// <summary>
/// Template-catalog storefront previews owned by Catalog via MediatR + ApiResponseFactory.
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
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetFashionTemplatePreviewQuery(), cancellationToken));

    private static async Task<IResult> GetIndustryTemplatePreviewAsync(
        string templateKey,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetIndustryTemplatePreviewQuery(templateKey), cancellationToken));
}
