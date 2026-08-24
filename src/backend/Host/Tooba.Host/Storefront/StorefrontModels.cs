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
