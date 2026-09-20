namespace Tooba.Offer.Contracts.Dtos;

/// <summary>
/// Stable public offer reference without persistence details.
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
