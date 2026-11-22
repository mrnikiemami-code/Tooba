using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Seller.Queries;

#pragma warning disable CS1591
namespace Tooba.Catalog.Endpoints.Admin;

/// <summary>Admin published Catalog variant picker for the AdminShell Offer create form.</summary>
public static class CatalogOfferPickerAdminEndpoints
{
    public static void MapCatalogOfferPickerAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/catalog");
        group.MapGet("/variants", ListAsync);
    }

    private static async Task<IResult> ListAsync(
        string? search,
        int? page,
        int? pageSize,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SearchPublishedCatalogVariantsQuery(null, search, page ?? 1, pageSize ?? 20),
            cancellationToken));
    }
}

