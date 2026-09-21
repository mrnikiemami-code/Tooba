namespace Tooba.Wallet.Contracts.Payments;

/// <summary>نقل قول قابل استفاده بودن کیف پول برای مبلغ قابل پرداخت.</summary>
public sealed record WalletCheckoutQuoteDto(
    decimal WalletBalance,
    decimal MaxUsable,
    decimal RemainingPayable,
    bool CanPayFullyWithWallet,
    string Currency);

/// <summary>نتیجهٔ بدهکار پرداخت سفارش از دید مرز Payment (بدون نشت Ledger Entry).</summary>
public sealed record WalletOrderPaymentDebitResultDto(
    decimal Balance,
    bool IdempotentReplay);

/// <summary>
/// درز پرداخت سفارش از کیف پول برای Payment.Infrastructure.
/// پیاده‌سازی در Wallet.Infrastructure؛ بدون افشای Domain/DbContext.
/// </summary>
public interface IWalletOrderPaymentPort
{
    /// <summary>بدهکار اتمی برای پرداخت سفارش؛ IdempotentReplay امن؛ بدون overdraw.</summary>
    Task<WalletOrderPaymentDebitResultDto> SpendForOrderPaymentAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>نقل قول موجودی در برابر مبلغ قابل پرداخت سفارش.</summary>
    Task<WalletCheckoutQuoteDto> QuoteForPayableAsync(
        Guid customerActorId,
        decimal payableAmount,
        string currency,
        CancellationToken cancellationToken);
}
