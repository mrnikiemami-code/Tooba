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
