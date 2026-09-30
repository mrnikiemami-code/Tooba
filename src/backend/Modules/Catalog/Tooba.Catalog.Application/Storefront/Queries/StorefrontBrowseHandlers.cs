using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Storefront.Models;
using Tooba.Catalog.Application.Storefront.Ports;

namespace Tooba.Catalog.Application.Storefront.Queries;

/// <summary>Handles storefront home composition.</summary>
public sealed class GetStorefrontHomeQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontHomeQuery, Result<StorefrontHomePage>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontHomePage>> Handle(GetStorefrontHomeQuery request, CancellationToken cancellationToken)
        => Result.Success(await composer.GetHomeAsync(request.Locale, cancellationToken));
}

/// <summary>Handles storefront category list.</summary>
public sealed class GetStorefrontCategoriesQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontCategoriesQuery, Result<IReadOnlyList<StorefrontCategoryItem>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StorefrontCategoryItem>>> Handle(
        GetStorefrontCategoriesQuery request, CancellationToken cancellationToken)
        => Result.Success(await composer.ListCategoriesAsync(cancellationToken));
}

/// <summary>Handles storefront brand list.</summary>
public sealed class GetStorefrontBrandsQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontBrandsQuery, Result<IReadOnlyList<StorefrontBrandItem>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StorefrontBrandItem>>> Handle(
        GetStorefrontBrandsQuery request, CancellationToken cancellationToken)
        => Result.Success(await composer.ListBrandsAsync(cancellationToken));
}

/// <summary>Handles storefront brand-by-slug.</summary>
public sealed class GetStorefrontBrandBySlugQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontBrandBySlugQuery, Result<StorefrontBrandPage>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontBrandPage>> Handle(
        GetStorefrontBrandBySlugQuery request, CancellationToken cancellationToken)
    {
        var page = await composer.GetBrandAsync(request.Slug, cancellationToken);
        return page is null
            ? Result.Failure<StorefrontBrandPage>(new SemanticError("storefront.brand.missing"))
            : Result.Success(page);
    }
}

/// <summary>Handles storefront public seller list.</summary>
public sealed class GetStorefrontSellersQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontSellersQuery, Result<IReadOnlyList<StorefrontPublicSellerItem>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StorefrontPublicSellerItem>>> Handle(
        GetStorefrontSellersQuery request, CancellationToken cancellationToken)
        => Result.Success(await composer.ListPublicSellersAsync(cancellationToken));
}

/// <summary>Handles storefront seller-by-public-id.</summary>
public sealed class GetStorefrontSellerByPublicIdQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontSellerByPublicIdQuery, Result<StorefrontPublicSellerPage>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontPublicSellerPage>> Handle(
        GetStorefrontSellerByPublicIdQuery request, CancellationToken cancellationToken)
    {
        var page = await composer.GetPublicSellerAsync(request.PublicId, cancellationToken);
        return page is null
            ? Result.Failure<StorefrontPublicSellerPage>(new SemanticError("storefront.seller.missing"))
            : Result.Success(page);
    }
}

/// <summary>Handles storefront merchandising kind page.</summary>
public sealed class GetStorefrontMerchandisingQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontMerchandisingQuery, Result<StorefrontMerchandisingPage>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontMerchandisingPage>> Handle(
        GetStorefrontMerchandisingQuery request, CancellationToken cancellationToken)
        => Result.Success(await composer.GetMerchandisingAsync(request.Kind, cancellationToken));
}

/// <summary>Handles storefront product listing.</summary>
public sealed class GetStorefrontProductListingQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontProductListingQuery, Result<StorefrontListingPage>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontListingPage>> Handle(
        GetStorefrontProductListingQuery request, CancellationToken cancellationToken)
        => Result.Success(await composer.GetListingAsync(
            request.Q,
            request.CategoryId,
            request.SellerPartyId,
            request.InStock,
            request.Sort,
            request.Page,
            request.PageSize,
            cancellationToken));
}

/// <summary>Handles storefront product detail.</summary>
public sealed class GetStorefrontProductDetailQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontProductDetailQuery, Result<StorefrontProductDetailPage>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontProductDetailPage>> Handle(
        GetStorefrontProductDetailQuery request, CancellationToken cancellationToken)
    {
        var page = await composer.GetDetailAsync(request.Slug, request.VariantId, cancellationToken);
        return page is null
            ? Result.Failure<StorefrontProductDetailPage>(new SemanticError("storefront.product.missing"))
            : Result.Success(page);
    }
}

/// <summary>Handles storefront category PLP.</summary>
public sealed class GetStorefrontCategoryPlpQueryHandler(IStorefrontComposer composer)
    : IRequestHandler<GetStorefrontCategoryPlpQuery, Result<StorefrontCategoryPlpPage>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontCategoryPlpPage>> Handle(
        GetStorefrontCategoryPlpQuery request, CancellationToken cancellationToken)
    {
        var page = await composer.GetCategoryPlpAsync(
            request.Locale,
            request.Slug,
            request.Filters,
            request.Sort,
            request.Page,
            request.PageSize,
            cancellationToken);
        return page is null
            ? Result.Failure<StorefrontCategoryPlpPage>(new SemanticError("storefront.category.missing"))
            : Result.Success(page);
    }
}
