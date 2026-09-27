using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای Admin برای تغییر رده (Variant Axes/Matrix در Catalog.Endpoints؛ تعاریف/schema/ویژگی محصول هم Catalog-owned).
/// </summary>
public static class CatalogAttributeEndpoints
{
    /// <summary>
    /// مسیرهای Admin category-change را ثبت می‌کند.
    /// </summary>
    public static void MapCatalogAttributeEndpoints(this WebApplication app)
    {
        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}");
        products.AddEndpointFilter(CatalogActorHttpBinding.BindAsync);
        products.MapPost("/category-change-preview", PreviewCategoryChangeAsync);
        products.MapPut("/primary-category", ReplacePrimaryCategoryAsync);
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

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}

/// <summary>بدنهٔ تغییر رده.</summary>
public sealed record CategoryChangeRequest(Guid NewCategoryId);

/// <summary>بدنهٔ پیش‌نمایش تغییر رده با locale برای برچسب‌ها.</summary>
public sealed record CategoryChangePreviewRequest(Guid NewCategoryId, string? Locale);
