using Tooba.Host.Storefront;

namespace Tooba.Host.Storefront;

/// <summary>
/// مرز HTTP خواندنی فروشگاه به‌علاوهٔ درز عمومی سبد مهمان. ترکیب در حافظه است و SQL بین‌schema ندارد.
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
        group.MapPost("/cart", CreateGuestCartAsync);
        group.MapGet("/cart/{cartId:guid}", GetCartAsync);
        group.MapPost("/cart/{cartId:guid}/lines", AddCartLineAsync);
        group.MapPatch("/cart/{cartId:guid}/lines/{lineId:guid}", ChangeCartLineAsync);
        group.MapDelete("/cart/{cartId:guid}/lines/{lineId:guid}", RemoveCartLineAsync);
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

    private static Task<IResult> CreateGuestCartAsync(StorefrontCartComposer composer, CancellationToken cancellationToken)
        => ExecuteCartAsync(() => composer.CreateGuestAsync(cancellationToken));

    private static async Task<IResult> GetCartAsync(
        Guid cartId,
        StorefrontCartComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        return await ExecuteCartAsync(async () =>
        {
            var page = await composer.GetAsync(cartId, ReadGuestSecret(request), cancellationToken);
            if (page is null)
            {
                throw new InvalidOperationException("سبد پیدا نشد.");
            }

            return page;
        });
    }

    private static Task<IResult> AddCartLineAsync(
        Guid cartId,
        StorefrontAddCartLineRequest body,
        StorefrontCartComposer composer,
        HttpRequest request,
        int? expectedVersion,
        CancellationToken cancellationToken)
        => ExecuteCartAsync(() => composer.AddLineAsync(
            cartId,
            ReadGuestSecret(request),
            ReadExpectedVersion(request, expectedVersion),
            body.OfferId,
            body.Quantity,
            cancellationToken));

    private static Task<IResult> ChangeCartLineAsync(
        Guid lineId,
        Guid cartId,
        StorefrontChangeCartLineRequest body,
        StorefrontCartComposer composer,
        HttpRequest request,
        int? expectedVersion,
        CancellationToken cancellationToken)
        => ExecuteCartAsync(() => composer.ChangeLineAsync(
            cartId,
            ReadGuestSecret(request),
            ReadExpectedVersion(request, expectedVersion),
            lineId,
            body.Quantity,
            cancellationToken));

    private static Task<IResult> RemoveCartLineAsync(
        Guid lineId,
        Guid cartId,
        StorefrontCartComposer composer,
        HttpRequest request,
        int? expectedVersion,
        CancellationToken cancellationToken)
        => ExecuteCartAsync(() => composer.RemoveLineAsync(
            cartId,
            ReadGuestSecret(request),
            ReadExpectedVersion(request, expectedVersion),
            lineId,
            cancellationToken));

    private static string? ReadGuestSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Tooba-Guest-Secret", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return request.Cookies.TryGetValue("tooba_guest_secret", out var cookie) ? cookie : null;
    }

    private static int ReadExpectedVersion(HttpRequest request, int? expectedVersion)
    {
        if (expectedVersion is int queryVersion)
        {
            return queryVersion;
        }

        if (request.Headers.TryGetValue("X-Tooba-Cart-Version", out var header) && int.TryParse(header, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ جهش همزمان خط رد شد.");
    }

    private static async Task<IResult> ExecuteCartAsync(Func<Task<StorefrontCartPage>> action)
    {
        try
        {
            return Results.Json(await action());
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapCartException(exception);
            return Results.Json(new { title = mapped.Title, errorCode = mapped.Code, detail = exception.Message }, statusCode: mapped.Status);
        }
    }

    private static (int Status, string Title, string Code) MapCartException(InvalidOperationException exception)
    {
        var text = exception.Message;
        if (text.Contains("پیدا نشد", StringComparison.Ordinal))
        {
            return (StatusCodes.Status404NotFound, "Not Found", "cart.missing");
        }

        if (text.Contains("راز", StringComparison.Ordinal) || text.Contains("مجوز", StringComparison.Ordinal))
        {
            return (StatusCodes.Status401Unauthorized, "Unauthorized", "cart.guest.invalid");
        }

        if (text.Contains("کهنه", StringComparison.Ordinal) || text.Contains("همزمان", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "cart.version.conflict");
        }

        if (text.Contains("موجودی", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "cart.inventory.insufficient");
        }

        if (text.Contains("تعداد", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "cart.quantity.invalid");
        }

        if (text.Contains("Offer", StringComparison.Ordinal) || text.Contains("غیرفعال", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "cart.offer.unavailable");
        }

        return (StatusCodes.Status400BadRequest, "Bad Request", "cart.rejected");
    }
}
