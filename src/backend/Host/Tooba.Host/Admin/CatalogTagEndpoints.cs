using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای Admin برای برچسب تاکسونومی Catalog — نه meta keywords.
/// </summary>
public static class CatalogTagEndpoints
{
    /// <summary>ثبت مسیرهای برچسب محصول/رده.</summary>
    public static void MapCatalogTagEndpoints(this WebApplication app)
    {
        var tags = app.MapGroup("/v1/admin/catalog/tags");
        tags.AddEndpointFilter(CatalogActorHttpBinding.BindAsync);
        tags.MapGet("/", ListTagsAsync);
        tags.MapPost("/", CreateTagAsync);
        tags.MapGet("/{tagId:guid}", GetTagAsync);

        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}/tags");
        products.AddEndpointFilter(CatalogActorHttpBinding.BindAsync);
        products.MapGet("/", ListProductTagsAsync);
        products.MapPost("/{tagId:guid}", AssignProductTagAsync);
        products.MapDelete("/{tagId:guid}", RemoveProductTagAsync);

        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/tags");
        categories.AddEndpointFilter(CatalogActorHttpBinding.BindAsync);
        categories.MapGet("/", ListCategoryTagsAsync);
        categories.MapPost("/{tagId:guid}", AssignCategoryTagAsync);
        categories.MapDelete("/{tagId:guid}", RemoveCategoryTagAsync);
    }

    private static async Task<IResult> ListTagsAsync(
        string? locale,
        string? search,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
        var items = await catalog.ListTagsAsync(string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale, search, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateTagAsync(
        CreateTagBody body,
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
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            var names = body.LocalizedNames ?? new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(body.NameFa))
            {
                names = new Dictionary<string, string>(names) { ["fa-IR"] = body.NameFa.Trim() };
            }

            if (!string.IsNullOrWhiteSpace(body.NameEn))
            {
                names = new Dictionary<string, string>(names) { ["en"] = body.NameEn.Trim() };
            }

            var created = await catalog.CreateTagAsync(body.Code, body.Slug, names, body.Locale ?? "fa-IR", cancellationToken);
            return Results.Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest, extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = "catalog.tag.invalid",
            });
        }
    }

    private static async Task<IResult> GetTagAsync(
        Guid tagId,
        string? locale,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
        var tag = await catalog.GetTagAsync(tagId, locale, cancellationToken);
        return tag is null ? Results.NotFound() : Results.Ok(tag);
    }

    private static async Task<IResult> ListProductTagsAsync(
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
        await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
        return Results.Ok(await catalog.ListProductTagsAsync(productId, locale, cancellationToken));
    }

    private static async Task<IResult> AssignProductTagAsync(
        Guid productId,
        Guid tagId,
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
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            await catalog.AssignProductTagAsync(productId, tagId, cancellationToken);
            return Results.Ok(await catalog.ListProductTagsAsync(productId, "fa-IR", cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest, extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = "catalog.tag.assign.duplicate",
            });
        }
    }

    private static async Task<IResult> RemoveProductTagAsync(
        Guid productId,
        Guid tagId,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
        await catalog.RemoveProductTagAsync(productId, tagId, cancellationToken);
        return Results.Ok(await catalog.ListProductTagsAsync(productId, "fa-IR", cancellationToken));
    }

    private static async Task<IResult> ListCategoryTagsAsync(
        Guid categoryId,
        string? locale,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
        return Results.Ok(await catalog.ListCategoryTagsAsync(categoryId, locale, cancellationToken));
    }

    private static async Task<IResult> AssignCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
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
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            await catalog.AssignCategoryTagAsync(categoryId, tagId, cancellationToken);
            return Results.Ok(await catalog.ListCategoryTagsAsync(categoryId, "fa-IR", cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest, extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = "catalog.tag.assign.duplicate",
            });
        }
    }

    private static async Task<IResult> RemoveCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
        await catalog.RemoveCategoryTagAsync(categoryId, tagId, cancellationToken);
        return Results.Ok(await catalog.ListCategoryTagsAsync(categoryId, "fa-IR", cancellationToken));
    }
}

/// <summary>بدنهٔ ایجاد برچسب.</summary>
public sealed record CreateTagBody(
    string? Code,
    string? Slug,
    string? NameFa,
    string? NameEn,
    string? Locale,
    Dictionary<string, string>? LocalizedNames);
