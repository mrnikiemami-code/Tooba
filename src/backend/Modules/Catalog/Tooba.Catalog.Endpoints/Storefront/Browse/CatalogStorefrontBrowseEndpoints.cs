using MediatR;
using Tooba.Catalog.Application.Storefront.Models;
using Tooba.Catalog.Application.Storefront.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.Browse;

/// <summary>
/// Catalog-owned storefront browse BFF routes (home/categories/brands/sellers/merchandising/products/PLP).
/// Response shapes and errorCodes preserved from Host StorefrontEndpoints.
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
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStorefrontHomeQuery(locale), cancellationToken);
        return Results.Json(result.Value);
    }

    private static async Task<IResult> GetCategoriesAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontCategoriesQuery(), cancellationToken);
        return Results.Json(result.Value);
    }

    private static async Task<IResult> GetBrandsAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontBrandsQuery(), cancellationToken);
        return Results.Json(result.Value);
    }

    private static async Task<IResult> GetBrandAsync(string slug, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontBrandBySlugQuery(slug), cancellationToken);
        return result.IsFailure
            ? Results.Json(new { title = "Not Found", errorCode = result.Errors[0].Code }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result.Value);
    }

    private static async Task<IResult> GetSellersAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontSellersQuery(), cancellationToken);
        return Results.Json(result.Value);
    }

    private static async Task<IResult> GetSellerAsync(string publicId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontSellerByPublicIdQuery(publicId), cancellationToken);
        return result.IsFailure
            ? Results.Json(new { title = "Not Found", errorCode = result.Errors[0].Code }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result.Value);
    }

    private static async Task<IResult> GetMerchandisingAsync(string kind, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontMerchandisingQuery(kind), cancellationToken);
        return Results.Json(result.Value);
    }

    private static async Task<IResult> GetListingAsync(
        ISender sender,
        string? q,
        Guid? categoryId,
        Guid? sellerPartyId,
        bool? inStock,
        string? sort,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetStorefrontProductListingQuery(q, categoryId, sellerPartyId, inStock, sort, page, pageSize),
            cancellationToken);
        return Results.Json(result.Value);
    }

    private static async Task<IResult> GetDetailAsync(
        string slug,
        Guid? variantId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStorefrontProductDetailQuery(slug, variantId), cancellationToken);
        return result.IsFailure
            ? Results.Json(new { title = "Not Found", errorCode = result.Errors[0].Code }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result.Value);
    }

    private static async Task<IResult> GetCategoryPlpAsync(
        string slug,
        ISender sender,
        HttpRequest request,
        string? locale,
        string? sort,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var filters = ParsePlpFilters(request);
        var result = await sender.Send(
            new GetStorefrontCategoryPlpQuery(locale ?? "fa-IR", slug, filters, sort, page, pageSize),
            cancellationToken);
        return result.IsFailure
            ? Results.Json(new { title = "Not Found", errorCode = result.Errors[0].Code }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(result.Value);
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
