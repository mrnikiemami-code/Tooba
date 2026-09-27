using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Settings.Quantity.Commands;
using Tooba.Catalog.Application.Settings.Quantity.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Settings;

/// <summary>Admin quantity-rounding settings — Catalog-owned HTTP via MediatR.</summary>
public static class QuantitySettingsEndpoints
{
    /// <summary>Maps GET/PUT /v1/admin/settings/quantity-rounding.</summary>
    public static void MapQuantitySettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/settings/quantity-rounding");
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetStoreQuantitySettingsQuery(), cancellationToken));
    }

    private static async Task<IResult> PutAsync(
        StoreQuantitySettingsWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SaveStoreQuantitySettingsCommand(body.GlobalRoundingMode),
            cancellationToken));
    }
}

/// <summary>PUT body for quantity-rounding settings.</summary>
/// <param name="GlobalRoundingMode">Floor / Ceiling / Nearest.</param>
public sealed record StoreQuantitySettingsWriteRequest(string GlobalRoundingMode);
