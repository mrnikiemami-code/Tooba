using Tooba.Payment.Domain;

namespace Tooba.Payment.Application;

/// <summary>
/// snapshot تخصیص پرداخت برای ماژول‌های مصرف‌کننده مثل Settlement.
/// </summary>
public sealed record PaymentSettlementAllocationSnapshot(
    Guid SellerOrderId,
    decimal AllocatedAmount,
    string Currency);

/// <summary>
/// snapshot پرداخت برای تسویه.
/// </summary>
public sealed record PaymentSettlementSnapshot(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    PaymentStatus Status);

/// <summary>
/// خواندن snapshot پرداخت بدون cross-DbContext.
/// </summary>
public interface IPaymentSettlementReader
{
    /// <summary>snapshot پرداخت را برمی‌گرداند.</summary>
    Task<PaymentSettlementSnapshot?> GetAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>تخصیص‌های پرداخت را برمی‌گرداند.</summary>
    Task<IReadOnlyList<PaymentSettlementAllocationSnapshot>> GetAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken);
}
