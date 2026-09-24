namespace Tooba.Offer.Contracts.Dtos;

/// <summary>
/// Input for primary-offer selection. The amount comes from Pricing and the available units
/// come from Inventory, so Offer selection never reads Product price.
/// </summary>
/// <param name="OfferId">Owning offer identity.</param>
/// <param name="CatalogVariantId">Catalog variant the offer belongs to.</param>
/// <param name="SellerPartyId">Selling party.</param>
/// <param name="AmountExclusiveOfTax">Tax-exclusive amount quoted for the offer.</param>
/// <param name="AvailableUnits">Sellable units available for the offer.</param>
public sealed record OfferSelectionCandidate(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    decimal AmountExclusiveOfTax,
    decimal AvailableUnits);
