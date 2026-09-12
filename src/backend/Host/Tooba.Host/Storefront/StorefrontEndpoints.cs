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
        group.MapGet("/brands", GetBrandsAsync);
        group.MapGet("/brands/{slug}", GetBrandAsync);
        group.MapGet("/sellers", GetSellersAsync);
        group.MapGet("/sellers/{publicId}", GetSellerAsync);
        group.MapGet("/merchandising/{kind}", GetMerchandisingAsync);
        group.MapGet("/products", GetListingAsync);
        group.MapGet("/products/{slug}", GetDetailAsync);
        group.MapGet("/category-plp/{slug}", GetCategoryPlpAsync);
        group.MapGet("/media/{assetId:guid}", GetPresentationMediaAsync);
        group.MapPost("/cart", CreateGuestCartAsync);
        group.MapGet("/cart/{cartId:guid}", GetCartAsync);
        group.MapPost("/cart/{cartId:guid}/lines", AddCartLineAsync);
        group.MapPatch("/cart/{cartId:guid}/lines/{lineId:guid}", ChangeCartLineAsync);
        group.MapDelete("/cart/{cartId:guid}/lines/{lineId:guid}", RemoveCartLineAsync);
        group.MapPost("/checkout/preview", PreviewCheckoutAsync);
        group.MapPost("/checkout", SubmitCheckoutAsync);
        group.MapGet("/checkout/{checkoutId:guid}", GetCheckoutAsync);
        group.MapPost("/shipping/projection", ProjectShippingAsync);
        group.MapPut("/shipping/selection", SaveShippingSelectionAsync);
        group.MapPost("/shipping/commit", CommitShippingAsync);
        group.MapGet("/geography/provinces", () => Results.Json(StorefrontIranGeography.Provinces));
        group.MapPost("/checkout/{checkoutId:guid}/payments", InitiatePaymentAsync);
        group.MapGet("/checkout/{checkoutId:guid}/wallet-quote", GetWalletQuoteAsync);
        group.MapGet("/payment-methods", ListPaymentMethodsAsync);
        group.MapGet("/payments/{paymentId:guid}", GetPaymentAsync);
        group.MapGet("/payments/{paymentId:guid}/sandbox", GetSandboxContextAsync);
        group.MapPost("/payments/{paymentId:guid}/sandbox/complete", CompleteSandboxPaymentAsync);
        group.MapPost("/payments/{paymentId:guid}/manual-evidence", SubmitManualEvidenceAsync);
        group.MapPost("/payments/{paymentId:guid}/manual-retry", RetryManualPaymentAsync);
        group.MapPost("/payments/{paymentId:guid}/unpaid-retry", RetryUnpaidPaymentAsync);
        group.MapPost("/payments/{paymentId:guid}/proof", UploadManualProofAsync).DisableAntiforgery();
    }

    private static async Task<IResult> GetHomeAsync(
        StorefrontComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
        => Results.Json(await composer.GetHomeAsync(locale, cancellationToken));

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
            q,
            categoryId,
            sellerPartyId,
            inStock,
            sort,
            page,
            pageSize,
            cancellationToken));

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

    /// <summary>
    /// PLP رده با مسیر slug. فیلترها از query: f_CODE=v1,v2 | r_CODE=min:max | b_CODE=true
    /// </summary>
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
            locale ?? "fa-IR",
            slug,
            filters,
            sort,
            page,
            pageSize,
            cancellationToken);
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
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

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

    /// <summary>
    /// تصویر نمایشی توسعه برای مرجع مات Media؛ در صورت وجود دارایی واقعی بایت‌ها سرو می‌شوند.
    /// </summary>
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
        string? couponCode = null,
        CancellationToken cancellationToken = default)
        => ExecuteCheckoutAsync(() => composer.PreviewAsync(
            cartId,
            ReadGuestSecret(request),
            couponCode ?? request.Query["couponCode"].FirstOrDefault(),
            cancellationToken));

    private static Task<IResult> ProjectShippingAsync(
        StorefrontShippingProjectionRequest body,
        StorefrontShippingComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteShippingAsync(() => composer.ProjectAsync(
            body.CartId,
            ReadGuestSecret(request),
            body.ProvinceName,
            body.MethodCode,
            body.Language,
            cancellationToken));

    private static Task<IResult> SaveShippingSelectionAsync(
        StorefrontShippingSelectionRequest body,
        StorefrontShippingComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteShippingAsync(() => composer.SaveSelectionAsync(body, ReadGuestSecret(request), cancellationToken));

    private static Task<IResult> CommitShippingAsync(
        StorefrontShippingCommitRequest body,
        StorefrontShippingComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteCheckoutAsync(() => composer.CommitAsync(
            body.CartId,
            ReadGuestSecret(request),
            body.ExpectedCartVersion,
            body.IdempotencyKey,
            body.CouponCode,
            cancellationToken));

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
            body.CouponCode,
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

    private static Task<IResult> InitiatePaymentAsync(
        Guid checkoutId,
        StorefrontInitiatePaymentRequest body,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecutePaymentAsync(() => composer.InitiateAsync(
            checkoutId,
            body.CartId,
            ReadGuestSecret(request),
            body.IdempotencyKey,
            body.WantsWallet,
            body.ProviderCode,
            cancellationToken));

    private static Task<IResult> GetWalletQuoteAsync(
        Guid checkoutId,
        Guid cartId,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecutePaymentAsync(() => composer.GetWalletQuoteAsync(
            checkoutId,
            cartId,
            ReadGuestSecret(request),
            cancellationToken));

    private static Task<IResult> ListPaymentMethodsAsync(
        StorefrontPaymentComposer composer)
        => Task.FromResult(Results.Json(composer.ListPaymentMethods()));

    private static async Task<IResult> GetPaymentAsync(
        Guid paymentId,
        Guid cartId,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        return await ExecutePaymentAsync(async () =>
        {
            var page = await composer.GetAsync(paymentId, cartId, ReadGuestSecret(request), cancellationToken);
            return page ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        });
    }

    private static Task<IResult> GetSandboxContextAsync(
        Guid paymentId,
        Guid cartId,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecutePaymentAsync(() => composer.GetSandboxContextAsync(
            paymentId,
            cartId,
            ReadGuestSecret(request),
            cancellationToken));

    private static Task<IResult> CompleteSandboxPaymentAsync(
        Guid paymentId,
        StorefrontSandboxPaymentRequest body,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecutePaymentAsync(() => composer.CompleteSandboxAsync(
            paymentId,
            body.CartId,
            ReadGuestSecret(request),
            body.AttemptId,
            body.ProviderRequestReference,
            body.Outcome,
            cancellationToken));

    private static Task<IResult> SubmitManualEvidenceAsync(
        Guid paymentId,
        StorefrontManualEvidenceRequest body,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecutePaymentAsync(() => composer.SubmitManualEvidenceAsync(
            paymentId,
            body.CartId,
            ReadGuestSecret(request),
            body.TransferReference,
            body.ProofMediaAssetId,
            cancellationToken));

    private static Task<IResult> RetryManualPaymentAsync(
        Guid paymentId,
        StorefrontPaymentCartRequest body,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecutePaymentAsync(() => composer.RetryManualAsync(
            paymentId,
            body.CartId,
            ReadGuestSecret(request),
            cancellationToken));

    private static Task<IResult> RetryUnpaidPaymentAsync(
        Guid paymentId,
        StorefrontPaymentCartRequest body,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecutePaymentAsync(() => composer.RetryUnpaidAsync(
            paymentId,
            body.CartId,
            ReadGuestSecret(request),
            cancellationToken));

    private static async Task<IResult> UploadManualProofAsync(
        Guid paymentId,
        Guid cartId,
        StorefrontPaymentComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!request.HasFormContentType)
            {
                return Results.Json(
                    new { title = "Bad Request", errorCode = "payment.proof.required", detail = "فایل مدرک پرداخت لازم است." },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null)
            {
                return Results.Json(
                    new { title = "Bad Request", errorCode = "payment.proof.required", detail = "فایل مدرک پرداخت لازم است." },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            await using var stream = file.OpenReadStream();
            var mediaAssetId = await composer.UploadManualProofAsync(
                paymentId,
                cartId,
                ReadGuestSecret(request),
                stream,
                file.FileName,
                file.ContentType ?? string.Empty,
                cancellationToken);
            return Results.Json(new { mediaAssetId });
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapPaymentException(exception);
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapPaymentCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
        }
        catch (Tooba.BuildingBlocks.PlatformHttpException platform)
        {
            return Results.Json(
                new { title = platform.Title, errorCode = platform.ErrorCode, detail = platform.Title },
                statusCode: platform.StatusCode);
        }
    }

    private static async Task<IResult> ExecutePaymentAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Json(await action());
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapPaymentException(exception);
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapPaymentCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
        }
    }

    private static (int Status, string Title, string Code) MapPaymentException(InvalidOperationException exception)
    {
        var text = exception.Message;
        if (text.Contains("قبلاً پرداخت", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "payment.already-paid");
        }

        if (text.Contains("پیدا نشد", StringComparison.Ordinal))
        {
            return (StatusCodes.Status404NotFound, "Not Found", "payment.missing");
        }

        if (text.Contains("checkout.access.denied", StringComparison.Ordinal)
            || text.Contains("payment.access.denied", StringComparison.Ordinal))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden", "payment.access.denied");
        }

        if (text.Contains("دسترسی", StringComparison.Ordinal) || text.Contains("راز", StringComparison.Ordinal))
        {
            return (StatusCodes.Status401Unauthorized, "Unauthorized", "payment.guest.invalid");
        }

        if (text.Contains("پرداخت ترکیبی", StringComparison.Ordinal)
            || text.Contains("موجودی کیف پول کافی نیست", StringComparison.Ordinal)
            || text.Contains("موجودی باید کل مبلغ", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "payment.wallet.mixed_deferred");
        }

        if (text.Contains("payment.method.unavailable", StringComparison.Ordinal)
            || text.Contains("کارت به کارت در این فروشگاه فعال نیست", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "payment.method.unavailable");
        }

        if (text.Contains("شماره پیگیری پرداخت الزامی است", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "payment.tracking.required");
        }

        if (text.Contains("payment.proof.required", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "payment.proof.required");
        }

        if (text.Contains("payment.proof.foreign", StringComparison.Ordinal))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden", "payment.proof.foreign");
        }

        if (text.Contains("payment.sandbox.unavailable", StringComparison.Ordinal))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden", "payment.sandbox.unavailable");
        }

        if (text.Contains("این سفارش در حال حاضر قابل تأمین نیست.", StringComparison.Ordinal)
            || text.Contains("inventory.supply.unavailable", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "payment.unpaid.supply_unavailable");
        }

        if (text.Contains("payment.unpaid.retry.invalid_state", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "payment.unpaid.retry.invalid");
        }

        return (StatusCodes.Status400BadRequest, "Bad Request", "payment.rejected");
    }

    private static string MapPaymentCustomerDetail(string code) => code switch
    {
        "payment.already-paid" => "این سفارش قبلاً پرداخت شده است.",
        "payment.missing" => "پرداخت پیدا نشد.",
        "payment.guest.invalid" => "دسترسی به پرداخت معتبر نیست.",
        "payment.access.denied" =>
            "دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید.",
        "payment.wallet.mixed_deferred" => "پرداخت ترکیبی کیف پول هنوز فعال نیست؛ موجودی باید کل مبلغ را پوشش دهد.",
        "payment.method.unavailable" => "این روش پرداخت برای فروشگاه فعال نیست.",
        "payment.tracking.required" => "شماره پیگیری پرداخت الزامی است.",
        "payment.proof.required" => "بارگذاری مدرک پرداخت الزامی است.",
        "payment.proof.foreign" => "مدرک پرداخت معتبر نیست.",
        "payment.sandbox.unavailable" => "درگاه آزمایشی در این محیط در دسترس نیست.",
        "payment.unpaid.supply_unavailable" => "این سفارش در حال حاضر قابل تأمین نیست.",
        "payment.unpaid.retry.invalid" => "مهلت پرداخت این سفارش به پایان رسیده است.",
        _ => "امکان شروع پرداخت در حال حاضر وجود ندارد.",
    };

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

    private static async Task<IResult> ExecuteShippingAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Json(await action());
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapShippingException(exception);
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapShippingCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
        }
    }

    private static (int Status, string Title, string Code) MapShippingException(InvalidOperationException exception)
    {
        var text = exception.Message;
        if (text.Contains("shipping.cart.forbidden", StringComparison.Ordinal)
            || text.Contains("متعلق", StringComparison.Ordinal)
            || text.Contains("دفترچه", StringComparison.Ordinal))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden", "shipping.address.forbidden");
        }

        if (text.Contains("shipping.cart.missing", StringComparison.Ordinal)
            || text.Contains("پیدا نشد", StringComparison.Ordinal))
        {
            return (StatusCodes.Status404NotFound, "Not Found", "shipping.cart.missing");
        }

        if (text.Contains("shipping.cart.empty", StringComparison.Ordinal) || text.Contains("خالی", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.cart.empty");
        }

        if (text.Contains("shipping.cart.stale", StringComparison.Ordinal) || text.Contains("کهنه", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "shipping.cart.stale");
        }

        if (text.Contains("shipping.method.unavailable", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.method.unavailable");
        }

        if (text.Contains("shipping.delivery.too_early", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.delivery.too_early");
        }

        if (text.Contains("shipping.delivery.slot_unavailable", StringComparison.Ordinal)
            || text.Contains("shipping.delivery.invalid", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.delivery.slot_unavailable");
        }

        if (text.Contains("shipping.note.too_long", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.note.too_long");
        }

        if (text.Contains("shipping.selection.required", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.selection.required");
        }

        return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.rejected");
    }

    private static string MapShippingCustomerDetail(string code) => code switch
    {
        "shipping.address.forbidden" => "نشانی انتخاب‌شده متعلق به این مشتری نیست.",
        "shipping.cart.missing" => "سبد خرید پیدا نشد.",
        "shipping.cart.empty" => "سبد خرید خالی است.",
        "shipping.cart.stale" => "سبد خرید تغییر کرده است؛ صفحه را تازه کنید.",
        "shipping.method.unavailable" => "روش ارسال انتخاب‌شده در دسترس نیست.",
        "shipping.delivery.too_early" => "تاریخ تحویل نمی‌تواند زودتر از حداقل زمان آماده‌سازی باشد.",
        "shipping.delivery.slot_unavailable" => "بازهٔ زمانی تحویل دیگر در دسترس نیست.",
        "shipping.note.too_long" => "توضیحات سفارش بیش از حد طولانی است.",
        "shipping.selection.required" => "ابتدا اطلاعات ارسال را تکمیل کنید.",
        _ => "امکان ادامهٔ مرحلهٔ ارسال وجود ندارد.",
    };

    private static (int Status, string Title, string Code) MapCheckoutException(InvalidOperationException exception)
    {
        var text = exception.Message;
        if (text.Contains("متعلق", StringComparison.Ordinal) || text.Contains("دفترچه", StringComparison.Ordinal))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden", "checkout.address.forbidden");
        }

        if (text.Contains("checkout.access.denied", StringComparison.Ordinal)
            || text.Contains("راز مهمان", StringComparison.Ordinal))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden", "checkout.access.denied");
        }

        if (text.Contains("پیدا نشد", StringComparison.Ordinal))
        {
            return (StatusCodes.Status404NotFound, "Not Found", "checkout.missing");
        }

        if (text.Contains("inventory.supply.unavailable", StringComparison.Ordinal)
            || text.Contains("قابل تأمین نیست", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.inventory.unavailable");
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
        "checkout.inventory.unavailable" => "موجودی یکی از کالاها برای ثبت سفارش کافی نیست.",
        "checkout.price.changed" => "قیمت یکی از کالاها تغییر کرده؛ لطفاً سفارش را دوباره بررسی کنید.",
        "checkout.tax.unavailable" => "محاسبهٔ مالیات این سفارش الان ممکن نیست. لطفاً دوباره تلاش کنید.",
        "checkout.cart.expired" => "سبد خرید منقضی شده است.",
        "checkout.shipping.incomplete" => "اطلاعات ارسال کامل نیست.",
        "checkout.cart.empty" => "سبد خرید خالی است.",
        "checkout.version.conflict" => "سبد هم‌زمان به‌روز شده است. صفحه را تازه کنید.",
        "checkout.missing" => "سفارش پیدا نشد.",
        "checkout.address.forbidden" => "این نشانی متعلق به مشتری جاری نیست.",
        "checkout.access.denied" =>
            "دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید.",
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
