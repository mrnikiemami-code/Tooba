using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Storefront.Models;

namespace Tooba.Catalog.Application.Storefront.Queries;

/// <summary>GET /v1/storefront/home</summary>
public sealed record GetStorefrontHomeQuery(string? Locale)
    : IRequest<Result<StorefrontHomePage>>;

/// <summary>GET /v1/storefront/categories</summary>
public sealed record GetStorefrontCategoriesQuery
    : IRequest<Result<IReadOnlyList<StorefrontCategoryItem>>>;

/// <summary>GET /v1/storefront/brands</summary>
public sealed record GetStorefrontBrandsQuery
    : IRequest<Result<IReadOnlyList<StorefrontBrandItem>>>;

/// <summary>GET /v1/storefront/brands/{slug}</summary>
public sealed record GetStorefrontBrandBySlugQuery(string Slug)
    : IRequest<Result<StorefrontBrandPage>>;

/// <summary>GET /v1/storefront/sellers</summary>
public sealed record GetStorefrontSellersQuery
    : IRequest<Result<IReadOnlyList<StorefrontPublicSellerItem>>>;

/// <summary>GET /v1/storefront/sellers/{publicId}</summary>
public sealed record GetStorefrontSellerByPublicIdQuery(string PublicId)
    : IRequest<Result<StorefrontPublicSellerPage>>;

/// <summary>GET /v1/storefront/merchandising/{kind}</summary>
public sealed record GetStorefrontMerchandisingQuery(string Kind)
    : IRequest<Result<StorefrontMerchandisingPage>>;

/// <summary>GET /v1/storefront/products</summary>
public sealed record GetStorefrontProductListingQuery(
    string? Q,
    Guid? CategoryId,
    Guid? SellerPartyId,
    bool? InStock,
    string? Sort,
    int Page,
    int PageSize)
    : IRequest<Result<StorefrontListingPage>>;

/// <summary>GET /v1/storefront/products/{slug}</summary>
public sealed record GetStorefrontProductDetailQuery(string Slug, Guid? VariantId)
    : IRequest<Result<StorefrontProductDetailPage>>;

/// <summary>GET /v1/storefront/category-plp/{slug}</summary>
public sealed record GetStorefrontCategoryPlpQuery(
    string Locale,
    string Slug,
    IReadOnlyList<StorefrontPlpFilterInput> Filters,
    string? Sort,
    int Page,
    int PageSize)
    : IRequest<Result<StorefrontCategoryPlpPage>>;
