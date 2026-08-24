namespace Tooba.Host.Storefront;

/// <summary>
/// کاندیدای انتخاب Offer نمایشی. مبلغ از Pricing و موجودی از Inventory می‌آید نه از Product.
/// </summary>
public sealed record StorefrontOfferCandidate(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    string SellerDisplayName,
    string? SellerSku,
    decimal AmountExclusiveOfTax,
    string Currency,
    string Market,
    int AvailableUnits,
    string TaxCategoryLabel);

/// <summary>
/// کارت فهرست/خانه. فیلد Price روی هویت Product وجود ندارد.
/// </summary>
public sealed record StorefrontProductCard(
    Guid ProductId,
    string Slug,
    string Title,
    string CategoryName,
    Guid? CategoryId,
    Guid? MediaAssetId,
    Guid PrimaryOfferId,
    string SellerDisplayName,
    decimal OfferAmountExclusiveOfTax,
    string Currency,
    int AvailableUnits,
    bool InStock,
    string? PromotionLabel);

/// <summary>
/// ردهٔ منتشرشده برای ناوبری فروشگاه. درخت CMS نیست.
/// </summary>
public sealed record StorefrontCategoryItem(Guid CategoryId, string Name);

/// <summary>
/// صفحهٔ خانه با بنر نمایشی ایستا و کارت‌های زندهٔ Catalog/Offer.
/// </summary>
public sealed record StorefrontHomePage(
    IReadOnlyList<StorefrontCategoryItem> Categories,
    IReadOnlyList<StorefrontProductCard> FeaturedProducts,
    string HeroTitle,
    string HeroSubtitle);

/// <summary>
/// فهرست فروشگاهی فیلترپذیر. منبع حقیقت دمو JSON نیست.
/// </summary>
public sealed record StorefrontListingPage(
    IReadOnlyList<StorefrontCategoryItem> Categories,
    IReadOnlyList<StorefrontProductCard> Products,
    string? Query,
    Guid? CategoryId);

/// <summary>
/// Offer غیر اصلی روی PDP برای نمایش فروشندگان دیگر.
/// </summary>
public sealed record StorefrontAlternateOffer(
    Guid OfferId,
    string SellerDisplayName,
    decimal AmountExclusiveOfTax,
    string Currency,
    int AvailableUnits,
    bool InStock);

/// <summary>
/// صفحهٔ جزئیات محصول. گالری از مرجع مات Media است نه باینری Catalog.
/// </summary>
public sealed record StorefrontProductDetailPage(
    Guid ProductId,
    string Slug,
    string Title,
    string? Description,
    string CategoryName,
    string? BrandName,
    IReadOnlyList<Guid> MediaAssetIds,
    Guid SelectedVariantId,
    StorefrontOfferCandidate PrimaryOffer,
    IReadOnlyList<StorefrontAlternateOffer> OtherSellers,
    string SeoTitle,
    string SeoDescription,
    bool CartMutationEnabled);

/// <summary>
/// ورودی قطعی انتخاب Offer نمایشی تا UI اولین ردیف جدول را حدس نزند.
/// </summary>
public sealed record StorefrontOfferResolutionInput(
    IReadOnlyList<StorefrontOfferCandidate> Candidates);
