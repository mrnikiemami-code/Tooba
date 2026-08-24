namespace Tooba.Host.Storefront;

/// <summary>
/// مرز HTTP خواندنی فروشگاه. ترکیب در حافظه است و SQL بین‌schema ندارد.
/// </summary>
public static class StorefrontEndpoints
{
    /// <summary>
    /// مسیرهای عمومی فروشگاه را ثبت می‌کند.
    /// </summary>
    public static void MapStorefrontEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/storefront");
        group.MapGet("/home", GetHomeAsync);
        group.MapGet("/categories", GetCategoriesAsync);
        group.MapGet("/products", GetListingAsync);
        group.MapGet("/products/{slug}", GetDetailAsync);
        group.MapGet("/media/{assetId:guid}", GetPresentationMediaAsync);
    }

    private static async Task<IResult> GetHomeAsync(StorefrontComposer composer, CancellationToken cancellationToken)
        => Results.Json(await composer.GetHomeAsync(cancellationToken));

    private static async Task<IResult> GetCategoriesAsync(StorefrontComposer composer, CancellationToken cancellationToken)
        => Results.Json(await composer.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> GetListingAsync(
        StorefrontComposer composer,
        string? q,
        Guid? categoryId,
        CancellationToken cancellationToken)
        => Results.Json(await composer.GetListingAsync(q, categoryId, cancellationToken));

    private static async Task<IResult> GetDetailAsync(string slug, StorefrontComposer composer, CancellationToken cancellationToken)
    {
        var page = await composer.GetDetailAsync(slug, cancellationToken);
        return page is null
            ? Results.Json(new { title = "Not Found", errorCode = "storefront.product.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(page);
    }

    /// <summary>
    /// تصویر نمایشی توسعه برای مرجع مات Media. URL دارایی حقیقت کسب‌وکار Product نیست.
    /// </summary>
    private static IResult GetPresentationMediaAsync(Guid assetId)
    {
        var hue = Math.Abs(assetId.GetHashCode()) % 40 + 200;
        var svg =
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 640 640\" role=\"img\" aria-label=\"نمایش موقت رسانه\">" +
            $"<defs><linearGradient id=\"g\" x1=\"0\" x2=\"1\"><stop offset=\"0\" stop-color=\"hsl({hue},70%,46%)\"/>" +
            $"<stop offset=\"1\" stop-color=\"hsl({hue + 20},62%,38%)\"/></linearGradient></defs>" +
            $"<rect width=\"640\" height=\"640\" rx=\"28\" fill=\"url(#g)\"/>" +
            $"<text x=\"320\" y=\"330\" text-anchor=\"middle\" fill=\"white\" font-size=\"36\" font-family=\"Tahoma\">Tooba</text>" +
            $"</svg>";
        return Results.Text(svg, "image/svg+xml; charset=utf-8");
    }
}
