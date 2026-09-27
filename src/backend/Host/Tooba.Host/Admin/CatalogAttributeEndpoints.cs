using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای Admin برای ویژگی محصول و محورهای Variant (schema رده در Catalog.Endpoints).
/// ماتریس تنوع در همین گروه ثبت می‌شود؛ قیمت/موجودی اینجا نیستند.
/// </summary>
public static class CatalogAttributeEndpoints
{
    /// <summary>
    /// مسیرهای Admin Attribute محصول/Variant را ثبت می‌کند.
    /// </summary>
    public static void MapCatalogAttributeEndpoints(this WebApplication app)
    {
        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}");
        products.AddEndpointFilter(CatalogActorHttpBinding.BindAsync);
        products.MapGet("/attributes", GetProductAttributeEditorStateAsync);
        products.MapPut("/attributes", SetProductAttributesAsync);
        products.MapGet("/attributes/readiness", GetProductAttributeReadinessAsync);
        products.MapPut("/attributes/{definitionId:guid}", SetProductAttributeAsync);
        products.MapPut("/variant-axes", SetProductVariantAxesAsync);
        products.MapGet("/variants/editor", GetProductVariantEditorStateAsync);
        products.MapPost("/variants/preview", PreviewProductVariantsAsync);
        products.MapPut("/variants/apply", ApplyProductVariantsAsync);
        products.MapGet("/variants/readiness", GetProductVariantReadinessAsync);
        products.MapPost("/category-change-preview", PreviewCategoryChangeAsync);
        products.MapPut("/primary-category", ReplacePrimaryCategoryAsync);
    }

    private static async Task<IResult> GetProductAttributeEditorStateAsync(
        Guid productId,
        string? locale,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var state = await catalog.GetProductAttributeEditorStateAsync(
                productId,
                string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale,
                cancellationToken);
            return Results.Json(state);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.attribute.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SetProductAttributesAsync(
        Guid productId,
        SetProductAttributesRequest body,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var inputs = (body.Values ?? [])
                .Select(v => new ProductAttributeValueInput(
                    v.DefinitionId,
                    v.RawValue,
                    v.EnumOptionId,
                    v.Clear))
                .ToList();
            await catalog.SetProductAttributesAsync(productId, inputs, cancellationToken);
            var locale = string.IsNullOrWhiteSpace(body.Locale) ? "fa-IR" : body.Locale.Trim();
            return Results.Json(await catalog.GetProductAttributeEditorStateAsync(productId, locale, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.attribute.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetProductAttributeReadinessAsync(
        Guid productId,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await catalog.GetProductAttributeReadinessAsync(productId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.attribute.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SetProductAttributeAsync(
        Guid productId,
        Guid definitionId,
        SetProductAttributeRequest body,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await catalog.SetProductAttributeAsync(
                productId,
                definitionId,
                body.RawValue,
                body.EnumOptionId,
                cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.attribute.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SetProductVariantAxesAsync(
        Guid productId,
        SetProductVariantAxesRequest body,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            await catalog.SetProductVariantAxesAsync(
                productId,
                body.OrderedDefinitionIds ?? [],
                cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.variant_axes.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetProductVariantEditorStateAsync(
        Guid productId,
        string? locale,
        ICatalogDirectory catalog,
        IOfferLookupGateway offers,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var normalized = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
            var state = await catalog.GetProductVariantEditorStateAsync(productId, normalized, cancellationToken);
            var enriched = await EnrichVariantEditorWithOfferCountsAsync(state, offers, cancellationToken);
            return Results.Json(enriched);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.variant.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> PreviewProductVariantsAsync(
        Guid productId,
        ProductVariantPreviewRequest body,
        ICatalogDirectory catalog,
        IOfferLookupGateway offers,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var locale = string.IsNullOrWhiteSpace(body.Locale) ? "fa-IR" : body.Locale.Trim();
            var axes = (body.SelectedAxes ?? [])
                .Select(a => new ProductVariantSelectedAxisInput(a.DefinitionId, a.OptionIds ?? []))
                .ToList();
            var preview = await catalog.PreviewProductVariantCombinationsAsync(productId, axes, locale, cancellationToken);
            var variantIds = preview.Combinations
                .Where(c => c.ExistingVariantId is Guid)
                .Select(c => c.ExistingVariantId!.Value)
                .Distinct()
                .ToArray();
            var counts = await offers.CountOffersByCatalogVariantIdsAsync(variantIds, cancellationToken);
            var combinations = preview.Combinations.Select(c =>
            {
                bool? referenced = null;
                if (c.ExistingVariantId is Guid id && counts.TryGetValue(id, out var count))
                {
                    referenced = count > 0;
                }

                return c with { ReferencedByOffers = referenced };
            }).ToList();
            return Results.Json(preview with { Combinations = combinations });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.variant.preview.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ApplyProductVariantsAsync(
        Guid productId,
        ProductVariantApplyRequest body,
        ICatalogDirectory catalog,
        IOfferLookupGateway offers,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            CatalogPublicationStatus? ParseStatus(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                return Enum.TryParse<CatalogPublicationStatus>(raw.Trim(), true, out var status)
                    ? status
                    : throw new InvalidOperationException("وضعیت تنوع نامعتبر است.");
            }

            var patches = (body.VariantPatches ?? [])
                .Select(p => new ProductVariantPatchInput(
                    p.VariantId,
                    ParseStatus(p.Status),
                    p.CatalogCodeSeam,
                    p.SortOrder,
                    p.IsDefault))
                .ToList();
            var input = new ProductVariantApplyInput(
                body.Locale,
                (body.SelectedAxes ?? [])
                    .Select(a => new ProductVariantSelectedAxisInput(a.DefinitionId, a.OptionIds ?? []))
                    .ToList(),
                body.DefaultVariantId,
                patches);
            var result = await catalog.ApplyProductVariantMatrixAsync(productId, input, cancellationToken);
            var counts = await offers.CountOffersByCatalogVariantIdsAsync(
                result.Variants.Select(v => v.VariantId).ToArray(),
                cancellationToken);
            var variants = result.Variants
                .Select(v => v with { OfferCount = counts.GetValueOrDefault(v.VariantId) })
                .ToList();
            return Results.Json(result with { Variants = variants });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.variant.apply.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetProductVariantReadinessAsync(
        Guid productId,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await catalog.GetProductVariantReadinessAsync(productId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.variant.readiness.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<ProductVariantEditorState> EnrichVariantEditorWithOfferCountsAsync(
        ProductVariantEditorState state,
        IOfferLookupGateway offers,
        CancellationToken cancellationToken)
    {
        var counts = await offers.CountOffersByCatalogVariantIdsAsync(
            state.Variants.Select(v => v.VariantId).ToArray(),
            cancellationToken);
        var variants = state.Variants
            .Select(v => v with { OfferCount = counts.GetValueOrDefault(v.VariantId) })
            .ToList();
        return state with { Variants = variants };
    }

    private static async Task<IResult> PreviewCategoryChangeAsync(
        Guid productId,
        CategoryChangePreviewRequest body,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var locale = string.IsNullOrWhiteSpace(body.Locale) ? "fa-IR" : body.Locale.Trim();
            return Results.Json(await catalog.PreviewCategoryChangeReportAsync(
                productId,
                body.NewCategoryId,
                locale,
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryChangeInvalid(ex);
        }
    }

    private static async Task<IResult> ReplacePrimaryCategoryAsync(
        Guid productId,
        CategoryChangeRequest body,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await catalog.ReplaceProductPrimaryCategoryAsync(productId, body.NewCategoryId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryChangeInvalid(ex);
        }
    }

    private static IResult MapCategoryChangeInvalid(InvalidOperationException ex)
    {
        var errorCode = string.Equals(
            ex.Message,
            CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa,
            StringComparison.Ordinal)
            ? CatalogCategoryTreeRules.AssignmentLevelInvalidErrorCode
            : "catalog.category_change.invalid";
        return Results.Json(
            new { title = ex.Message, errorCode },
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static IResult MapAttributeInvalid(InvalidOperationException ex)
    {
        if (ex.Message == "catalog.attribute.code.duplicate"
            || (ex.Message.Contains("کد", StringComparison.Ordinal)
                && ex.Message.Contains("تکراری", StringComparison.Ordinal)))
        {
            return Results.Json(
                new
                {
                    title = "این کد ویژگی قبلاً استفاده شده است.",
                    errorCode = "catalog.attribute.code.duplicate",
                },
                statusCode: StatusCodes.Status409Conflict);
        }

        if (ex.Message == "catalog.attribute.name.duplicate"
            || (ex.Message.Contains("نام", StringComparison.Ordinal)
                && ex.Message.Contains("تکراری", StringComparison.Ordinal)))
        {
            return Results.Json(
                new
                {
                    title = "ویژگی‌ای با این نام قبلاً وجود دارد.",
                    errorCode = "catalog.attribute.name.duplicate",
                },
                statusCode: StatusCodes.Status409Conflict);
        }

        if (ex.Message == "catalog.attribute.missing")
        {
            return Results.Json(
                new { title = "تعریف ویژگی پیدا نشد.", errorCode = "catalog.attribute.missing" },
                statusCode: StatusCodes.Status404NotFound);
        }

        if (ex.Message == "catalog.attribute.variant_axis.value_kind.invalid")
        {
            return Results.Json(
                new
                {
                    title = "این نوع ویژگی برای ساخت تنوع مناسب نیست.",
                    errorCode = "catalog.attribute.variant_axis.value_kind.invalid",
                },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (ex.Message == "catalog.attribute.variant_axis.capability_disabled")
        {
            return Results.Json(
                new
                {
                    title = "امکان استفاده از این ویژگی برای تنوع در تعریف اصلی آن فعال نشده است.",
                    errorCode = "catalog.attribute.variant_axis.capability_disabled",
                },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (ex.Message == "catalog.attribute.variant_axis.in_use")
        {
            return Results.Json(
                new
                {
                    title = "این ویژگی در تنوع‌های فعال استفاده می‌شود.",
                    errorCode = "catalog.attribute.variant_axis.in_use",
                },
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Json(
            new { title = ex.Message, errorCode = "catalog.attribute.invalid" },
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}

/// <summary>بدنهٔ مقدار ویژگی محصول.</summary>
public sealed record SetProductAttributeRequest(string RawValue, Guid? EnumOptionId);

/// <summary>یک ردیف مقدار ویژگی برای ذخیرهٔ دسته‌ای.</summary>
public sealed record ProductAttributeValueRequest(
    Guid DefinitionId,
    string? RawValue,
    Guid? EnumOptionId,
    bool Clear);

/// <summary>بدنهٔ ذخیرهٔ دسته‌ای ویژگی‌های محصول.</summary>
public sealed record SetProductAttributesRequest(
    string? Locale,
    List<ProductAttributeValueRequest>? Values);

/// <summary>بدنهٔ محورهای Variant محصول.</summary>
public sealed record SetProductVariantAxesRequest(List<Guid>? OrderedDefinitionIds);

/// <summary>محور انتخاب‌شده برای پیش‌نمایش/اعمال ماتریس تنوع.</summary>
public sealed record ProductVariantSelectedAxisRequest(Guid DefinitionId, List<Guid>? OptionIds);

/// <summary>بدنهٔ پیش‌نمایش ماتریس تنوع.</summary>
public sealed record ProductVariantPreviewRequest(
    string? Locale,
    List<ProductVariantSelectedAxisRequest>? SelectedAxes);

/// <summary>پچ تنوع هنگام اعمال ماتریس.</summary>
public sealed record ProductVariantPatchRequest(
    Guid VariantId,
    string? Status,
    string? CatalogCodeSeam,
    int? SortOrder,
    bool? IsDefault);

/// <summary>بدنهٔ اعمال ماتریس تنوع.</summary>
public sealed record ProductVariantApplyRequest(
    string? Locale,
    List<ProductVariantSelectedAxisRequest>? SelectedAxes,
    Guid? DefaultVariantId,
    List<ProductVariantPatchRequest>? VariantPatches);

/// <summary>بدنهٔ تغییر رده.</summary>
public sealed record CategoryChangeRequest(Guid NewCategoryId);

/// <summary>بدنهٔ پیش‌نمایش تغییر رده با locale برای برچسب‌ها.</summary>
public sealed record CategoryChangePreviewRequest(Guid NewCategoryId, string? Locale);
