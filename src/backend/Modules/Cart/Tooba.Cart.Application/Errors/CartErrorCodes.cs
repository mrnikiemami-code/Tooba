namespace Tooba.Cart.Application.Errors;

/// <summary>Stable Cart semantic error codes for HTTP/use-case outcomes.</summary>
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

    /// <summary>Authenticated session required for this cart route.</summary>
    public const string AuthenticationRequired = "checkout.authentication_required";
}
