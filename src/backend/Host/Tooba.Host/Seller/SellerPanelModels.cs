global using SellerOfferListItem = Tooba.Offer.Contracts.SellerOfferListItem;
global using SellerOfferDetailPage = Tooba.Offer.Contracts.SellerOfferDetailPage;
global using SellerOfferPatchRequest = Tooba.Offer.Contracts.SellerOfferPatchRequest;
global using SellerOfferCreateRequest = Tooba.Offer.Contracts.SellerOfferCreateRequest;
global using SellerOfferPriceWriteRequest = Tooba.Offer.Contracts.SellerOfferPriceWriteRequest;
global using SellerOfferInventoryWriteRequest = Tooba.Offer.Contracts.SellerOfferInventoryWriteRequest;

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
/// گزینهٔ انتخاب گونهٔ Catalog منتشرشده برای ایجاد Offer؛ فقط‌خواندنی است.
/// </summary>
public sealed record SellerCatalogVariantOption(
    Guid CatalogVariantId,
    Guid ProductId,
    string ProductTitle,
    string? CatalogCode,
    string ProductStatus);

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
    decimal Quantity,
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
