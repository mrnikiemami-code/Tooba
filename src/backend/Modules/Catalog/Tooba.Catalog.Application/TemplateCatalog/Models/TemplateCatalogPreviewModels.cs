namespace Tooba.Catalog.Application.TemplateCatalog.Models;

/// <summary>Storefront template-catalog preview envelope (Fashion + Industry samples).</summary>
public sealed record FashionTemplatePreviewDto(
    string Origin,
    string TemplateId,
    string TemplateKey,
    string TemplateName,
    FashionTemplatePageDto Page,
    IReadOnlyList<FashionTemplateCategoryDto> Categories,
    IReadOnlyList<FashionTemplateProductDto> Products,
    IReadOnlyList<FashionTemplateBrandDto> Brands,
    FashionTemplateCompositionFillersDto CompositionFillers,
    FashionTemplatePurityDto Purity);

/// <summary>Landing page projection inside a template preview.</summary>
public sealed record FashionTemplatePageDto(
    string PageId,
    string Locale,
    string Slug,
    string Title,
    string? SeoTitle,
    string? SeoDescription,
    string TemplateKey,
    IReadOnlyList<FashionTemplateSectionDto> Sections);

/// <summary>Landing section JSON payload.</summary>
public sealed record FashionTemplateSectionDto(
    string PageSectionId,
    string SectionType,
    int SortOrder,
    string ConfigurationJson);

/// <summary>Template category row.</summary>
public sealed record FashionTemplateCategoryDto(
    string CategoryId,
    string? ParentCategoryId,
    string Name,
    string? ImageMediaAssetId,
    string? ImageUrl);

/// <summary>Template product card with filler commercial fields.</summary>
public sealed record FashionTemplateProductDto(
    string ProductId,
    string Slug,
    string Title,
    string CategoryName,
    string CategoryId,
    string MediaAssetId,
    string? MediaUrl,
    string PrimaryOfferId,
    string SellerPartyId,
    string SellerDisplayName,
    decimal OfferAmountExclusiveOfTax,
    decimal? PromotionalAmountExclusiveOfTax,
    string Currency,
    int AvailableUnits,
    bool InStock,
    string? PromotionLabel,
    double AverageRating,
    int ReviewCount,
    string? BrandId);

/// <summary>Template brand row.</summary>
public sealed record FashionTemplateBrandDto(
    string BrandId,
    string Slug,
    string Name,
    int ProductCount,
    string? LogoMediaAssetId,
    string? LogoUrl);

/// <summary>Non-persisted composition fillers (articles/reviews) for preview UI.</summary>
public sealed record FashionTemplateCompositionFillersDto(
    IReadOnlyList<FashionTemplateArticleDto> Articles,
    IReadOnlyList<FashionTemplateReviewDto> Reviews);

/// <summary>Filler article.</summary>
public sealed record FashionTemplateArticleDto(
    string ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    string CoverMediaAssetId,
    string? CoverMediaUrl,
    string PublishDate,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured);

/// <summary>Filler review.</summary>
public sealed record FashionTemplateReviewDto(
    string PublicId,
    string AuthorDisplayName,
    int Rating,
    string Title,
    string Body,
    bool VerifiedPurchase,
    string CreatedAt,
    string ProductTitle,
    string ProductSlug);

/// <summary>Operational-catalog purity proof counters.</summary>
public sealed record FashionTemplatePurityDto(
    int TemplateProductCount,
    int TemplateTopLevelCategoryCount,
    int TemplateBrandCount,
    int TemplateBannerItemCount,
    int OperationalProductIdHits,
    int OperationalCategoryIdHits,
    int OperationalBrandIdHits,
    bool IsPure);
