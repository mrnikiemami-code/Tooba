global using SellerOfferListItem = Tooba.Offer.Contracts.Dtos.SellerOfferListItem;
global using SellerOfferDetailPage = Tooba.Offer.Contracts.Dtos.SellerOfferDetailPage;
global using SellerOfferPatchRequest = Tooba.Offer.Contracts.Dtos.SellerOfferPatchRequest;
global using SellerOfferCreateRequest = Tooba.Offer.Contracts.Dtos.SellerOfferCreateRequest;
global using SellerOfferPriceWriteRequest = Tooba.Offer.Contracts.Dtos.SellerOfferPriceWriteRequest;
global using SellerOfferInventoryWriteRequest = Tooba.Offer.Contracts.Dtos.SellerOfferInventoryWriteRequest;

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
