using Tooba.Offer.Domain;

namespace Tooba.Offer.Contracts;

/// <summary>
/// مرجع پایدار Offer بدون نشت EF. مبلغ و موجودی ندارد.
/// </summary>
public sealed record OfferReference(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    SalesChannel Channel,
    OfferStatus Status,
    string? SellerSku,
    string ReturnPolicyChoice = "Default",
    int? CustomReturnWindowDays = null,
    decimal? MinimumOrderQuantity = null,
    decimal? MaximumOrderQuantity = null);

/// <summary>
/// درز خواندن Offer برای Pricing/Inventory/Cart/Order/Promotion.
/// </summary>
public interface IOfferLookupGateway
{
    /// <summary>Offer را پیدا می‌کند؛ Host parse نمی‌شود.</summary>
    Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>چند Offer را در یک خواندن برمی‌گرداند.</summary>
    Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);

    /// <summary>تعداد Offerهای وابسته به هر CatalogVariant.</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);
}
