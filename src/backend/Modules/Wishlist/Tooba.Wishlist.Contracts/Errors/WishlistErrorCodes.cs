namespace Tooba.Wishlist.Contracts.Errors;

/// <summary>Stable semantic error codes owned by Wishlist.</summary>
public static class WishlistErrorCodes
{
    /// <summary>A trusted customer session is required (shared foundation code; do not re-register).</summary>
    public const string SessionRequired = "customer.session.required";

    /// <summary>Target product is missing or not published.</summary>
    public const string ProductUnavailable = "customer.wishlist.product_unavailable";

    /// <summary>Product id path/body value is required.</summary>
    public const string ProductIdRequired = "customer.wishlist.product_id_required";

    /// <summary>Membership product-ids collection is required.</summary>
    public const string ProductIdsRequired = "customer.wishlist.product_ids_required";
}
