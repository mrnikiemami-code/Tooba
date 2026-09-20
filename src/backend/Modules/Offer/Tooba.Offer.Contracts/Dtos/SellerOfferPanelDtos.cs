namespace Tooba.Offer.Contracts;

/// <summary>
/// ردیف فهرست Offer فروشنده. Product.Price و Product.Stock ندارد.
/// </summary>
public sealed record SellerOfferListItem(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid? ProductId,
    string ProductTitle,
    string? SellerSku,
    string Status,
    decimal? Amount,
    string Currency,
    decimal AvailableUnits,
    DateTimeOffset? LastUpdatedAt);

/// <summary>
/// جزئیات Offer فروشنده با زمینهٔ فقط‌خواندنی Catalog.
/// </summary>
public sealed record SellerOfferDetailPage(
    Guid OfferId,
    Guid SellerPartyId,
    string SellerDisplayName,
    Guid CatalogVariantId,
    Guid? ProductId,
    string ProductTitle,
    string? BrandName,
    string? SellerSku,
    string Status,
    string Channel,
    decimal? Amount,
    string Currency,
    decimal OnHand,
    decimal Reserved,
    decimal AvailableUnits,
    bool CatalogReadOnly,
    string ReturnPolicyChoice = "Default",
    int? CustomReturnWindowDays = null,
    int DefaultReturnWindowDays = 7,
    bool SellerCanOverrideReturnPolicy = true,
    int MinReturnWindowDays = 1,
    int MaxReturnWindowDays = 30,
    bool AllowNonReturnableOffers = true,
    decimal? MinimumOrderQuantity = null,
    decimal? MaximumOrderQuantity = null,
    string? ProductUnitCode = null,
    string? ProductUnitName = null,
    string? ProductUnitShortName = null);

/// <summary>
/// فرمان باریک به‌روزرسانی seam تجاری فروشنده.
/// </summary>
public sealed record SellerOfferPatchRequest(
    string? SellerSku,
    string? Status,
    string? ReturnPolicyChoice = null,
    int? CustomReturnWindowDays = null,
    decimal? MinimumOrderQuantity = null,
    decimal? MaximumOrderQuantity = null);

/// <summary>
/// فرمان ایجاد Offer روی گونهٔ Catalog؛ Party فروشنده فقط از زمینهٔ احراز می‌آید نه از بدنه.
/// </summary>
public sealed record SellerOfferCreateRequest(
    Guid CatalogVariantId,
    string? SellerSku,
    string? Status,
    string? ReturnPolicyChoice = null,
    int? CustomReturnWindowDays = null);

/// <summary>
/// فرمان نوشتن مبلغ بدون مالیات روی Offer متعلق به همان فروشنده از طریق Pricing.
/// </summary>
public sealed record SellerOfferPriceWriteRequest(
    decimal Amount,
    string? Currency,
    string? Market);

/// <summary>
/// فرمان تنظیم موجودی روی‌دست Offer از طریق Inventory؛ Product.Stock نیست.
/// </summary>
public sealed record SellerOfferInventoryWriteRequest(
    decimal OnHand,
    string? Reason);
