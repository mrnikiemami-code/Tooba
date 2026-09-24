namespace Tooba.Cart.Contracts;

/// <summary>Storefront cart line presentation. Quoted amounts come from Pricing via Cart snapshot.</summary>
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
/// Total of one line currency group. Amounts of unlike currencies are never summed together.
/// </summary>
public sealed record CartCurrencyTotal(string Currency, decimal SubtotalExclusiveOfTax);

/// <summary>
/// Live cart page. Totals are tax-exclusive estimates from Cart quotes, not Checkout settlement.
/// <c>DefaultCurrency</c> is default-selection metadata only; per-currency truth lives in
/// <see cref="TotalsByCurrency"/> and in each <see cref="CartLineView.Currency"/>.
/// There is intentionally no single scalar page-level subtotal: unlike currencies are never summed.
/// </summary>
public sealed record CartPage(
    Guid CartId,
    int Version,
    string Market,
    string DefaultCurrency,
    string Channel,
    decimal ItemCount,
    IReadOnlyList<CartCurrencyTotal> TotalsByCurrency,
    IReadOnlyList<CartLineView> Lines,
    string? GuestSecret,
    string Status = "Active");

/// <summary>
/// Cart presentation reads for checkout-adjacent Order surfaces (no Cart.Application dependency).
/// </summary>
public interface ICartPresentationGateway
{
    /// <summary>Loads and presents a cart after access checks. Null when the cart id is unknown.</summary>
    Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken);

    /// <summary>
    /// Ownership probe for committed checkout: invalid guest secret yields null (not an exception).
    /// </summary>
    Task<CartPage?> TryGetForOwnershipAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken);
}
