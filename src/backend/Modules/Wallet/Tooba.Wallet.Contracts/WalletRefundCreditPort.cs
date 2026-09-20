namespace Tooba.Wallet.Contracts;

/// <summary>نتیجهٔ اعتبار refund به کیف پول از دید مرز Returns (بدون نشت Ledger Entry).</summary>
public sealed record WalletRefundCreditResultDto(
    decimal Balance,
    bool IdempotentReplay);

/// <summary>
/// درز اعتبار refund کیف پول برای Returns.Infrastructure.
/// پیاده‌سازی در Wallet.Infrastructure؛ بدون افشای Domain/DbContext.
/// </summary>
public interface IWalletRefundCreditPort
{
    /// <summary>اعتبار refund به کیف پول؛ یک‌بار برای هر ReturnRequestId.</summary>
    Task<WalletRefundCreditResultDto> CreditRefundAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid returnRequestId,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
