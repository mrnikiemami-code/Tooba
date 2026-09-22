namespace Tooba.Cart.Application.Models;

/// <summary>
/// Storefront cart line presentation. Quoted amounts come from Pricing via Cart snapshot.
/// </summary>
public sealed record CartLineView(
    Guid LineId,
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    Guid? ProductId,
    string? ProductSlug,
    string Title,
    string SellerDisplayName,
    Guid? MediaAssetId,
    decimal Quantity,
    decimal? UnitAmountExclusiveOfTax,
    decimal? LineAmountExclusiveOfTax,
    string Currency,
    bool QuotedTaxExclusive,
    string? UnitCode = null,
    string? UnitDisplayName = null,
    int QuantityDecimalPlaces = 0,
    decimal? QuantityStep = null,
    string Availability = "Available",
    Guid? MerchandisingCampaignId = null);

/// <summary>
/// Live cart page. Totals are tax-exclusive estimates from Cart quotes, not Checkout settlement.
/// </summary>
public sealed record CartPage(
    Guid CartId,
    int Version,
    string Market,
    string Currency,
    string Channel,
    decimal ItemCount,
    decimal SubtotalExclusiveOfTax,
    IReadOnlyList<CartLineView> Lines,
    string? GuestSecret,
    string Status = "Active");
