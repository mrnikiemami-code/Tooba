using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;

namespace Tooba.Wallet.Application.Ports;

/// <summary>دایرکتوری کاربردی کیف پول و کارت هدیه.</summary>
public interface IWalletDirectory
{
    /// <summary>خلاصهٔ کیف پول مالک؛ در صورت نبود حساب، حساب Active می‌سازد.</summary>
    Task<WalletSummaryDto> GetOrCreateSummaryForCustomerAsync(Guid customerActorUserId, CancellationToken cancellationToken);

    /// <summary>دفتر صفحه‌بندی‌شدهٔ مالک.</summary>
    Task<WalletLedgerPageDto> ListLedgerForCustomerAsync(
        Guid customerActorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>بازخرید کارت هدیه به کیف پول مالک.</summary>
    Task<GiftCardRedeemResultDto> RedeemGiftCardForCustomerAsync(
        Guid customerActorUserId,
        RedeemGiftCardCommand command,
        CancellationToken cancellationToken);

    /// <summary>فهرست Admin کارت‌ها.</summary>
    Task<GiftCardListPageDto> ListGiftCardsForAdminAsync(AdminGiftCardListQuery query, CancellationToken cancellationToken);

    /// <summary>جزئیات Admin.</summary>
    Task<GiftCardDetailDto?> GetGiftCardForAdminAsync(Guid cardId, CancellationToken cancellationToken);

    /// <summary>صدور کارت.</summary>
    Task<GiftCardIssueResultDto> IssueGiftCardForAdminAsync(
        Guid adminActorUserId,
        IssueGiftCardCommand command,
        CancellationToken cancellationToken);

    /// <summary>ابطال کارت.</summary>
    Task<GiftCardDetailDto> RevokeGiftCardForAdminAsync(Guid cardId, CancellationToken cancellationToken);

    /// <summary>بازرسی کیف پول مشتری توسط Admin.</summary>
    Task<WalletSummaryDto?> GetWalletForAdminAsync(Guid customerActorUserId, CancellationToken cancellationToken);

    /// <summary>دفتر Admin.</summary>
    Task<WalletLedgerPageDto> ListLedgerForAdminAsync(
        Guid customerActorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>تعدیل immutable دفتر توسط Admin.</summary>
    Task<AdminWalletAdjustmentResultDto> AdjustWalletForAdminAsync(
        Guid customerActorUserId,
        Guid adminActorUserId,
        AdminWalletAdjustmentCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// بدهکار اتمی برای پرداخت سفارش؛ IdempotentReplay امن؛ بدون overdraw.
    /// </summary>
    Task<WalletSpendResultDto> SpendForOrderPaymentAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// اعتبار refund به کیف پول؛ یک‌بار برای هر ReturnRequestId.
    /// </summary>
    Task<WalletCreditResultDto> CreditRefundAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid returnRequestId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>نقل قول موجودی در برابر مبلغ قابل پرداخت سفارش.</summary>
    Task<WalletCheckoutQuoteDto> QuoteForPayableAsync(
        Guid customerActorId,
        decimal payableAmount,
        string currency,
        CancellationToken cancellationToken);
}
