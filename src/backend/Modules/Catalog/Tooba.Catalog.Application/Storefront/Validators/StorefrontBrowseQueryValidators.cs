using FluentValidation;
using Tooba.Catalog.Application.Storefront.Queries;

namespace Tooba.Catalog.Application.Storefront.Validators;

/// <summary>Transport shape for brand slug.</summary>
public sealed class GetStorefrontBrandBySlugQueryValidator : AbstractValidator<GetStorefrontBrandBySlugQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetStorefrontBrandBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithErrorCode("storefront.validation.brand_slug_required");
    }
}

/// <summary>Transport shape for seller public id.</summary>
public sealed class GetStorefrontSellerByPublicIdQueryValidator : AbstractValidator<GetStorefrontSellerByPublicIdQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetStorefrontSellerByPublicIdQueryValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty()
            .WithErrorCode("storefront.validation.seller_public_id_required");
    }
}

/// <summary>Transport shape for merchandising kind.</summary>
public sealed class GetStorefrontMerchandisingQueryValidator : AbstractValidator<GetStorefrontMerchandisingQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetStorefrontMerchandisingQueryValidator()
    {
        RuleFor(x => x.Kind)
            .NotEmpty()
            .WithErrorCode("storefront.validation.merchandising_kind_required");
    }
}

/// <summary>Transport shape for product listing paging.</summary>
public sealed class GetStorefrontProductListingQueryValidator : AbstractValidator<GetStorefrontProductListingQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetStorefrontProductListingQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("storefront.validation.page_invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 48).WithErrorCode("storefront.validation.page_size_invalid");
    }
}

/// <summary>Transport shape for product detail slug.</summary>
public sealed class GetStorefrontProductDetailQueryValidator : AbstractValidator<GetStorefrontProductDetailQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetStorefrontProductDetailQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithErrorCode("storefront.validation.product_slug_required");
    }
}

/// <summary>Transport shape for category PLP.</summary>
public sealed class GetStorefrontCategoryPlpQueryValidator : AbstractValidator<GetStorefrontCategoryPlpQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetStorefrontCategoryPlpQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithErrorCode("storefront.validation.category_slug_required");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("storefront.validation.page_invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 48).WithErrorCode("storefront.validation.page_size_invalid");
    }
}
