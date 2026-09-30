using MediatR;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Queries;
using Tooba.Catalog.Application.Settings.StoreAppearance.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.Settings;

/// <summary>
/// Catalog-owned storefront settings reads (checkout-identity policy + appearance).
/// Response shapes preserved from Host residual StorefrontEndpoints.
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
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontCheckoutIdentityPolicyQuery(), cancellationToken);
        if (result.IsFailure)
            return Results.Json(new { title = "Error", errorCode = result.Errors[0].Code }, statusCode: StatusCodes.Status500InternalServerError);

        var dto = result.Value;
        return Results.Json(new
        {
            policy = dto.Policy,
            cartAnonymousAllowed = dto.CartAnonymousAllowed,
            checkoutAuthenticationRequired = dto.CheckoutAuthenticationRequired,
        });
    }

    private static async Task<IResult> GetAppearanceAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontAppearanceQuery(), cancellationToken);
        if (result.IsFailure)
            return Results.Json(new { title = "Error", errorCode = result.Errors[0].Code }, statusCode: StatusCodes.Status500InternalServerError);

        var appearance = result.Value;
        return Results.Json(new
        {
            storeScope = appearance.StoreScope,
            paletteKey = appearance.PaletteKey,
            paletteKeyWasKnown = appearance.PaletteKeyWasKnown,
            themeMode = appearance.ThemeMode,
            productCardSkin = appearance.ProductCardSkin,
            backgroundStyle = appearance.BackgroundStyle,
            tokens = new
            {
                primaryRgb = appearance.Tokens.PrimaryRgb,
                primaryStrongRgb = appearance.Tokens.PrimaryStrongRgb,
                onPrimaryRgb = appearance.Tokens.OnPrimaryRgb,
                focusRgb = appearance.Tokens.FocusRgb,
                primaryOnDarkRgb = appearance.Tokens.PrimaryOnDarkRgb,
            },
            tint = new
            {
                pageBackgroundRgb = appearance.Tint.PageBackgroundRgb,
                sectionBackgroundRgb = appearance.Tint.SectionBackgroundRgb,
                sectionSurfaceRgb = appearance.Tint.SectionSurfaceRgb,
                sectionAlternateRgb = appearance.Tint.SectionAlternateRgb,
                sectionAccentRgb = appearance.Tint.SectionAccentRgb,
                pageBackgroundDarkRgb = appearance.Tint.PageBackgroundDarkRgb,
                sectionBackgroundDarkRgb = appearance.Tint.SectionBackgroundDarkRgb,
                sectionSurfaceDarkRgb = appearance.Tint.SectionSurfaceDarkRgb,
                sectionAlternateDarkRgb = appearance.Tint.SectionAlternateDarkRgb,
                sectionAccentDarkRgb = appearance.Tint.SectionAccentDarkRgb,
            },
            updatedAt = appearance.UpdatedAt,
        });
    }
}
