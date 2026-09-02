using Tooba.BuildingBlocks;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Host.Admin;

namespace Tooba.Host.Content;

/// <summary>مسیرهای Admin دسته‌بندی مقاله.</summary>
public static class ContentCategoryEndpoints
{
    /// <summary>مسیرهای Admin دسته‌بندی مقاله را ثبت می‌کند.</summary>
    public static void MapContentCategoryEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/content/categories");
        admin.MapGet("/tree", GetTreeAsync);
        admin.MapGet("/{id:guid}", GetWorkspaceAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPut("/{id:guid}/seo", UpdateSeoAsync);
        admin.MapPut("/{id:guid}/media", UpdateMediaAsync);
        admin.MapPost("/{id:guid}/move", MoveAsync);
        admin.MapPost("/reorder", ReorderAsync);
        admin.MapPost("/{id:guid}/archive", ArchiveAsync);
    }

    private static async Task<IResult> GetTreeAsync(
        string languageCode,
        string? search,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await directory.GetTreeAsync(languageCode, search, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid id,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var workspace = await directory.GetWorkspaceAsync(id, cancellationToken);
            return workspace is null
                ? Results.Json(new { title = "Not Found", errorCode = ContentCategoryErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(workspace);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> CreateAsync(
        CreateContentCategoryHttpRequest body,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var created = await directory.CreateAsync(new CreateContentCategoryCommand(
                body.LanguageCode ?? "",
                body.ParentCategoryId,
                body.Name ?? "",
                body.Slug ?? "",
                body.ShortDescription,
                body.Description,
                body.SortOrder ?? 0), cancellationToken);
            return Results.Json(created);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateContentCategoryHttpRequest body,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.UpdateAsync(id, new UpdateContentCategoryCommand(
                body.Name ?? "",
                body.Slug ?? "",
                body.ShortDescription,
                body.Description,
                body.SortOrder ?? 0,
                body.Status ?? "Active"), cancellationToken);
            return Results.Json(updated);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> UpdateSeoAsync(
        Guid id,
        UpdateContentCategorySeoHttpRequest body,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.UpdateSeoAsync(id, new UpdateContentCategorySeoCommand(body.SeoTitle, body.SeoDescription), cancellationToken);
            return Results.Json(updated);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> UpdateMediaAsync(
        Guid id,
        UpdateContentCategoryMediaHttpRequest body,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.UpdateMediaAsync(id, new UpdateContentCategoryMediaCommand(body.ImageMediaAssetId), cancellationToken);
            return Results.Json(updated);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> MoveAsync(
        Guid id,
        MoveContentCategoryHttpRequest body,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.MoveAsync(id, new MoveContentCategoryCommand(body.NewParentId), cancellationToken);
            return Results.Json(updated);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> ReorderAsync(
        ReorderContentCategoryHttpRequest body,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var items = (body.Items ?? [])
                .Select(x => new ReorderContentCategoryItem(x.CategoryId, x.SortOrder))
                .ToList();
            await directory.ReorderAsync(items, cancellationToken);
            return Results.Ok();
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        IContentCategoryDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            await directory.ArchiveAsync(id, cancellationToken);
            return Results.Ok();
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult MapInvalid(InvalidOperationException ex) =>
        Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
}

/// <summary>بدنهٔ ایجاد دستهٔ مقاله.</summary>
public sealed record CreateContentCategoryHttpRequest(
    string? LanguageCode,
    Guid? ParentCategoryId,
    string? Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    int? SortOrder);

/// <summary>بدنهٔ به‌روزرسانی عمومی دسته.</summary>
public sealed record UpdateContentCategoryHttpRequest(
    string? Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    int? SortOrder,
    string? Status);

/// <summary>بدنهٔ SEO دسته.</summary>
public sealed record UpdateContentCategorySeoHttpRequest(string? SeoTitle, string? SeoDescription);

/// <summary>بدنهٔ رسانه دسته.</summary>
public sealed record UpdateContentCategoryMediaHttpRequest(Guid? ImageMediaAssetId);

/// <summary>بدنهٔ جابه‌جایی والد.</summary>
public sealed record MoveContentCategoryHttpRequest(Guid? NewParentId);

/// <summary>بدنهٔ مرتب‌سازی مجدد.</summary>
public sealed record ReorderContentCategoryHttpRequest(IReadOnlyList<ReorderContentCategoryHttpItem>? Items);

/// <summary>آیتم مرتب‌سازی.</summary>
public sealed record ReorderContentCategoryHttpItem(Guid CategoryId, int SortOrder);
