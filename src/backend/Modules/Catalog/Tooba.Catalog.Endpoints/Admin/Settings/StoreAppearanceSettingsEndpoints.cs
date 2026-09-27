using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Settings.StoreAppearance.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Settings;

/// <summary>Admin store-appearance settings — Catalog-owned HTTP via MediatR.</summary>
public static class StoreAppearanceSettingsEndpoints
{
    /// <summary>Maps GET/PUT /v1/admin/settings/appearance.</summary>
    public static void MapStoreAppearanceSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/settings/appearance");
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
        return api.From(await sender.Send(new GetStoreAppearanceAdminSettingsQuery(), cancellationToken));
    }

    private static async Task<IResult> PutAsync(
        StoreAppearanceSettingsWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        await sender.Send(
            new SaveStoreAppearanceSettingsCommand(
                new StoreAppearanceSettingsWriteModel(
                    body.PaletteKey,
                    body.ThemeMode,
                    body.ProductCardSkin,
                    body.BackgroundStyle)),
            cancellationToken);
        return api.From(await sender.Send(new GetStoreAppearanceAdminSettingsQuery(), cancellationToken));
    }
}

/// <summary>بدنه ذخیره ظاهر؛ PaletteKey الزامی و ThemeMode اختیاری.</summary>
public sealed record StoreAppearanceSettingsWriteRequest(
    string? PaletteKey,
    string? ThemeMode = null,
    string? ProductCardSkin = null,
    string? BackgroundStyle = null);
