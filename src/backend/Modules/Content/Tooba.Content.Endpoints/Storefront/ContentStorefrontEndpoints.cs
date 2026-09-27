using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Tooba.Content.Endpoints.Storefront;

/// <summary>مسیرهای عمومی storefront برای Content.</summary>
public static class ContentStorefrontEndpoints
{
    /// <summary>مسیرهای /v1/content/* را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/content/articles", ListPublishedAsync);
        app.MapGet("/v1/content/articles/{slug}", GetPublishedBySlugAsync);
        app.MapGet("/v1/content/categories", ListPublicCategoriesAsync);
        app.MapGet("/v1/content/categories/{slug}", GetPublicCategoryBySlugAsync);
        app.MapGet("/v1/content/authors", ListPublicAuthorsAsync);
        app.MapGet("/v1/content/authors/{slug}", GetPublicAuthorBySlugAsync);
    }

    private static async Task<IResult> ListPublishedAsync(
        ContentPanelComposer composer,
        int page = 1,
        int pageSize = 20,
        string? category = null,
        string? locale = null,
        string? categorySlug = null,
        string? authorSlug = null,
        CancellationToken cancellationToken = default) =>
        Results.Json(await composer.ListPublishedAsync(
            page,
            pageSize,
            category,
            locale,
            categorySlug,
            authorSlug,
            cancellationToken));

    private static async Task<IResult> GetPublishedBySlugAsync(
        string slug,
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var article = await composer.GetPublishedBySlugAsync(slug, locale, cancellationToken);
        return article is null ? Results.NotFound() : Results.Json(article);
    }

    private static async Task<IResult> ListPublicCategoriesAsync(
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        Results.Json(await composer.ListPublicCategoriesAsync(locale, cancellationToken));

    private static async Task<IResult> GetPublicCategoryBySlugAsync(
        string slug,
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var category = await composer.GetPublicCategoryBySlugAsync(locale, slug, cancellationToken);
        return category is null ? Results.NotFound() : Results.Json(category);
    }

    private static async Task<IResult> ListPublicAuthorsAsync(
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        Results.Json(await composer.ListPublicAuthorsAsync(locale, cancellationToken));

    private static async Task<IResult> GetPublicAuthorBySlugAsync(
        string slug,
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var author = await composer.GetPublicAuthorBySlugAsync(slug, locale, cancellationToken);
        return author is null ? Results.NotFound() : Results.Json(author);
    }
}
