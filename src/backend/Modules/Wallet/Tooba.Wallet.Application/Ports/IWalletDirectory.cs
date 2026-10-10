using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Payments.Models;
using Tooba.Wallet.Application.Refunds.Models;
using Tooba.Wallet.Contracts.Payments;

namespace Tooba.Wallet.Application.Ports;

/// <summary>
/// Wallet capability directory owned by the Infrastructure layer. Contract identity only: the
/// Infrastructure implementation and every consumer depend on this abstraction, so the module can be
/// extracted as an isolated microservice without leaking persistence into callers.
/// </summary>
public interface IWalletDirectory
{
    /// <summary>Owner wallet summary; creates an Active account when none exists.</summary>
    Task<WalletSummaryDto> GetOrCreateSummaryForCustomerAsync(Guid customerActorUserId, CancellationToken cancellationToken);

    /// <summary>Paged owner ledger.</summary>
    Task<WalletLedgerPageDto> ListLedgerForCustomerAsync(
        Guid customerActorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Gift-card redemption into the owner wallet.</summary>
    Task<GiftCardRedeemResultDto> RedeemGiftCardForCustomerAsync(
        Guid customerActorUserId,
        RedeemGiftCardCommand command,
        CancellationToken cancellationToken);

    /// <summary>Admin gift-card list.</summary>
    Task<GiftCardListPageDto> ListGiftCardsForAdminAsync(AdminGiftCardListQuery query, CancellationToken cancellationToken);

    /// <summary>Admin gift-card detail.</summary>
    Task<GiftCardDetailDto?> GetGiftCardForAdminAsync(Guid cardId, CancellationToken cancellationToken);

    /// <summary>Issues a gift card.</summary>
    Task<GiftCardIssueResultDto> IssueGiftCardForAdminAsync(
        Guid adminActorUserId,
        IssueGiftCardCommand command,
        CancellationToken cancellationToken);

    /// <summary>Revokes a gift card.</summary>
    Task<GiftCardDetailDto> RevokeGiftCardForAdminAsync(Guid cardId, CancellationToken cancellationToken);

    /// <summary>Admin inspection of a customer wallet.</summary>
    Task<WalletSummaryDto?> GetWalletForAdminAsync(Guid customerActorUserId, CancellationToken cancellationToken);

    /// <summary>Admin ledger page.</summary>
    Task<WalletLedgerPageDto> ListLedgerForAdminAsync(
        Guid customerActorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Immutable ledger adjustment by Admin.</summary>
    Task<AdminWalletAdjustmentResultDto> AdjustWalletForAdminAsync(
        Guid customerActorUserId,
        Guid adminActorUserId,
        AdminWalletAdjustmentCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomic debit for an order payment; idempotent replay safe; no overdraw.
    /// </summary>
    Task<WalletSpendResultDto> SpendForOrderPaymentAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Credits a refund into the wallet; once per ReturnRequestId.
    /// </summary>
    Task<WalletCreditResultDto> CreditRefundAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid returnRequestId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>Quotes the balance against an order payable amount.</summary>
    Task<WalletCheckoutQuoteDto> QuoteForPayableAsync(
        Guid customerActorId,
        decimal payableAmount,
        string currency,
        CancellationToken cancellationToken);
}
