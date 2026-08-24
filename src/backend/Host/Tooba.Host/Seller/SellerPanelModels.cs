namespace Tooba.Host.Seller;

/// <summary>
/// کارت خلاصهٔ داشبورد فروشنده. نمودار جعلی درآمد نیست.
/// </summary>
public sealed record SellerDashboardSummary(
    Guid SellerPartyId,
    string SellerDisplayName,
    int ActiveOffers,
    int OpenOrders,
    int PaidOrders);

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
    int AvailableUnits,
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
    int OnHand,
    int Reserved,
    int AvailableUnits,
    bool CatalogReadOnly);

/// <summary>
/// فرمان باریک به‌روزرسانی seam تجاری فروشنده.
/// </summary>
public sealed record SellerOfferPatchRequest(
    string? SellerSku,
    string? Status);

/// <summary>
/// ردیف فهرست سفارش فروشنده. خطوط فروشندهٔ دیگر دیده نمی‌شود.
/// </summary>
public sealed record SellerOrderListItem(
    Guid SellerOrderId,
    string OrderNumber,
    DateTimeOffset SubmittedAt,
    string RecipientName,
    int LineCount,
    decimal PayableAmount,
    string Currency,
    string PaymentState,
    string Status);

/// <summary>
/// خط سفارش فروشنده با عنوان snapshot/عرضه‌شده.
/// </summary>
public sealed record SellerOrderLineView(
    Guid OfferId,
    string Title,
    int Quantity,
    decimal UnitAmount,
    decimal LinePayable,
    string Currency);

/// <summary>
/// جزئیات سفارش متعلق به همان فروشنده.
/// </summary>
public sealed record SellerOrderDetailPage(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    DateTimeOffset SubmittedAt,
    string Status,
    string PaymentState,
    decimal Subtotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Currency,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string ShippingMethodLabel,
    IReadOnlyList<SellerOrderLineView> Lines);
