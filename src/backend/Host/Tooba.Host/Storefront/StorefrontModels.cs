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
    Guid SellerPartyId,
    string SellerDisplayName,
    decimal OfferAmountExclusiveOfTax,
    decimal? PromotionalAmountExclusiveOfTax,
    string Currency,
    int AvailableUnits,
    bool InStock,
    string? PromotionLabel,
    decimal? AverageRating = null,
    long ReviewCount = 0);

/// <summary>
/// ردهٔ منتشرشده برای ناوبری فروشگاه. رابطهٔ والد از Catalog می‌آید و مسیر landing همان فیلتر پایدار رده است.
/// </summary>
public sealed record StorefrontCategoryItem(Guid CategoryId, Guid? ParentCategoryId, string Name);

/// <summary>
/// برند Catalog برای نوار برند خانه؛ رسانهٔ CMS نیست.
/// </summary>
public sealed record StorefrontBrandItem(Guid BrandId, string Slug, string Name, int ProductCount);

/// <summary>
/// هویت عمومی فروشنده که از شناسهٔ داخلی Party جداست و فقط دادهٔ لازم برای ویترین را حمل می‌کند.
/// </summary>
public sealed record StorefrontPublicSellerItem(
    string PublicId,
    string DisplayName,
    int ActiveOfferCount,
    int ProductCount);

/// <summary>
/// صفحهٔ عمومی یک برند با کالاهای ترکیب‌شدهٔ همان برند.
/// </summary>
public sealed record StorefrontBrandPage(
    StorefrontBrandItem Brand,
    IReadOnlyList<StorefrontProductCard> Products);

/// <summary>
/// صفحهٔ عمومی فروشنده؛ هیچ شناسه، رابطهٔ مجوز، اطلاعات تماس یا دادهٔ تسویهٔ Party را افشا نمی‌کند.
/// </summary>
public sealed record StorefrontPublicSellerPage(
    StorefrontPublicSellerItem Seller,
    IReadOnlyList<StorefrontProductCard> Products);

/// <summary>
/// پاسخ مسیر merchandising که پشتیبانی یا نبود صادقانهٔ سیگنال را صریح می‌کند.
/// </summary>
public sealed record StorefrontMerchandisingPage(
    string Kind,
    string Title,
    bool Supported,
    string? UnavailableReason,
    IReadOnlyList<StorefrontProductCard> Products);

/// <summary>
/// صفحهٔ خانه با بنر نمایشی ایستا و کارت‌های زندهٔ Catalog/Offer.
/// </summary>
public sealed record StorefrontHomePage(
    IReadOnlyList<StorefrontCategoryItem> Categories,
    IReadOnlyList<StorefrontProductCard> FeaturedProducts,
    IReadOnlyList<StorefrontProductCard> SpecialOffers,
    IReadOnlyList<StorefrontProductCard> CampaignProducts,
    IReadOnlyList<StorefrontProductCard> NewArrivals,
    IReadOnlyList<StorefrontProductCard> ProductRail,
    IReadOnlyList<StorefrontBrandItem> Brands,
    string HeroTitle,
    string HeroSubtitle);

