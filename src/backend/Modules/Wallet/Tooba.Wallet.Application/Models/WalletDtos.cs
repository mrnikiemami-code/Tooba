using Tooba.Wallet.Application.Models;

namespace Tooba.Wallet.Application.Models;

/// <summary>Wallet summary with a balance derived from the ledger.</summary>
public sealed record WalletSummaryDto(
    Guid AccountId,
    Guid CustomerActorUserId,
    string Currency,
    string Status,
    decimal Balance,
    decimal TotalCredits,
    decimal TotalDebits,
    int EntryCount,
    DateTimeOffset CreatedAt);

/// <summary>Ledger row for history.</summary>
public sealed record WalletLedgerEntryDto(
    Guid EntryId,
    Guid AccountId,
    string Type,
    decimal Amount,
    string Currency,
    string Direction,
    string SourceType,
    Guid SourceId,
    DateTimeOffset CreatedAt,
    string? Metadata);

/// <summary>Ledger page.</summary>
public sealed record WalletLedgerPageDto(
    IReadOnlyList<WalletLedgerEntryDto> Items,
    int Total,
    int Page,
    int PageSize,
    decimal Balance);

/// <summary>Gift-card redemption result.</summary>
public sealed record GiftCardRedeemResultDto(
    Guid RedemptionId,
    Guid CardId,
    Guid AccountId,
    decimal Amount,
    decimal WalletBalance,
    string CardStatus,
    decimal CardRemainingAmount,
    bool IdempotentReplay);

/// <summary>Customer redemption input.</summary>
public sealed record RedeemGiftCardCommand(string Code, string IdempotencyKey);

/// <summary>Gift-card summary for Admin (no plaintext).</summary>
public sealed record GiftCardSummaryDto(
    Guid CardId,
    string Currency,
    decimal InitialAmount,
    decimal RemainingAmount,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    Guid? RecipientActorUserId,
    Guid CreatedByActorUserId,
    int RedemptionCount);

/// <summary>Gift-card detail including redemption history.</summary>
public sealed record GiftCardDetailDto(
    Guid CardId,
    string Currency,
    decimal InitialAmount,
    decimal RemainingAmount,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    Guid? RecipientActorUserId,
    Guid CreatedByActorUserId,
    IReadOnlyList<GiftCardRedemptionDto> Redemptions);

/// <summary>Redemption row.</summary>
public sealed record GiftCardRedemptionDto(
    Guid RedemptionId,
    Guid CardId,
    Guid AccountId,
    decimal Amount,
    DateTimeOffset CreatedAt);

/// <summary>Gift-card list page.</summary>
public sealed record GiftCardListPageDto(
    IReadOnlyList<GiftCardSummaryDto> Items,
    int Total,
    int Page,
    int PageSize);

/// <summary>Issue result; DisplayCode is returned only once.</summary>
public sealed record GiftCardIssueResultDto(
    GiftCardSummaryDto Card,
    string DisplayCode,
    bool IdempotentReplay);

/// <summary>Admin issue input.</summary>
public sealed record IssueGiftCardCommand(
    decimal InitialAmount,
    string? Currency,
    DateTimeOffset? ExpiresAt,
    Guid? RecipientActorUserId,
    string IdempotencyKey);

/// <summary>Admin list filter.</summary>
public sealed record AdminGiftCardListQuery(
    string? Status,
    string? Q,
    int Page,
    int PageSize);

/// <summary>Admin adjustment input.</summary>
public sealed record AdminWalletAdjustmentCommand(
    decimal Amount,
    string Direction,
    string Reason,
    string IdempotencyKey);

/// <summary>Adjustment result.</summary>
public sealed record AdminWalletAdjustmentResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);

/// <summary>Order-payment debit result.</summary>
public sealed record WalletSpendResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);

/// <summary>Refund-credit result.</summary>
public sealed record WalletCreditResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);

/// <summary>Development demo snapshot (matches the prior Host demo JSON shape).</summary>
public sealed record WalletDemoPreviewDto(
    Guid CustomerActorUserId,
    Guid AccountId,
    decimal Balance,
    Guid UnusedGiftCardId,
    string UnusedGiftCardDemoCode,
    Guid PartiallyRedeemedGiftCardId,
    Guid ExpiredGiftCardId,
    Guid RevokedGiftCardId,
    Guid? WalletPaidCheckoutId,
    Guid? WalletPaidPaymentId,
    Guid? WalletPaidSellerOrderId,
    Guid? WalletRefundReturnRequestId,
    string Note);
