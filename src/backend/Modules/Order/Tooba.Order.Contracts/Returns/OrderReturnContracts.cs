namespace Tooba.Order.Contracts.Returns;

/// <summary>
/// snapshot خط سفارش برای مرجوعی.
/// </summary>
public sealed record OrderReturnLineSnapshot(
    Guid OrderLineId,
    decimal Quantity,
    decimal UnitPriceSnapshot,
    string Currency,
    Guid? ReservationId,
    bool IsReturnableSnapshot = true,
    int ReturnWindowDaysSnapshot = 7,
    string? ReturnPolicySourceSnapshot = null,
    string? ReturnPolicyLabelSnapshot = null);

/// <summary>
/// snapshot سفارش برای eligibility مرجوعی.
/// </summary>
public sealed record OrderReturnContextSnapshot(
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    Guid PlacedByUserId,
    bool IsPaid,
    string Currency,
    IReadOnlyList<OrderReturnLineSnapshot> Lines);

/// <summary>
/// خواندن snapshot سفارش برای Returns بدون cross-DbContext.
/// </summary>
public interface IOrderReturnReader
{
    /// <summary>
    /// snapshot سفارش را برای مرجوعی برمی‌گرداند.
    /// </summary>
    Task<OrderReturnContextSnapshot?> GetReturnContextAsync(Guid sellerOrderId, CancellationToken cancellationToken);
}

/// <summary>
/// TB-265: a narrowly authorized request to close the remaining delivery obligation of ONE order line because an
/// approved courier-origin return case covers that quantity.
/// </summary>
public sealed record OrderLineObligationClosureRequest(
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid OrderLineId,
    decimal Quantity,
    Guid ReturnRequestId,
    Guid SourceClaimId,
    Guid ActorUserId);

/// <summary>
/// TB-265: authoritative answer of the obligation closure. It reports the exact held reservation of that line so the
/// caller can release only its unconsumed quantity once; it never decrements ordered/packed/shipped history.
/// </summary>
public sealed record OrderLineObligationClosureResult(
    bool Closed,
    Guid OrderLineId,
    decimal ClosedQuantity,
    decimal RemainingDeliverableQuantity,
    Guid? HeldReservationId = null,
    string? BlockerCode = null,
    Guid? ClosureId = null);

/// <summary>
/// TB-265-R1: the durable Order-owned closure fact of one approved courier-origin case line. It is a real row in
/// <c>order.order_line_obligation_closures</c>, not a computed number: a retried approval answers the same row, and the
/// cumulative closed quantity of a line never exceeds what the line still owed.
/// </summary>
public sealed record OrderLineObligationClosureSnapshot(
    Guid ClosureId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid OrderLineId,
    Guid ReturnRequestId,
    Guid SourceClaimId,
    decimal Quantity,
    Guid ActorUserId,
    DateTimeOffset ClosedAt);

/// <summary>
/// TB-265: Order-owned authority over the remaining customer delivery obligation of a line. Returns never decides
/// this for itself: it asks here, and only a Closed answer may lead to the money step.
/// </summary>
public interface IOrderReturnObligationClosure
{
    /// <summary>Validates and closes the obligation of one line for the given returned quantity.</summary>
    Task<OrderLineObligationClosureResult> CloseLineObligationAsync(
        OrderLineObligationClosureRequest request,
        CancellationToken cancellationToken);

    /// <summary>Reads the durable closure recorded for this exact case line (null when nothing was closed yet).</summary>
    Task<OrderLineObligationClosureSnapshot?> GetClosureAsync(
        Guid returnRequestId,
        Guid orderLineId,
        CancellationToken cancellationToken);

    /// <summary>Total quantity already durably closed for this order line across every approved case.</summary>
    Task<decimal> GetClosedQuantityAsync(Guid orderLineId, CancellationToken cancellationToken);
}
