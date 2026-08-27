namespace Tooba.Returns.Application;

/// <summary>
/// snapshot refund برای ماژول‌های مصرف‌کننده مثل Settlement.
/// </summary>
public sealed record ReturnSettlementSnapshot(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    decimal RefundAmount,
    string Currency);

/// <summary>
/// خواندن snapshot refund بدون cross-DbContext.
/// </summary>
public interface IReturnSettlementReader
{
    /// <summary>snapshot refund را برمی‌گرداند.</summary>
    Task<ReturnSettlementSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken);
}
