namespace Tooba.Cart.Contracts.Errors;

/// <summary>
/// Stable Cart semantic error codes for HTTP/use-case outcomes.
/// Identity is the code itself — never a message string and never localized prose.
/// </summary>
public static class CartErrorCodes
{
    /// <summary>Cart was not found.</summary>
    public const string Missing = "cart.missing";

    /// <summary>Guest secret is invalid or access is denied.</summary>
    public const string GuestInvalid = "cart.guest.invalid";

    /// <summary>Authenticated/guest access denied.</summary>
    public const string AccessDenied = "cart.access.denied";

    /// <summary>Optimistic concurrency version conflict.</summary>
    public const string VersionConflict = "cart.version.conflict";

    /// <summary>Cart has expired.</summary>
    public const string Expired = "cart.expired";

    /// <summary>Requested quantity is invalid.</summary>
    public const string QuantityInvalid = "cart.quantity.invalid";

    /// <summary>Cart line was not found.</summary>
    public const string LineMissing = "cart.line.missing";

    /// <summary>A quoted line has no currency truth; unlike currencies are never assumed.</summary>
    public const string LineCurrencyMissing = "cart.line.currency_missing";

    /// <summary>Offer cannot be added to the cart.</summary>
    public const string OfferUnavailable = "cart.offer.unavailable";

    /// <summary>Sellable inventory is insufficient.</summary>
    public const string InventoryInsufficient = "cart.inventory.insufficient";

    /// <summary>Inventory hold/availability became stale.</summary>
    public const string InventoryStale = "cart.inventory.stale";

    /// <summary>Generic rejected cart mutation.</summary>
    public const string Rejected = "cart.rejected";

    /// <summary>Authenticated session required for this cart route (shared checkout-boundary code).</summary>
    public const string AuthenticationRequired = "checkout.authentication_required";

    /// <summary>A cart must always carry an owner user id when it is authenticated.</summary>
    public const string UserIdRequired = "cart.user_id.required";

    /// <summary>A guest cart must always carry a credential hash.</summary>
    public const string GuestCredentialHashRequired = "cart.guest_secret.hash_required";

    /// <summary>Duplicate offer lines are merged through the quantity path, never appended.</summary>
    public const string LineMergeViaQuantity = "cart.line.merge_via_quantity";

    /// <summary>Conversion must record a concrete order path.</summary>
    public const string ConversionOrderRequired = "cart.convert.order_required";

    /// <summary>Only a guest cart can adopt an authenticated owner.</summary>
    public const string AdoptGuestOnly = "cart.assign.guest_only";

    /// <summary>Cart expiry must be in the future.</summary>
    public const string ExpiryFutureRequired = "cart.expiry.future_required";

    /// <summary>Cart expiry must be after creation.</summary>
    public const string ExpiryAfterCreated = "cart.expiry.after_created";

    /// <summary>Cart must carry a commercial market.</summary>
    public const string MarketRequired = "cart.market.required";

    /// <summary>Cart default currency selection must be a shaped 3-character code.</summary>
    public const string CurrencyInvalid = "cart.currency.invalid";

    /// <summary>Pricing returned no quote for the selected offer/market/channel/currency.</summary>
    public const string PricingQuoteMissing = "cart.pricing.quote_missing";

    /// <summary>Platform store-commerce context has not been resolved for this request/worker.</summary>
    public const string CommerceContextUnavailable = "cart.commerce.context_unavailable";

    /// <summary>Platform store-commerce context carries no market.</summary>
    public const string CommerceMarketUnconfigured = "cart.commerce.market_unconfigured";

    /// <summary>Platform store-commerce context carries no default currency.</summary>
    public const string CommerceCurrencyUnconfigured = "cart.commerce.currency_unconfigured";

    /// <summary>Platform store-commerce context carries no parseable sales channel.</summary>
    public const string CommerceChannelUnconfigured = "cart.commerce.channel_unconfigured";
}
