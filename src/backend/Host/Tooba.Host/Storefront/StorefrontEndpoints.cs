using Tooba.Host.Storefront;
using Tooba.Order.Application;

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
        group.MapPost("/pending-payments", ListPendingPaymentsAsync);
        group.MapPost("/checkout/{checkoutId:guid}/cancel", CancelPendingCheckoutAsync);
        group.MapPost("/checkout/{checkoutId:guid}/hide-pending-card", HidePendingCardAsync);
        group.MapPost("/checkout/preview", PreviewCheckoutAsync);
        group.MapPost("/checkout", SubmitCheckoutAsync);
        group.MapGet("/checkout/{checkoutId:guid}", GetCheckoutAsync);
        group.MapPost("/shipping/projection", ProjectShippingAsync);
        group.MapPut("/shipping/selection", SaveShippingSelectionAsync);
        group.MapPost("/shipping/commit", CommitShippingAsync);
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

    private static async Task<IResult> ListPendingPaymentsAsync(
        StorefrontPendingPaymentQueryRequest? body,
        StorefrontPendingPaymentComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await composer.ListAsync(body, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapPendingCardException(exception);
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapPendingCardCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
        }
    }

    private static async Task<IResult> CancelPendingCheckoutAsync(
        Guid checkoutId,
        HttpRequest request,
        StorefrontPendingPaymentComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await composer.CancelAsync(
                checkoutId,
                await ReadOptionalCartIdAsync(request, cancellationToken),
                ReadGuestSecret(request),
                cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapPendingCardException(exception);
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapPendingCardCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
        }
    }

    private static async Task<IResult> HidePendingCardAsync(
        Guid checkoutId,
        HttpRequest request,
        StorefrontPendingPaymentComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await composer.HidePendingCardAsync(
                checkoutId,
                await ReadOptionalCartIdAsync(request, cancellationToken),
                ReadGuestSecret(request),
                cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            var mapped = MapPendingCardException(exception);
            return Results.Json(
                new { title = mapped.Title, errorCode = mapped.Code, detail = MapPendingCardCustomerDetail(mapped.Code) },
                statusCode: mapped.Status);
        }
    }

    private static async Task<Guid> ReadOptionalCartIdAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.Query.TryGetValue("cartId", out var query)
            && Guid.TryParse(query.ToString(), out var fromQuery)
            && fromQuery != Guid.Empty)
        {
            return fromQuery;
        }

        try
        {
            if (!request.HasJsonContentType() || request.ContentLength is 0)
            {
                return Guid.Empty;
            }

            var body = await request.ReadFromJsonAsync<StorefrontPaymentCartRequest>(cancellationToken);
            return body?.CartId ?? Guid.Empty;
        }
        catch (BadHttpRequestException)
        {
            return Guid.Empty;
        }
        catch (System.Text.Json.JsonException)
        {
            return Guid.Empty;
        }
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

    private static Task<IResult> PreviewCheckoutAsync(
        Guid cartId,
        StorefrontCheckoutComposer composer,
        CheckoutIdentityGate gate,
        HttpRequest request,
        string? couponCode = null,
        CancellationToken cancellationToken = default)
        => ExecuteCheckoutAsync(async () =>
        {
            await gate.EnsureCheckoutActorAsync(cancellationToken);
            return await composer.PreviewAsync(
                cartId,
                ReadGuestSecret(request),
                couponCode ?? request.Query["couponCode"].FirstOrDefault(),
                cancellationToken);
        });

    private static Task<IResult> ProjectShippingAsync(
        StorefrontShippingProjectionRequest body,
        StorefrontShippingComposer composer,
        CheckoutIdentityGate gate,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteShippingAsync(async () =>
        {
            await gate.EnsureCheckoutActorAsync(cancellationToken);
            return await composer.ProjectAsync(
                body.CartId,
                ReadGuestSecret(request),
                body.ProvinceName,
                body.MethodCode,
                body.Language,
                cancellationToken);
        });

    private static Task<IResult> SaveShippingSelectionAsync(
        StorefrontShippingSelectionRequest body,
        StorefrontShippingComposer composer,
        CheckoutIdentityGate gate,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteShippingAsync(async () =>
        {
            await gate.EnsureCheckoutActorAsync(cancellationToken);
            return await composer.SaveSelectionAsync(body, ReadGuestSecret(request), cancellationToken);
        });

    private static Task<IResult> CommitShippingAsync(
        StorefrontShippingCommitRequest body,
        StorefrontShippingComposer composer,
        CheckoutIdentityGate gate,
        ICheckoutAbuseGate abuse,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteCheckoutAsync(
            async () =>
            {
                await gate.EnsureCheckoutActorAsync(cancellationToken);
                return await composer.CommitAsync(
                    body.CartId,
                    ReadGuestSecret(request),
                    body.ExpectedCartVersion,
                    body.IdempotencyKey,
                    body.CouponCode,
                    cancellationToken);
            },
            abuse,
            cancellationToken);

    private static Task<IResult> SubmitCheckoutAsync(
        StorefrontSubmitCheckoutRequest body,
        StorefrontCheckoutComposer composer,
        CheckoutIdentityGate gate,
        ICheckoutAbuseGate abuse,
        HttpRequest request,
        CancellationToken cancellationToken)
        => ExecuteCheckoutAsync(
            async () =>
            {
                await gate.EnsureCheckoutActorAsync(cancellationToken);
                return await composer.SubmitAsync(
                    body.CartId,
                    ReadGuestSecret(request),
                    body.ExpectedCartVersion,
                    body.IdempotencyKey,
                    body.Shipping,
                    body.CouponCode,
                    cancellationToken);
            },
            abuse,
            cancellationToken);

    private static async Task<IResult> GetCheckoutAsync(
        Guid checkoutId,
        Guid cartId,
        StorefrontCheckoutComposer composer,
        Tooba.Payment.Application.Ports.IPaymentDirectory payments,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        return await ExecuteCheckoutAsync(async () =>
        {
            var page = await composer.GetAsync(checkoutId, cartId, ReadGuestSecret(request), cancellationToken)
                ?? throw new InvalidOperationException("سفارش پیدا نشد.");
            if (page.CheckoutId is Guid id
                && await payments.HasSucceededPaymentForCheckoutAsync(id, cancellationToken))
            {
                return page with { PaymentState = "Paid", CanInitiatePayment = false };
            }

            return page;
        });
    }

    private static (int Status, string Title, string Code) MapPendingCardException(InvalidOperationException exception)
    {
        var text = exception.Message;
        if (text.Contains("order.cancel.unpaid_only", StringComparison.Ordinal))
            return (StatusCodes.Status409Conflict, "Conflict", "order.cancel.unpaid_only");
        if (text.Contains("order.cancel.forbidden", StringComparison.Ordinal)
            || text.Contains("پس از ارسال کالا", StringComparison.Ordinal))
            return (StatusCodes.Status409Conflict, "Conflict", "order.cancel.forbidden");
        if (text.Contains("pending.hide.active_hold", StringComparison.Ordinal))
            return (StatusCodes.Status409Conflict, "Conflict", "pending.hide.active_hold");
        if (text.Contains("checkout.authentication_required", StringComparison.Ordinal))
            return (StatusCodes.Status401Unauthorized, "Unauthorized", "checkout.authentication_required");
        if (text.Contains("پیدا نشد", StringComparison.Ordinal))
            return (StatusCodes.Status404NotFound, "Not Found", "payment.missing");
        return (StatusCodes.Status400BadRequest, "Bad Request", "payment.rejected");
    }

    private static string MapPendingCardCustomerDetail(string code) => code switch
    {
        "order.cancel.unpaid_only" => "لغو این سفارش از سبد فقط قبل از پرداخت موفق امکان‌پذیر است.",
        "order.cancel.forbidden" => "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.",
        "pending.hide.active_hold" => "تا پایان مهلت رزرو نمی‌توان این کارت را پنهان کرد.",
        "checkout.authentication_required" => "برای ادامه فرایند خرید وارد حساب خود شوید.",
        "payment.missing" => "پرداخت پیدا نشد.",
        _ => "امکان انجام این عملیات در حال حاضر وجود ندارد.",
    };

    private static async Task<IResult> ExecuteCheckoutAsync(
        Func<Task<StorefrontCheckoutPage>> action,
        ICheckoutAbuseGate? abuse = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Results.Json(await action());
        }
        catch (CheckoutAbuseLimitException exception)
        {
            if (abuse is not null)
            {
                await abuse.RecordBlockAsync(exception, cancellationToken);
            }

            return Results.Json(
                new
                {
                    title = "Conflict",
                    errorCode = exception.ErrorCode,
                    detail = MapCheckoutCustomerDetail(exception.ErrorCode),
                    currentCount = exception.CurrentCount,
                    maxCount = exception.MaxCount,
                    nextAvailableAt = exception.NextAvailableAt,
                },
                statusCode: StatusCodes.Status409Conflict);
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
        if (text.Contains("checkout.authentication_required", StringComparison.Ordinal))
        {
            return (StatusCodes.Status401Unauthorized, "Unauthorized", "checkout.authentication_required");
        }

        if (text.Contains("checkout.open_unpaid_limit_reached", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.open_unpaid_limit_reached");
        }

        if (text.Contains("checkout.reservation_commit_limit_reached", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.reservation_commit_limit_reached");
        }

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

        if (text.Contains("shipping.firstname.required", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.firstname.required");
        }

        if (text.Contains("shipping.lastname.required", StringComparison.Ordinal))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "shipping.lastname.required");
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
        "shipping.firstname.required" => "نام الزامی است.",
        "shipping.lastname.required" => "نام خانوادگی الزامی است.",
        "checkout.authentication_required" => "برای ادامه فرایند خرید وارد حساب خود شوید.",
        "checkout.open_unpaid_limit_reached" =>
            "شما به حداکثر تعداد سفارش‌های در انتظار پرداخت رسیده‌اید. ابتدا یکی از سفارش‌های قبلی را پرداخت یا لغو کنید.",
        "checkout.reservation_commit_limit_reached" =>
            "تعداد دفعات مجاز شروع رزرو در بازه زمانی اخیر به پایان رسیده است. کمی بعد دوباره تلاش کنید.",
        _ => "امکان ادامهٔ مرحلهٔ ارسال وجود ندارد.",
    };

    private static (int Status, string Title, string Code) MapCheckoutException(InvalidOperationException exception)
    {
        var text = exception.Message;
        if (text.Contains("متعلق", StringComparison.Ordinal) || text.Contains("دفترچه", StringComparison.Ordinal))
        {
            return (StatusCodes.Status403Forbidden, "Forbidden", "checkout.address.forbidden");
        }

        if (text.Contains("checkout.authentication_required", StringComparison.Ordinal))
        {
            return (StatusCodes.Status401Unauthorized, "Unauthorized", "checkout.authentication_required");
        }

        if (text.Contains("checkout.open_unpaid_limit_reached", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.open_unpaid_limit_reached");
        }

        if (text.Contains("checkout.reservation_commit_limit_reached", StringComparison.Ordinal))
        {
            return (StatusCodes.Status409Conflict, "Conflict", "checkout.reservation_commit_limit_reached");
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
        "checkout.authentication_required" => "برای ادامه فرایند خرید وارد حساب خود شوید.",
        "checkout.open_unpaid_limit_reached" =>
            "شما به حداکثر تعداد سفارش‌های در انتظار پرداخت رسیده‌اید. ابتدا یکی از سفارش‌های قبلی را پرداخت یا لغو کنید.",
        "checkout.reservation_commit_limit_reached" =>
            "تعداد دفعات مجاز شروع رزرو در بازه زمانی اخیر به پایان رسیده است. کمی بعد دوباره تلاش کنید.",
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
}
