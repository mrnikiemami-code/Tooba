namespace Tooba.Order.Contracts.Payments;

/// <summary>
/// Read-only outcome of a pre-order, cart-scoped payable quotation. A cart fault is a typed status —
/// never a message — so the consuming module maps it onto its own stable public outcome.
/// </summary>
public enum CartPaymentQuoteStatus
{
    /// <summary>An Active cart with a persisted shipping selection was valued authoritatively.</summary>
    Ready = 0,

    /// <summary>Cart does not exist or is not visible to this actor (guest secret / authenticated buyer).</summary>
    CartMissing = 1,

    /// <summary>Cart exists but is owned by another actor.</summary>
    CartForbidden = 2,

    /// <summary>Cart is Active but has no lines.</summary>
    CartEmpty = 3,

    /// <summary>Cart has no persisted shipping selection — the pre-order draft cannot be valued yet.</summary>
    ShippingSelectionRequired = 4,

    /// <summary>Cart is no longer Active (converted/expired/stale) or its version moved on.</summary>
    CartNotActive = 5,

    /// <summary>Any other typed Order value fault; callers must fail closed.</summary>
    Unavailable = 6,
}

/// <summary>
/// Server-authoritative pre-order payable snapshot for one Active cart. Amounts are computed by Order
/// (the same authoritative commerce evaluation as checkout preview) and are never recomputed by
/// Payment or the browser. No order, reservation, hold or cart conversion exists for this snapshot.
/// </summary>
public sealed record CartPaymentQuoteSnapshot(
    Guid CartId,
    int CartVersion,
    string Currency,
    decimal SubtotalExclusiveOfTax,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal PayableAmount,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    int SellerOrderCount);

/// <summary>Typed result of a cart payable quotation: either a snapshot or a fail-closed status.</summary>
public sealed record CartPaymentQuoteResult(
    CartPaymentQuoteStatus Status,
    CartPaymentQuoteSnapshot? Snapshot)
{
    /// <summary>True only when the snapshot is authoritative and fully materialized.</summary>
    public bool IsReady => Status == CartPaymentQuoteStatus.Ready && Snapshot is not null;

    /// <summary>Ready result carrying the authoritative snapshot.</summary>
    public static CartPaymentQuoteResult Ready(CartPaymentQuoteSnapshot snapshot) =>
        new(CartPaymentQuoteStatus.Ready, snapshot);

    /// <summary>Fail-closed result carrying only a stable status.</summary>
    public static CartPaymentQuoteResult Failed(CartPaymentQuoteStatus status) => new(status, null);
}

/// <summary>
/// Contracts-only read port: the pre-order payable amount of one Active cart, for tender quotation
/// (for example wallet coverage) before any order commit. Implementations must not create, convert,
/// reserve or mutate anything; a missing/foreign/stale cart fails closed with a typed status.
/// </summary>
public interface ICartPaymentQuoteReader
{
    /// <summary>
    /// Values the cart the actor owns. <paramref name="guestSecret"/> is the canonical guest proof for
    /// anonymous carts; authenticated ownership is proven by the caller's trusted actor boundary.
    /// </summary>
    Task<CartPaymentQuoteResult> GetForCartAsync(
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken);
}
