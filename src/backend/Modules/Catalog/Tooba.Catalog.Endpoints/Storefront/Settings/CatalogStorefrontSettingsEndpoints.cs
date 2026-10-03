using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Queries;
using Tooba.Catalog.Application.Settings.StoreAppearance.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.Settings;

/// <summary>
/// Catalog-owned storefront settings reads (checkout-identity policy + appearance).
/// MediatR + ApiResponseFactory.
/// </summary>
public static class CatalogStorefrontSettingsEndpoints
{
    /// <summary>Maps checkout-identity-policy and appearance under /v1/storefront.</summary>
    public static void MapCatalogStorefrontSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/storefront");
        group.MapGet("/checkout-identity-policy", GetCheckoutIdentityPolicyAsync);
        group.MapGet("/appearance", GetAppearanceAsync);
    }

    private static async Task<IResult> GetCheckoutIdentityPolicyAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontCheckoutIdentityPolicyQuery(), cancellationToken));

    private static async Task<IResult> GetAppearanceAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontAppearanceQuery(), cancellationToken));
}
