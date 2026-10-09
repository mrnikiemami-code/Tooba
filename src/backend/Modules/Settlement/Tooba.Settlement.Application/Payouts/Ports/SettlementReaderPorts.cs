namespace Tooba.Settlement.Application.Payouts.Ports;

/// <summary>
/// snapshot سفارش برای تسویه. FK Order نیست.
/// </summary>
public sealed record SettlementOrderSnapshot(
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    bool IsPaid,
    string Currency);

/// <summary>
/// snapshot تخصیص پرداخت برای accrual.
/// </summary>
public sealed record SettlementPaymentAllocationSnapshot(
    Guid SellerOrderId,
    decimal AllocatedAmount,
    string Currency);

/// <summary>
/// snapshot پرداخت برای تسویه.
/// </summary>
public sealed record SettlementPaymentSnapshot(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    bool IsSucceeded);

/// <summary>
/// snapshot refund برای adjustment.
/// </summary>
public sealed record SettlementRefundSnapshot(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    decimal RefundAmount,
    string Currency);

/// <summary>
/// خواندن snapshot سفارش بدون DbContext Order.
/// </summary>
public interface ISettlementOrderReader
{
    /// <summary>snapshot سفارش را برمی‌گرداند.</summary>
    Task<SettlementOrderSnapshot?> GetAsync(Guid sellerOrderId, CancellationToken cancellationToken);
}

/// <summary>
/// خواندن snapshot پرداخت بدون DbContext Payment.
/// </summary>
public interface ISettlementPaymentReader
{
    /// <summary>snapshot پرداخت را برمی‌گرداند.</summary>
    Task<SettlementPaymentSnapshot?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>تخصیص‌های پرداخت را برمی‌گرداند.</summary>
    Task<IReadOnlyList<SettlementPaymentAllocationSnapshot>> GetAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken);
}

/// <summary>
/// خواندن snapshot refund بدون DbContext Returns.
/// </summary>
public interface ISettlementReturnsReader
{
    /// <summary>snapshot refund را برمی‌گرداند.</summary>
    Task<SettlementRefundSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken);
}
