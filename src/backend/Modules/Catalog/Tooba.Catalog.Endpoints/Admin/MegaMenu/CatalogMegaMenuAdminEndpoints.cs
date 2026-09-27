using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.MegaMenu.Commands;
using Tooba.Catalog.Application.MegaMenu.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.MegaMenu;

/// <summary>Admin Catalog MegaMenu HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogMegaMenuAdminEndpoints
{
    /// <summary>Maps four Admin MegaMenu routes.</summary>
    public static void MapCatalogMegaMenuAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/mega-menu");
        categories.MapGet("", GetCategoryMegaMenuAsync);
        categories.MapGet("/placement-options", ListPlacementOptionsAsync);
        categories.MapPut("", UpsertCategoryMegaMenuAsync);
        categories.MapDelete("", RemoveCategoryMegaMenuAsync);
    }

    private static async Task<IResult> GetCategoryMegaMenuAsync(
        Guid categoryId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetCategoryMegaMenuQuery(categoryId, locale), cancellationToken));
    }

    private static async Task<IResult> ListPlacementOptionsAsync(
        Guid categoryId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListMegaMenuPlacementOptionsQuery(categoryId, locale), cancellationToken));
    }

    private static async Task<IResult> UpsertCategoryMegaMenuAsync(
        Guid categoryId,
        CategoryMegaMenuBindingInput body,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new UpsertCategoryMegaMenuCommand(categoryId, locale, body), cancellationToken));
    }

    private static async Task<IResult> RemoveCategoryMegaMenuAsync(
        Guid categoryId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new RemoveCategoryMegaMenuCommand(categoryId), cancellationToken));
    }
}
