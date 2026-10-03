using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Storefront.Models;
using Tooba.Catalog.Application.Storefront.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.Browse;

/// <summary>
/// Catalog-owned storefront browse BFF routes (home/categories/brands/sellers/merchandising/products/PLP).
/// Dispatches via MediatR + ApiResponseFactory (canonical ProblemDetails on failure).
/// </summary>
public static class CatalogStorefrontBrowseEndpoints
{
    /// <summary>Maps browse routes under /v1/storefront.</summary>
    public static void MapCatalogStorefrontBrowseEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/storefront");
        group.MapGet("/home", GetHomeAsync);
        group.MapGet("/categories", GetCategoriesAsync);
        group.MapGet("/brands", GetBrandsAsync);
        group.MapGet("/brands/{slug}", GetBrandAsync);
        group.MapGet("/sellers", GetSellersAsync);
        group.MapGet("/sellers/{publicId}", GetSellerAsync);
        group.MapGet("/merchandising/{kind}", GetMerchandisingAsync);
        group.MapGet("/products", GetListingAsync);
        group.MapGet("/products/{slug}", GetDetailAsync);
        group.MapGet("/category-plp/{slug}", GetCategoryPlpAsync);
    }

    private static async Task<IResult> GetHomeAsync(
        ISender sender,
        ApiResponseFactory api,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(new GetStorefrontHomeQuery(locale), cancellationToken));

    private static async Task<IResult> GetCategoriesAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontCategoriesQuery(), cancellationToken));

    private static async Task<IResult> GetBrandsAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontBrandsQuery(), cancellationToken));

    private static async Task<IResult> GetBrandAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontBrandBySlugQuery(slug), cancellationToken));

    private static async Task<IResult> GetSellersAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontSellersQuery(), cancellationToken));

    private static async Task<IResult> GetSellerAsync(
        string publicId,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontSellerByPublicIdQuery(publicId), cancellationToken));

    private static async Task<IResult> GetMerchandisingAsync(
        string kind,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontMerchandisingQuery(kind), cancellationToken));

    private static async Task<IResult> GetListingAsync(
        ISender sender,
        ApiResponseFactory api,
        string? q,
        Guid? categoryId,
        Guid? sellerPartyId,
        bool? inStock,
        string? sort,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default) =>
        api.From(await sender.Send(
            new GetStorefrontProductListingQuery(q, categoryId, sellerPartyId, inStock, sort, page, pageSize),
            cancellationToken));

    private static async Task<IResult> GetDetailAsync(
        string slug,
        Guid? variantId,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontProductDetailQuery(slug, variantId), cancellationToken));

    private static async Task<IResult> GetCategoryPlpAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        string? locale,
        string? sort,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var filters = ParsePlpFilters(request);
        return api.From(await sender.Send(
            new GetStorefrontCategoryPlpQuery(locale ?? "fa-IR", slug, filters, sort, page, pageSize),
            cancellationToken));
    }

    private static IReadOnlyList<StorefrontPlpFilterInput> ParsePlpFilters(HttpRequest request)
    {
        var list = new List<StorefrontPlpFilterInput>();
        foreach (var pair in request.Query)
        {
            var key = pair.Key;
            var raw = pair.Value.ToString();
            if (string.IsNullOrWhiteSpace(raw)) continue;
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
}
