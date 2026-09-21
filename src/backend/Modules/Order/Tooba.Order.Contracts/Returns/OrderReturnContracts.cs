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
