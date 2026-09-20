namespace Tooba.Offer.Contracts.Dtos;

/// <summary>
/// Seller/status slice used by admin seller listings without EF entities.
/// </summary>
public sealed record OfferSellerStatusRow(Guid SellerPartyId, OfferStatus Status);

/// <summary>
/// Offer list projection including UpdatedAt for merchandising candidate ordering.
/// </summary>
public sealed record OfferListItem(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    SalesChannel Channel,
    OfferStatus Status,
    string? SellerSku,
    DateTimeOffset UpdatedAt);