/// <summary>
/// فهرست فروشگاهی فیلترپذیر. منبع حقیقت دمو JSON نیست.
/// </summary>
public sealed record StorefrontListingPage(
    IReadOnlyList<StorefrontCategoryItem> Categories,
    IReadOnlyList<StorefrontSellerFilterItem> Sellers,
    IReadOnlyList<StorefrontProductCard> Products,
    string? Query,
    Guid? CategoryId,
    Guid? SellerPartyId,
    bool? InStock,
    string Sort,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>
/// فروشنده‌ای که واقعاً در نتیجهٔ ترکیب‌شده Offer دارد و بنابراین می‌تواند به‌عنوان facet نمایش داده شود.
/// </summary>
public sealed record StorefrontSellerFilterItem(Guid SellerPartyId, string DisplayName);

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
/// یک مشخصهٔ عمومی و خوانا از Catalog؛ شناسهٔ تعریف یا گزینه را افشا نمی‌کند.
/// </summary>
public sealed record StorefrontProductSpecification(string Label, string Value);

/// <summary>
/// مقدار نمایشی یک محور گونه، مانند «رنگ: مشکی»، بدون افشای شناسهٔ داخلی گزینه.
/// </summary>
public sealed record StorefrontVariantAxis(string Label, string Value);

/// <summary>
/// گونهٔ قابل انتخاب محصول و خلاصهٔ حقیقت خرید آن که در backend از Offer، Pricing و Inventory ترکیب شده است.
/// </summary>
public sealed record StorefrontProductVariant(
    Guid VariantId,
    IReadOnlyList<StorefrontVariantAxis> Axes,
    bool Purchasable,
    StorefrontOfferCandidate? PrimaryOffer,
    decimal? PromotionalAmountExclusiveOfTax,
    string? PromotionLabel);

/// <summary>
/// صفحهٔ جزئیات محصول. گالری از مرجع مات Media است نه باینری Catalog.
/// </summary>
public sealed record StorefrontProductDetailPage(
    Guid ProductId,
    string Slug,
    string Title,
    string? Description,
    string? ShortDescription,
    string? FullDescription,
    string CategoryName,
    string? BrandName,
    IReadOnlyList<Guid> MediaAssetIds,
    IReadOnlyList<StorefrontProductSpecification> Specifications,
    IReadOnlyList<StorefrontProductVariant> Variants,
    Guid SelectedVariantId,
    StorefrontOfferCandidate PrimaryOffer,
    decimal? PromotionalAmountExclusiveOfTax,
    string? PromotionLabel,
    IReadOnlyList<StorefrontAlternateOffer> OtherSellers,
    IReadOnlyList<StorefrontProductCard> RelatedProducts,
    string SeoTitle,
    string SeoDescription,
    bool CartMutationEnabled,
    decimal? AverageRating = null,
    long ReviewCount = 0);

/// <summary>
/// ورودی قطعی انتخاب Offer نمایشی تا UI اولین ردیف جدول را حدس نزند.
/// </summary>
public sealed record StorefrontOfferResolutionInput(
    IReadOnlyList<StorefrontOfferCandidate> Candidates);

/// <summary>
/// خط نمایشی سبد. مبلغ نقل‌قول‌شده از Pricing روی Offer است نه فیلد قیمت Product.
/// </summary>
public sealed record StorefrontCartLineView(
    Guid LineId,
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    Guid? ProductId,
    string? ProductSlug,
    string Title,
    string SellerDisplayName,
    Guid? MediaAssetId,
    int Quantity,
    decimal? UnitAmountExclusiveOfTax,
    decimal? LineAmountExclusiveOfTax,
    string Currency,
    bool QuotedTaxExclusive);

/// <summary>
/// صفحهٔ سبد زنده. جمع‌ها برآورد بدون مالیات از نقل‌قول سبد هستند نه تسویهٔ Checkout.
/// </summary>
public sealed record StorefrontCartPage(
    Guid CartId,
    int Version,
    string Market,
    string Currency,
    string Channel,
    int ItemCount,
    decimal SubtotalExclusiveOfTax,
    IReadOnlyList<StorefrontCartLineView> Lines,
    string? GuestSecret);

/// <summary>
/// ورودی افزودن خط از PDP. هویت خط Offer است.
/// </summary>
public sealed record StorefrontAddCartLineRequest(Guid OfferId, int Quantity);

/// <summary>
/// ورودی تغییر تعداد خط. صفر یعنی حذف.
/// </summary>
public sealed record StorefrontChangeCartLineRequest(int Quantity);

/// <summary>
/// تصویر ارسال فروشگاهی. دفترچهٔ آدرس مشتری پایدار نیست.
/// </summary>
public sealed record StorefrontCheckoutShippingInput(
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode);

/// <summary>
/// ورودی ارسال checkout از سبد زنده.
/// </summary>
public sealed record StorefrontSubmitCheckoutRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string IdempotencyKey,
    StorefrontCheckoutShippingInput Shipping);

/// <summary>
/// خط بازبینی checkout. مبلغ از Order/Tax است نه جمع React.
/// </summary>
public sealed record StorefrontCheckoutLineView(
    Guid OfferId,
    Guid SellerPartyId,
    string Title,
    string SellerDisplayName,
    int Quantity,
    decimal LineExclusiveOfTax,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LinePayable,
    string Currency);

/// <summary>
/// سفارش فروشنده داخل checkout. پرداخت‌شده نیست.
/// </summary>
public sealed record StorefrontSellerOrderView(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    decimal SubtotalExclusiveOfTax,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<StorefrontCheckoutLineView> Lines);

/// <summary>
/// صفحهٔ checkout/تأیید. جمع نهایی از backend است.
/// </summary>
public sealed record StorefrontCheckoutPage(
    Guid? CheckoutId,
    Guid CartId,
    int CartVersion,
    string Market,
    string Currency,
    string Channel,
    string PaymentState,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    decimal SubtotalExclusiveOfTax,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal PayableAmount,
    IReadOnlyList<StorefrontSellerOrderView> SellerOrders);

/// <summary>
/// ورودی شروع پرداخت فروشگاهی. مبلغ در بدنه نیست.
/// </summary>
public sealed record StorefrontInitiatePaymentRequest(Guid CartId, string IdempotencyKey);

/// <summary>
/// نتیجهٔ شروع پرداخت. Redirect به صفحهٔ sandbox/dev است نه بانک واقعی.
/// </summary>
public sealed record StorefrontPaymentInitiationPage(
    Guid PaymentId,
    Guid AttemptId,
    Guid CheckoutId,
    string Status,
    string ProviderCode,
    string ProviderRequestReference,
    string RedirectUrl,
    decimal Amount,
    string Currency);

/// <summary>
/// تصویر خواندنی پرداخت برای صفحهٔ نتیجه. Paid بودن سفارش از همین JSON استنتاج نمی‌شود مگر وضعیت سفارش جدا خوانده شود.
/// </summary>
public sealed record StorefrontPaymentPage(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    string Status,
    string ProviderCode,
    IReadOnlyList<StorefrontPaymentAllocationView> Allocations);

/// <summary>
/// تخصیص نمایشی پرداخت به سفارش فروشنده. تسویه فروشنده نیست.
/// </summary>
public sealed record StorefrontPaymentAllocationView(Guid SellerOrderId, decimal AllocatedAmount, string Currency);

/// <summary>
/// تکمیل sandbox/dev. Outcome موفقیت درگاه نیست؛ Host هنوز Verify می‌کند.
/// </summary>
public sealed record StorefrontSandboxPaymentRequest(
    Guid CartId,
    Guid AttemptId,
    string ProviderRequestReference,
    string Outcome);
