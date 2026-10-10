namespace Tooba.Wishlist.Contracts.Errors;

/// <summary>
/// Stable Wishlist semantic error codes. Identity is the code itself — never a message string and
/// never localized prose. Values are the machine codes emitted by the Wishlist Domain/Application/
/// Infrastructure and mapped by the composed error catalog; they must never be renamed or repurposed.
/// <para>
/// This is the single canonical home for Wishlist stable-code identity. Ownership is unique: every code
/// in <see cref="KnownCodes"/> is Wishlist-owned, registered exactly once by
/// <c>WishlistErrorCatalogContributor</c> and localized by <c>WishlistErrorResourceSet</c>. The module
/// never re-registers a foreign-owned descriptor and no other module registers a Wishlist code.
/// </para>
/// <para>
/// <c>customer.session.required</c> (<see cref="SessionRequired"/>) is a shared Foundation-owned
/// cross-cutting code: its descriptor and both-culture resources belong to
/// <c>FoundationErrorCatalogContributor</c>. The constant stays here so the module's HTTP boundary can
/// consume it, but it is deliberately excluded from <see cref="KnownCodes"/> because it is not a
/// Wishlist use-case fault.
/// </para>
/// </summary>
public static class WishlistErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        ProductUnavailable,
        ProductIdRequired,
        ProductIdsRequired,
        OutboxUnmappedEventType,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>WishlistOperation</c> so Wishlist faults map to
    /// <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched to
    /// the canonical global exception boundary. Classification is by typed code only — never by message
    /// text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>
    /// Shared Foundation-owned customer session code (descriptor and resources are Foundation-owned;
    /// consume it, never re-register it). Deliberately excluded from <see cref="KnownCodes"/>.
    /// </summary>
    public const string SessionRequired = "customer.session.required";

    /// <summary>Target product is missing or not published.</summary>
    public const string ProductUnavailable = "customer.wishlist.product_unavailable";

    /// <summary>Product id path/body value is required.</summary>
    public const string ProductIdRequired = "customer.wishlist.product_id_required";

    /// <summary>Membership product-ids collection is required.</summary>
    public const string ProductIdsRequired = "customer.wishlist.product_ids_required";

    /// <summary>The outbox registration has no mapping for the given integration event type.</summary>
    public const string OutboxUnmappedEventType = "wishlist.outbox.unmapped_event_type";
}
