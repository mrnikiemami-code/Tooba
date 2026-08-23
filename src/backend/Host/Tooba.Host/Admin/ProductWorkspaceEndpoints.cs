using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای HTTP ترکیب Workspace محصول. SQL بین‌ماژولی اینجا نوشته نمی‌شود.
/// </summary>
public static class ProductWorkspaceEndpoints
{
    /// <summary>
    /// مسیرهای Admin Product Workspace را ثبت می‌کند.
    /// </summary>
    public static void MapProductWorkspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/products");
        group.MapGet("/", ListAsync);
        group.MapGet("/{productId:guid}", GetAsync);
        group.MapPatch("/{productId:guid}/catalog-title", PatchTitleAsync);
    }

    private static ProductWorkspacePermissions ReadPermissions(HttpRequest request)
    {
        var scope = request.Headers["X-Tooba-Workspace-Scope"].ToString();
        if (string.Equals(scope, "view", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductWorkspacePermissions(true, false, false, false, false);
        }

        return new ProductWorkspacePermissions(true, true, true, true, true);
    }

    private static async Task<IResult> ListAsync(ProductWorkspaceComposer composer, CancellationToken cancellationToken)
    {
        var items = await composer.ListAsync(cancellationToken);
        return Results.Json(items);
    }

    private static async Task<IResult> GetAsync(Guid productId, ProductWorkspaceComposer composer, HttpRequest request, CancellationToken cancellationToken)
    {
        var workspace = await composer.GetAsync(productId, ReadPermissions(request), cancellationToken);
        return workspace is null
            ? Results.Json(new { title = "Not Found", errorCode = "workspace.product.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(workspace);
    }

    private static async Task<IResult> PatchTitleAsync(
        Guid productId,
        CatalogTitlePatch body,
        ProductWorkspaceComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var workspace = await composer.UpdateCatalogTitleAsync(
                productId,
                body.Locale,
                body.Title,
                body.ExpectedUpdatedAt,
                ReadPermissions(request),
                cancellationToken);
            return Results.Json(workspace);
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }
}

/// <summary>
/// بدنهٔ به‌روزرسانی عنوان Catalog با قفل خوش‌بینانه.
/// </summary>
public sealed record CatalogTitlePatch(string Locale, string Title, DateTimeOffset ExpectedUpdatedAt);
