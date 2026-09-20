using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Ports;

/// <summary>
/// درز پنل فروشنده برای Offer. پیاده‌سازی Host BFF باقی‌ماندهٔ enrichment بین‌ماژولی را نگه می‌دارد.
/// </summary>
public interface IOfferSellerPanel
{
    /// <summary>فهرست Offerهای فروشنده.</summary>
    Task<IReadOnlyList<SellerOfferListItem>> ListOffersAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>جزئیات Offer متعلق به فروشنده.</summary>
    Task<SellerOfferDetailPage?> GetOfferAsync(Guid sellerPartyId, Guid offerId, CancellationToken cancellationToken);

    /// <summary>به‌روزرسانی باریک Offer متعلق به فروشنده.</summary>
    Task<SellerOfferDetailPage> PatchOfferAsync(Guid sellerPartyId, Guid offerId, SellerOfferPatchRequest patch, CancellationToken cancellationToken);

    /// <summary>ایجاد Offer برای فروشندهٔ احرازشده.</summary>
    Task<SellerOfferDetailPage> CreateOfferAsync(Guid sellerPartyId, SellerOfferCreateRequest request, CancellationToken cancellationToken);

    /// <summary>نوشتن قیمت از طریق Pricing برای Offer متعلق.</summary>
    Task<SellerOfferDetailPage> SetOfferPriceAsync(Guid sellerPartyId, Guid offerId, SellerOfferPriceWriteRequest request, CancellationToken cancellationToken);

    /// <summary>تنظیم موجودی از طریق Inventory برای Offer متعلق.</summary>
    Task<SellerOfferDetailPage> SetOfferInventoryAsync(Guid sellerPartyId, Guid offerId, SellerOfferInventoryWriteRequest request, CancellationToken cancellationToken);
}
