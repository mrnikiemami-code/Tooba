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
        group.MapPost("/checkout/preview", PreviewCheckoutAsync);
        group.MapPost("/checkout", SubmitCheckoutAsync);
        group.MapGet("/checkout/{checkoutId:guid}", GetCheckoutAsync);
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

    private static Task<IResult> PreviewCheckoutAsync(
        Guid cartId,
        StorefrontCheckoutComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteCheckoutAsync(() => composer.PreviewAsync(cartId, ReadGuestSecret(request), cancellationToken));

    private static Task<IResult> SubmitCheckoutAsync(
        StorefrontSubmitCheckoutRequest body,
        StorefrontCheckoutComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteCheckoutAsync(() => composer.SubmitAsync(
            body.CartId,
            ReadGuestSecret(request),
            body.ExpectedCartVersion,
            body.IdempotencyKey,
            body.Shipping,
            cancellationToken));

    private static async Task<IResult> GetCheckoutAsync(
        Guid checkoutId,
        Guid cartId,
        StorefrontCheckoutComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        return await ExecuteCheckoutAsync(async () =>
        {
            var page = await composer.GetAsync(checkoutId, cartId, ReadGuestSecret(request), cancellationToken);
            return page ?? throw new InvalidOperationException("سفارش پیدا نشد.");
        });
    }

    private static async Task<IResult> ExecuteCheckoutAsync(Func<Task<StorefrontCheckoutPage>> action)
    {
        try
        {
            return Results.Json(await action());
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapCheckoutException(exception);
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapCheckoutCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
        }
    }

    private static (int Status, string Title, string Code) MapCheckoutException(InvalidOperationException exception)
    {
        var text = exception.Message;
        if (text.Contains("پیدا نشد", StringComparison.Ordinal))
        {
            return (StatusCodes.Status404NotFound, "Not Found", "checkout.missing");
        }

        if (text.Contains("PRICE_CHANGED", StringComparison.Ordinal) || text.Contains("قیمت", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.price.changed");
        }

        if (text.Contains("TAX_", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.tax.unavailable");
        }

        if (text.Contains("منقضی", StringComparison.Ordinal) || text.Contains("Active", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.cart.expired");
        }

        if (text.Contains("ارسال", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "checkout.shipping.incomplete");
        }

        if (text.Contains("خالی", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "checkout.cart.empty");
        }

        if (text.Contains("کهنه", StringComparison.Ordinal) || text.Contains("همزمان", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.version.conflict");
        }

        return (StatusCodes.Status400BadRequest, "Bad Request", "checkout.rejected");
    }

    private static string MapCheckoutCustomerDetail(string code) => code switch
    {
        "checkout.price.changed" => "قیمت یکی از کالاها تغییر کرده؛ لطفاً سفارش را دوباره بررسی کنید.",
        "checkout.tax.unavailable" => "محاسبهٔ مالیات این سفارش الان ممکن نیست. لطفاً دوباره تلاش کنید.",
        "checkout.cart.expired" => "سبد خرید منقضی شده است.",
        "checkout.shipping.incomplete" => "اطلاعات ارسال کامل نیست.",
        "checkout.cart.empty" => "سبد خرید خالی است.",
        "checkout.version.conflict" => "سبد هم‌زمان به‌روز شده است. صفحه را تازه کنید.",
        "checkout.missing" => "سفارش پیدا نشد.",
        _ => "ثبت سفارش انجام نشد. لطفاً دوباره تلاش کنید.",
    };

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
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapCartCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
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

        if (text.Contains("Held", StringComparison.Ordinal)
            || text.Contains("رزرو", StringComparison.Ordinal)
            || text.Contains("آزادسازی", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "cart.inventory.stale");
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

    /// <summary>
    /// متن مشتری را از کد ماشین می‌سازد؛ واژگان Held/رزرو داخلی را به ویترین نمی‌برد.
    /// </summary>
    private static string MapCartCustomerDetail(string code) => code switch
    {
        "cart.inventory.insufficient" => "تعداد انتخاب‌شده بیشتر از موجودی قابل فروش است.",
        "cart.inventory.stale" => "موجودی این کالا تغییر کرده است. لطفاً تعداد را دوباره بررسی کنید.",
        "cart.quantity.invalid" => "تعداد انتخاب‌شده معتبر نیست.",
        "cart.offer.unavailable" => "این کالا در حال حاضر قابل افزودن به سبد نیست.",
        "cart.version.conflict" => "سبد هم‌زمان به‌روز شده است. صفحه را تازه کنید.",
        "cart.guest.invalid" => "دسترسی به سبد مهمان معتبر نیست.",
        "cart.missing" => "سبد پیدا نشد.",
        "cart.rejected" => "عملیات سبد انجام نشد. لطفاً دوباره تلاش کنید.",
        _ => "عملیات سبد انجام نشد. لطفاً دوباره تلاش کنید.",
    };
}
