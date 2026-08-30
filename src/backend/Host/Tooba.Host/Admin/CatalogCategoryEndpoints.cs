using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای Admin برای foundation درخت/ترجمه/مسیر رده Catalog.
/// UI درخت Ant در T005 است؛ اینجا فقط قرارداد HTTP است.
/// </summary>
public static class CatalogCategoryEndpoints
{
    /// <summary>
    /// مسیرهای Admin Category و resolve ویترین را ثبت می‌کند.
    /// </summary>
    public static void MapCatalogCategoryEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/catalog/categories");
        admin.MapGet("/tree", GetTreeAsync);
        admin.MapGet("/{id:guid}", GetWorkspaceAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPatch("/{id:guid}", UpdateCoreAsync);
        admin.MapPut("/{id:guid}/translations/{locale}", UpsertTranslationAsync);
        admin.MapPost("/{id:guid}/move", MoveAsync);
        admin.MapPost("/reorder", ReorderAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/archive", ArchiveAsync);

        app.MapGet("/v1/storefront/category-routes/resolve", ResolveRouteAsync);
    }

    private static async Task<IResult> GetTreeAsync(
        string locale,
        string? search,
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
            if (string.IsNullOrWhiteSpace(locale))
            {
                return Results.Json(
                    new { title = "locale الزامی است.", errorCode = "catalog.category.invalid" },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Json(await catalog.GetCategoryTreeAsync(locale, search, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid id,
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
            var workspace = await catalog.GetCategoryWorkspaceAsync(id, locale, cancellationToken);
            return workspace is null
                ? Results.Json(new { title = "Not Found", errorCode = "catalog.category.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(workspace);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> CreateAsync(
        CreateCategoryHttpRequest body,
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

            CategoryReference created;
            if (body.Translations is { Count: > 0 })
            {
                created = await catalog.CreateCategoryAsync(
                    new CategoryCreateRequest(
                        body.ParentCategoryId,
                        body.SortOrder,
                        body.IsVisible,
                        body.ImageMediaAssetId,
                        body.IconMediaAssetId,
                        body.BannerMediaAssetId,
                        body.Translations
                            .Select(t => new CategoryTranslationUpsertRequest(
                                t.Locale,
                                t.Name,
                                t.Slug,
                                t.ShortDescription,
                                t.Description,
                                t.SeoTitle,
                                t.SeoDescription,
                                t.MetaKeywords))
                            .ToList()),
                    cancellationToken);
            }
            else
            {
                created = await catalog.CreateCategoryAsync(
                    body.ParentCategoryId,
                    body.LocalizedNames ?? new Dictionary<string, string>(),
                    cancellationToken);
            }

            return Results.Json(created, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> UpdateCoreAsync(
        Guid id,
        UpdateCategoryCoreHttpRequest body,
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
            await catalog.UpdateCategoryCoreAsync(
                id,
                new CategoryCoreUpdateRequest(
                    body.Status,
                    body.SortOrder,
                    body.IsVisible,
                    body.ImageMediaAssetId,
                    body.IconMediaAssetId,
                    body.BannerMediaAssetId,
                    body.ClearImage,
                    body.ClearIcon,
                    body.ClearBanner,
                    body.ExpectedUpdatedAt),
                cancellationToken);
            return Results.Json(await catalog.GetCategoryWorkspaceAsync(id, null, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> UpsertTranslationAsync(
        Guid id,
        string locale,
        UpsertCategoryTranslationHttpRequest body,
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
            var dto = await catalog.UpsertCategoryTranslationAsync(
                id,
                new CategoryTranslationUpsertRequest(
                    locale,
                    body.Name,
                    body.Slug,
                    body.ShortDescription,
                    body.Description,
                    body.SeoTitle,
                    body.SeoDescription,
                    body.MetaKeywords),
                cancellationToken);
            return Results.Json(dto);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> MoveAsync(
        Guid id,
        MoveCategoryHttpRequest body,
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
            await catalog.MoveCategoryAsync(id, body.NewParentId, body.ExpectedUpdatedAt, cancellationToken);
            return Results.Json(await catalog.GetCategoryWorkspaceAsync(id, null, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> ReorderAsync(
        ReorderCategoriesHttpRequest body,
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
            await catalog.ReorderCategorySiblingsAsync(
                body.ParentId,
                body.OrderedCategoryIds ?? [],
                cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> PublishAsync(
        Guid id,
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
            await catalog.PublishCategoryAsync(id, cancellationToken);
            return Results.Json(await catalog.GetCategoryWorkspaceAsync(id, null, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
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
            await catalog.ArchiveCategoryAsync(id, cancellationToken);
            return Results.Json(await catalog.GetCategoryWorkspaceAsync(id, null, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return MapCategoryInvalid(ex);
        }
    }

    private static async Task<IResult> ResolveRouteAsync(
        string locale,
        string slug,
        bool forStorefront,
        ICatalogDirectory catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(locale) || string.IsNullOrWhiteSpace(slug))
            {
                return Results.Json(
                    new { title = "locale و slug الزامی هستند.", errorCode = "catalog.category.route.invalid" },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await catalog.ResolveCategoryRouteAsync(locale, slug, forStorefront, cancellationToken);
            return result is null
                ? Results.Json(new { title = "Not Found", errorCode = "catalog.category.route.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.category.route.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static IResult MapCategoryInvalid(InvalidOperationException ex)
    {
        // کوچک‌ترین نگاشت برای تعارض نامک تکراری — بدون پسوند CategoryId.
        if (ex.Message.Contains("slug", StringComparison.OrdinalIgnoreCase)
            && ex.Message.Contains("تکراری", StringComparison.Ordinal))
        {
            return Results.Json(
                new
                {
                    title = "این نامک برای یک دسته‌بندی دیگر استفاده شده است. یک نامک متفاوت انتخاب کنید.",
                    errorCode = "catalog.category.slug.duplicate",
                },
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Json(
            new { title = ex.Message, errorCode = "catalog.category.invalid" },
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}

/// <summary>بدنهٔ ایجاد رده Admin.</summary>
public sealed record CreateCategoryHttpRequest(
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId,
    List<CategoryTranslationInputHttpRequest>? Translations,
    Dictionary<string, string>? LocalizedNames);

/// <summary>بدنهٔ به‌روزرسانی هسته.</summary>
public sealed record UpdateCategoryCoreHttpRequest(
    CatalogPublicationStatus? Status,
    int? SortOrder,
    bool? IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId = null,
    bool ClearImage = false,
    bool ClearIcon = false,
    bool ClearBanner = false,
    DateTimeOffset? ExpectedUpdatedAt = null);

/// <summary>ترجمه در بدنهٔ ایجاد.</summary>
public sealed record CategoryTranslationInputHttpRequest(
    string Locale,
    string Name,
    string Slug,
    string? ShortDescription = null,
    string? Description = null,
    string? SeoTitle = null,
    string? SeoDescription = null,
    string? MetaKeywords = null);

/// <summary>بدنهٔ upsert ترجمه؛ locale از مسیر می‌آید.</summary>
public sealed record UpsertCategoryTranslationHttpRequest(
    string Name,
    string Slug,
    string? ShortDescription = null,
    string? Description = null,
    string? SeoTitle = null,
    string? SeoDescription = null,
    string? MetaKeywords = null);

/// <summary>بدنهٔ جابه‌جایی.</summary>
public sealed record MoveCategoryHttpRequest(Guid? NewParentId, DateTimeOffset? ExpectedUpdatedAt = null);

/// <summary>بدنهٔ ترتیب خواهر/برادر.</summary>
public sealed record ReorderCategoriesHttpRequest(Guid? ParentId, List<Guid>? OrderedCategoryIds);
