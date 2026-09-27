namespace Tooba.Catalog.Application.StoreLandingPages.Models;

/// <summary>درخواست نوشتن صفحه.</summary>
public sealed record StoreLandingPageWriteRequest(
    string? Title,
    string? Slug,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Status,
    string? PageType = null,
    bool? RobotsIndex = null,
    bool? RobotsFollow = null,
    string? CanonicalUrl = null,
    string? OgTitle = null,
    string? OgDescription = null,
    string? OgImageUrl = null,
    string? PrimaryH1 = null);

/// <summary>درخواست انتخاب خانه.</summary>
public sealed record StoreHomeSelectionWriteRequest(Guid? HomePageId);

/// <summary>نمای Admin.</summary>
public sealed record StoreLandingPageAdminView(
    Guid PageId,
    string PageType,
    string Locale,
    string Slug,
    string Title,
    string? SeoTitle,
    string? SeoDescription,
    bool RobotsIndex,
    bool RobotsFollow,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? PrimaryH1,
    string TemplateKey,
    string Status,
    DateTimeOffset UpdatedAt);

/// <summary>درخواست نوشتن بخش.</summary>
public sealed record StoreLandingPageSectionWriteRequest(
    string? SectionType,
    string? Config,
    string? ConfigJson,
    bool? IsEnabled,
    int? InsertAt = null);

/// <summary>درخواست جایگزینی کامل ترکیب بخش‌ها (اعمال قالب).</summary>
public sealed record StoreLandingPageCompositionReplaceRequest(
    IReadOnlyList<StoreLandingPageSectionWriteRequest>? Sections);

/// <summary>درخواست ترتیب بخش‌ها.</summary>
public sealed record StoreLandingPageSectionReorderRequest(IReadOnlyList<Guid>? SectionIds);

/// <summary>درخواست فعال‌سازی بخش.</summary>
public sealed record StoreLandingPageSectionEnabledRequest(bool IsEnabled);

/// <summary>نمای Admin بخش.</summary>
public sealed record StoreLandingPageSectionAdminView(
    Guid PageSectionId,
    Guid PageId,
    string SectionType,
    int SortOrder,
    bool IsEnabled,
    string Config,
    DateTimeOffset UpdatedAt);

/// <summary>آیتم حل‌شدهٔ منبع کنترل‌شده.</summary>
public sealed record StoreLandingPageResolvedItem(
    Guid Id,
    string? Slug,
    decimal? OfferAmountExclusiveOfTax = null,
    decimal? PromotionalAmountExclusiveOfTax = null,
    string? Currency = null,
    string? PromotionLabel = null,
    Guid? MerchandisingCampaignId = null);

/// <summary>نمای عمومی بخش Published.</summary>
public sealed record StoreLandingPagePublicSectionView(
    Guid PageSectionId,
    string SectionType,
    int SortOrder,
    string Config,
    IReadOnlyList<StoreLandingPageResolvedItem> Items);

/// <summary>کارت محصول ویترین برای پاسخ Landing (شکل JSON پایدار).</summary>
public sealed record StoreLandingShellProductCard(
    Guid ProductId,
    string Slug,
    string Title,
    string CategoryName,
    Guid? CategoryId,
    Guid? MediaAssetId,
    Guid PrimaryOfferId,
    Guid SellerPartyId,
    string SellerDisplayName,
    decimal OfferAmountExclusiveOfTax,
    decimal? PromotionalAmountExclusiveOfTax,
    string Currency,
    decimal AvailableUnits,
    bool InStock,
    string? PromotionLabel,
    decimal? AverageRating = null,
    long ReviewCount = 0,
    Guid? BrandId = null,
    Guid? MerchandisingCampaignId = null);

/// <summary>رده ویترین برای پاسخ Landing.</summary>
public sealed record StoreLandingShellCategoryItem(Guid CategoryId, Guid? ParentCategoryId, string Name);

/// <summary>برند ویترین برای پاسخ Landing.</summary>
public sealed record StoreLandingShellBrandItem(
    Guid BrandId,
    string Slug,
    string Name,
    int ProductCount,
    Guid? LogoMediaAssetId = null);

/// <summary>مقاله ویترین برای پاسخ Landing.</summary>
public sealed record StoreLandingShellArticleItem(
    string ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    Guid? CoverMediaAssetId,
    DateTimeOffset PublishDate,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured);

/// <summary>نظر برجسته ویترین برای پاسخ Landing.</summary>
public sealed record StoreLandingShellFeaturedReviewItem(
    string PublicId,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    bool VerifiedPurchase,
    DateTimeOffset CreatedAt,
    string ProductTitle,
    string ProductSlug);

/// <summary>نمای عمومی Published.</summary>
public sealed record StoreLandingPagePublicView(
    Guid PageId,
    string PageType,
    string Locale,
    string Slug,
    string Title,
    string SeoTitle,
    string? SeoDescription,
    bool RobotsIndex,
    bool RobotsFollow,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string PrimaryH1,
    string TemplateKey,
    IReadOnlyList<StoreLandingPagePublicSectionView> Sections,
    IReadOnlyList<StoreLandingShellProductCard> Products,
    IReadOnlyList<StoreLandingShellCategoryItem> Categories,
    IReadOnlyList<StoreLandingShellBrandItem> Brands,
    IReadOnlyList<StoreLandingShellArticleItem> Articles,
    IReadOnlyList<StoreLandingShellFeaturedReviewItem> Reviews);

/// <summary>ورودی sitemap برای Landing ایندکس‌پذیر.</summary>
public sealed record StoreLandingSitemapEntry(string Locale, string Slug, DateTimeOffset UpdatedAt);

/// <summary>ارجاع خانه بدون جایگزینی UI خانه.</summary>
public sealed record StoreHomeSelectionView(
    string StoreScope,
    Guid? HomePageId,
    StoreLandingPagePublicView? SelectedPage,
    bool UsesCanonicalHome);

/// <summary>پاسخ حذف موفق (شکل پایدار { ok: true }).</summary>
public sealed record StoreLandingOkView(bool Ok = true);
