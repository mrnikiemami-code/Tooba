using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای Admin برای schema ویژگی Catalog و محورهای Variant محصول.
/// ماتریس کامل ترکیبی و قیمت/موجودی اینجا نیستند.
/// </summary>
public static class CatalogAttributeEndpoints
{
    /// <summary>
    /// مسیرهای Admin Attribute Schema را ثبت می‌کند.
    /// </summary>
    public static void MapCatalogAttributeEndpoints(this WebApplication app)
    {
        var defs = app.MapGroup("/v1/admin/catalog/attribute-definitions");
        defs.MapGet("/", ListDefinitionsAsync);
        defs.MapGet("/{definitionId:guid}", GetDefinitionAsync);
        defs.MapPost("/", CreateDefinitionAsync);
        defs.MapPatch("/{definitionId:guid}", UpdateDefinitionAsync);
        defs.MapPost("/{definitionId:guid}/options", AddOptionAsync);

        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/attribute-schema");
        categories.MapGet("/effective", GetEffectiveSchemaAsync);
        categories.MapPost("/bindings", BindAsync);
        categories.MapDelete("/bindings/{definitionId:guid}", UnbindAsync);
        categories.MapPut("/bindings/order", ReorderBindingsAsync);

        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}");
        products.MapPut("/attributes/{definitionId:guid}", SetProductAttributeAsync);
        products.MapPut("/variant-axes", SetProductVariantAxesAsync);
        products.MapPost("/category-change-preview", PreviewCategoryChangeAsync);
        products.MapPut("/primary-category", ReplacePrimaryCategoryAsync);
    }

    private static async Task<IResult> ListDefinitionsAsync(
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
            return Results.Json(await catalog.ListAttributeDefinitionsAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetDefinitionAsync(
        Guid definitionId,
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
            var view = await catalog.GetAttributeDefinitionAsync(definitionId, cancellationToken);
            return view is null
                ? Results.Json(new { title = "Not Found", errorCode = "catalog.attribute.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(view);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> CreateDefinitionAsync(
        CreateAttributeDefinitionRequest body,
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
            var id = await catalog.CreateAttributeDefinitionAsync(
                body.Code,
                body.ValueKind,
                body.IsVariantAxisAllowed,
                body.LocalizedNames ?? new Dictionary<string, string>(),
                cancellationToken);
            if (body.Metadata is { } meta)
            {
                await catalog.UpdateAttributeDefinitionAsync(
                    id,
                    meta.Unit,
                    meta.IsRequired,
                    meta.IsFilterable,
                    meta.IsComparable,
                    meta.IsMultivalue,
                    meta.DisplayOrder,
                    meta.ValidationMin,
                    meta.ValidationMax,
                    meta.ValidationMaxLength,
                    meta.IsActive,
                    cancellationToken);
            }

            return Results.Json(new { definitionId = id }, statusCode: StatusCodes.Status201Created);
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

    private static async Task<IResult> UpdateDefinitionAsync(
        Guid definitionId,
        UpdateAttributeDefinitionRequest body,
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
            await catalog.UpdateAttributeDefinitionAsync(
                definitionId,
                body.Unit,
                body.IsRequired,
                body.IsFilterable,
                body.IsComparable,
                body.IsMultivalue,
                body.DisplayOrder,
                body.ValidationMin,
                body.ValidationMax,
                body.ValidationMaxLength,
                body.IsActive,
                cancellationToken);
            return Results.Json(await catalog.GetAttributeDefinitionAsync(definitionId, cancellationToken));
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

    private static async Task<IResult> AddOptionAsync(
        Guid definitionId,
        AddAttributeOptionRequest body,
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
            var optionId = await catalog.AddAttributeOptionAsync(
                definitionId,
                body.Code,
                body.LocalizedNames ?? new Dictionary<string, string>(),
                cancellationToken);
            return Results.Json(new { optionId }, statusCode: StatusCodes.Status201Created);
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

    private static async Task<IResult> GetEffectiveSchemaAsync(
        Guid categoryId,
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
            return Results.Json(await catalog.GetEffectiveCategorySchemaAsync(categoryId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.schema.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> BindAsync(
        Guid categoryId,
        BindCategoryAttributeRequest body,
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
            await catalog.BindCategoryAttributeAsync(
                categoryId,
                body.DefinitionId,
                body.DisplayOrder,
                body.IsRequiredOverride,
                cancellationToken);
            return Results.Json(new { ok = true }, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.schema.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UnbindAsync(
        Guid categoryId,
        Guid definitionId,
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
            await catalog.UnbindCategoryAttributeAsync(categoryId, definitionId, cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.schema.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ReorderBindingsAsync(
        Guid categoryId,
        ReorderCategoryBindingsRequest body,
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
            await catalog.ReorderCategoryAttributeBindingsAsync(
                categoryId,
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
            return Results.Json(new { title = ex.Message, errorCode = "catalog.schema.invalid" }, statusCode: StatusCodes.Status400BadRequest);
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

    private static async Task<IResult> PreviewCategoryChangeAsync(
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
            return Results.Json(await catalog.PreviewCategoryChangeAsync(productId, body.NewCategoryId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.category_change.invalid" }, statusCode: StatusCodes.Status400BadRequest);
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
            return Results.Json(new { title = ex.Message, errorCode = "catalog.category_change.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}

/// <summary>بدنهٔ ایجاد تعریف ویژگی.</summary>
public sealed record CreateAttributeDefinitionRequest(
    string Code,
    CatalogAttributeValueKind ValueKind,
    bool IsVariantAxisAllowed,
    Dictionary<string, string>? LocalizedNames,
    UpdateAttributeDefinitionRequest? Metadata);

/// <summary>بدنهٔ به‌روزرسانی فرادادهٔ تعریف.</summary>
public sealed record UpdateAttributeDefinitionRequest(
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    decimal? ValidationMin,
    decimal? ValidationMax,
    int? ValidationMaxLength,
    bool IsActive);

/// <summary>بدنهٔ افزودن گزینه.</summary>
public sealed record AddAttributeOptionRequest(string Code, Dictionary<string, string>? LocalizedNames);

/// <summary>بدنهٔ پیوند schema رده.</summary>
public sealed record BindCategoryAttributeRequest(Guid DefinitionId, int DisplayOrder, bool? IsRequiredOverride);

/// <summary>بدنهٔ ترتیب پیوندها.</summary>
public sealed record ReorderCategoryBindingsRequest(List<Guid>? OrderedDefinitionIds);

/// <summary>بدنهٔ مقدار ویژگی محصول.</summary>
public sealed record SetProductAttributeRequest(string RawValue, Guid? EnumOptionId);

/// <summary>بدنهٔ محورهای Variant محصول.</summary>
public sealed record SetProductVariantAxesRequest(List<Guid>? OrderedDefinitionIds);

/// <summary>بدنهٔ تغییر رده.</summary>
public sealed record CategoryChangeRequest(Guid NewCategoryId);
