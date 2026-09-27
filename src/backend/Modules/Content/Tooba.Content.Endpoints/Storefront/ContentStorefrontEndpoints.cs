using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Content.Application.Authors.Queries;
using Tooba.Content.Application.Categories.Queries;
using Tooba.Content.Application.Articles.Queries;

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
        ISender sender,
        ApiResponseFactory api,
        int page = 1,
        int pageSize = 20,
        string? category = null,
        string? locale = null,
        string? categorySlug = null,
        string? authorSlug = null,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(
            new ListPublishedArticlesQuery(page, pageSize, category, locale, categorySlug, authorSlug),
            cancellationToken));

    private static async Task<IResult> GetPublishedBySlugAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(new GetPublishedArticleBySlugQuery(slug, locale), cancellationToken));

    private static async Task<IResult> ListPublicCategoriesAsync(
        ISender sender,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(new ListPublicCategoriesQuery(locale), cancellationToken));

    private static async Task<IResult> GetPublicCategoryBySlugAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(new GetPublicCategoryBySlugQuery(slug, locale), cancellationToken));

    private static async Task<IResult> ListPublicAuthorsAsync(
        ISender sender,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(new ListPublicAuthorsQuery(locale), cancellationToken));

    private static async Task<IResult> GetPublicAuthorBySlugAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(new GetPublicAuthorBySlugQuery(slug, locale), cancellationToken));
}
