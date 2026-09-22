using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Host.Storefront;

/// <summary>
/// مرز HTTP خواندنی فروشگاه به‌علاوهٔ درز عمومی سبد مهمان. ترکیب در حافظه است و SQL بین‌schema ندارد.
/// Checkout/pending/shipping Order routes live in Order.Endpoints.
/// </summary>
public static class StorefrontEndpoints
{
    /// <summary>
    /// مسیرهای عمومی فروشگاه را ثبت می‌کند (بدون checkout/shipping/pending — Order.Endpoints).
    /// </summary>
    public static void MapStorefrontEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/storefront");
        group.MapGet("/home", GetHomeAsync);
        group.MapGet("/template-catalog/fashion/preview", GetFashionTemplatePreviewAsync);
        group.MapGet("/template-catalog/{templateKey}/preview", GetIndustryTemplatePreviewAsync);
        group.MapGet("/categories", GetCategoriesAsync);
        group.MapGet("/brands", GetBrandsAsync);
        group.MapGet("/brands/{slug}", GetBrandAsync);
        group.MapGet("/sellers", GetSellersAsync);
        group.MapGet("/sellers/{publicId}", GetSellerAsync);
        group.MapGet("/merchandising/{kind}", GetMerchandisingAsync);
        group.MapGet("/products", GetListingAsync);
        group.MapGet("/products/{slug}", GetDetailAsync);
        group.MapGet("/category-plp/{slug}", GetCategoryPlpAsync);
        group.MapGet("/media/{assetId:guid}", GetPresentationMediaAsync);
        group.MapGet("/checkout-identity-policy", GetCheckoutIdentityPolicyAsync);
        group.MapGet("/appearance", GetAppearanceAsync);
        group.MapGet("/geography/provinces", () => Results.Json(StorefrontIranGeography.Provinces));
    }

    private static async Task<IResult> GetHomeAsync(
        StorefrontComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
        => Results.Json(await composer.GetHomeAsync(locale, cancellationToken));

    private static async Task<IResult> GetFashionTemplatePreviewAsync(
        FashionTemplatePreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var preview = await query.GetFashionSampleAsync(cancellationToken);
        return preview is null
            ? Results.Json(new { title = "Not Found", errorCode = "template_catalog.fashion.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(preview);
    }

    private static async Task<IResult> GetIndustryTemplatePreviewAsync(
        string templateKey,
        IndustryTemplatePreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(templateKey, "fashion", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Json(new { title = "Not Found", errorCode = "template_catalog.use_fashion_route" }, statusCode: StatusCodes.Status404NotFound);
        }

        var preview = await query.GetSampleAsync(templateKey, cancellationToken);
        return preview is null
            ? Results.Json(new { title = "Not Found", errorCode = $"template_catalog.{templateKey}.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(preview);
    }

    private static async Task<IResult> GetCategoriesAsync(StorefrontComposer composer, CancellationToken cancellationToken)
        => Results.Json(await composer.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> GetBrandsAsync(StorefrontComposer composer, CancellationToken cancellationToken)
        => Results.Json(await composer.ListBrandsAsync(cancellationToken));

    private static async Task<IResult> GetBrandAsync(string slug, StorefrontComposer composer, CancellationToken cancellationToken)
    {
        var page = await composer.GetBrandAsync(slug, cancellationToken);
        return page is null
            ? Results.Json(new { title = "Not Found", errorCode = "storefront.brand.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(page);
    }

    private static async Task<IResult> GetSellersAsync(StorefrontComposer composer, CancellationToken cancellationToken)
        => Results.Json(await composer.ListPublicSellersAsync(cancellationToken));

    private static async Task<IResult> GetSellerAsync(string publicId, StorefrontComposer composer, CancellationToken cancellationToken)
    {
        var page = await composer.GetPublicSellerAsync(publicId, cancellationToken);
        return page is null
            ? Results.Json(new { title = "Not Found", errorCode = "storefront.seller.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(page);
    }

    private static async Task<IResult> GetMerchandisingAsync(string kind, StorefrontComposer composer, CancellationToken cancellationToken)
        => Results.Json(await composer.GetMerchandisingAsync(kind, cancellationToken));

    private static async Task<IResult> GetListingAsync(
        StorefrontComposer composer,
        string? q,
        Guid? categoryId,
        Guid? sellerPartyId,
        bool? inStock,
        string? sort,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
        => Results.Json(await composer.GetListingAsync(
            q, categoryId, sellerPartyId, inStock, sort, page, pageSize, cancellationToken));

    private static async Task<IResult> GetDetailAsync(
        string slug,
        Guid? variantId,
        StorefrontComposer composer,
        CancellationToken cancellationToken)
    {
        var page = await composer.GetDetailAsync(slug, variantId, cancellationToken);
        return page is null
            ? Results.Json(new { title = "Not Found", errorCode = "storefront.product.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(page);
    }

    private static async Task<IResult> GetCategoryPlpAsync(
        string slug,
        StorefrontComposer composer,
        HttpRequest request,
        string? locale,
        string? sort,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var filters = ParsePlpFilters(request);
        var pageModel = await composer.GetCategoryPlpAsync(
            locale ?? "fa-IR", slug, filters, sort, page, pageSize, cancellationToken);
        return pageModel is null
            ? Results.Json(new { title = "Not Found", errorCode = "storefront.category.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(pageModel);
    }

    private static IReadOnlyList<StorefrontPlpFilterInput> ParsePlpFilters(HttpRequest request)
    {
        var list = new List<StorefrontPlpFilterInput>();
        foreach (var pair in request.Query)
        {
            var key = pair.Key;
            var raw = pair.Value.ToString();
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (key.StartsWith("f_", StringComparison.OrdinalIgnoreCase))
            {
                var code = key[2..];
                var values = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                list.Add(new StorefrontPlpFilterInput(code, "enum", values, null, null));
            }
            else if (key.StartsWith("r_", StringComparison.OrdinalIgnoreCase))
            {
                var code = key[2..];
                var parts = raw.Split(':', 2);
                decimal? min = parts.Length > 0 && decimal.TryParse(parts[0], out var mn) ? mn : null;
                decimal? max = parts.Length > 1 && decimal.TryParse(parts[1], out var mx) ? mx : null;
                list.Add(new StorefrontPlpFilterInput(code, "range", [], min, max));
            }
            else if (key.StartsWith("b_", StringComparison.OrdinalIgnoreCase))
            {
                var code = key[2..];
                list.Add(new StorefrontPlpFilterInput(code, "boolean", [raw.Trim()], null, null));
            }
        }
        return list;
    }

    private static async Task<IResult> GetPresentationMediaAsync(
        Guid assetId,
        Tooba.Media.Application.IMediaDirectory directory,
        Tooba.Media.Application.IMediaObjectStore store,
        CancellationToken cancellationToken)
    {
        var served = await Tooba.Host.Media.MediaEndpoints.TryServeStoredMediaAsync(
            assetId, directory, store, cancellationToken);
        return served ?? Tooba.Host.Media.MediaEndpoints.PlaceholderSvg(assetId);
    }

    private static async Task<IResult> GetCheckoutIdentityPolicyAsync(
        CheckoutIdentityGate gate,
        CancellationToken cancellationToken)
    {
        var policy = await gate.GetEffectiveAsync(cancellationToken);
        return Results.Json(new
        {
            policy = policy.ToString(),
            cartAnonymousAllowed = true,
            checkoutAuthenticationRequired = policy == Tooba.Catalog.Domain.CheckoutIdentityPolicyKind.AuthenticatedOnly,
        });
    }

    private static async Task<IResult> GetAppearanceAsync(
        StoreAppearanceProjector projector,
        CancellationToken cancellationToken)
    {
        var appearance = await projector.GetEffectiveAsync(cancellationToken);
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
                primaryRgb = appearance.PrimaryRgb,
                primaryStrongRgb = appearance.PrimaryStrongRgb,
                onPrimaryRgb = appearance.OnPrimaryRgb,
                focusRgb = appearance.FocusRgb,
                primaryOnDarkRgb = appearance.PrimaryOnDarkRgb,
            },
            tint = new
            {
                pageBackgroundRgb = appearance.PageBackgroundRgb,
                sectionBackgroundRgb = appearance.SectionBackgroundRgb,
                sectionSurfaceRgb = appearance.SectionBackgroundRgb,
                sectionAlternateRgb = appearance.SectionAlternateRgb,
                sectionAccentRgb = appearance.SectionAccentRgb,
                pageBackgroundDarkRgb = appearance.PageBackgroundDarkRgb,
                sectionBackgroundDarkRgb = appearance.SectionBackgroundDarkRgb,
                sectionSurfaceDarkRgb = appearance.SectionBackgroundDarkRgb,
                sectionAlternateDarkRgb = appearance.SectionAlternateDarkRgb,
                sectionAccentDarkRgb = appearance.SectionAccentDarkRgb,
            },
            updatedAt = appearance.UpdatedAt,
        });
    }
}
