namespace Tooba.Order.Domain.Aggregates;

/// <summary>
/// TB-265-R1: the DURABLE Order-owned closure of the remaining customer delivery obligation of one order line,
/// recorded when an approved courier-origin Returns case covers that exact quantity.
/// <para>
/// It is a fact, not a calculation: the row remembers the case, the Fulfillment source claim, the reviewing actor and
/// the exact quantity. It never decrements the ordered/packed/shipped history (a returned courier parcel is not an
/// "unshipped" parcel) and it never releases stock — the owning Inventory seam releases only the unconsumed hold.
/// </para>
/// </summary>
public sealed class OrderLineObligationClosureRecord
{
    private OrderLineObligationClosureRecord()
    {
    }

    /// <summary>Stable identity of this closure fact.</summary>
    public Guid ClosureId { get; init; }

    /// <summary>Owning seller order.</summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>Owning checkout.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>The order line whose obligation is closed.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>The approved Returns case that owns this closure.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>The Fulfillment-owned source-quantity claim that funds the case.</summary>
    public Guid SourceClaimId { get; init; }

    /// <summary>Closed quantity of that line.</summary>
    public decimal Quantity { get; init; }

    /// <summary>Reviewing operator (server-side identity, never a client boolean).</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>When the closure became durable.</summary>
    public DateTimeOffset ClosedAt { get; init; }

    /// <summary>Creates the durable closure fact of one approved case line.</summary>
    public static OrderLineObligationClosureRecord Create(
        Guid closureId,
        Guid sellerOrderId,
        Guid checkoutId,
        Guid orderLineId,
        Guid returnRequestId,
        Guid sourceClaimId,
        decimal quantity,
        Guid actorUserId,
        DateTimeOffset closedAt) =>
        new()
        {
            ClosureId = closureId,
            SellerOrderId = sellerOrderId,
            CheckoutId = checkoutId,
            OrderLineId = orderLineId,
            ReturnRequestId = returnRequestId,
            SourceClaimId = sourceClaimId,
            Quantity = quantity,
            ActorUserId = actorUserId,
            ClosedAt = closedAt,
        };
}
