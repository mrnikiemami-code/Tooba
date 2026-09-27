using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Commands;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Settings;

/// <summary>Admin checkout-identity settings — Catalog-owned HTTP via MediatR.</summary>
public static class CheckoutIdentitySettingsEndpoints
{
    /// <summary>Maps GET/PUT /v1/admin/settings/checkout-identity.</summary>
    public static void MapCheckoutIdentitySettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/settings/checkout-identity");
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
        return api.From(await sender.Send(new GetCheckoutIdentitySettingsQuery(), cancellationToken));
    }

    private static async Task<IResult> PutAsync(
        CheckoutIdentitySettingsWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SaveCheckoutIdentitySettingsCommand(body.Policy, actor),
            cancellationToken));
    }
}

/// <summary>PUT body for checkout-identity settings.</summary>
public sealed record CheckoutIdentitySettingsWriteRequest(string? Policy);
