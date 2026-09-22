using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Application.Models;

/// <summary>خلاصهٔ کیف پول با موجودی مشتق‌شده از دفتر.</summary>
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

/// <summary>سطر دفتر برای تاریخچه.</summary>
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

/// <summary>صفحهٔ دفتر.</summary>
public sealed record WalletLedgerPageDto(
    IReadOnlyList<WalletLedgerEntryDto> Items,
    int Total,
    int Page,
    int PageSize,
    decimal Balance);

/// <summary>نتیجهٔ بازخرید کارت هدیه.</summary>
public sealed record GiftCardRedeemResultDto(
    Guid RedemptionId,
    Guid CardId,
    Guid AccountId,
    decimal Amount,
    decimal WalletBalance,
    string CardStatus,
    decimal CardRemainingAmount,
    bool IdempotentReplay);

/// <summary>ورودی بازخرید مشتری.</summary>
public sealed record RedeemGiftCardCommand(string Code, string IdempotencyKey);

/// <summary>خلاصهٔ کارت هدیه برای Admin (بدون plaintext).</summary>
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

/// <summary>جزئیات کارت شامل تاریخچهٔ بازخرید.</summary>
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

/// <summary>سطر بازخرید.</summary>
public sealed record GiftCardRedemptionDto(
    Guid RedemptionId,
    Guid CardId,
    Guid AccountId,
    decimal Amount,
    DateTimeOffset CreatedAt);

/// <summary>صفحهٔ فهرست کارت.</summary>
public sealed record GiftCardListPageDto(
    IReadOnlyList<GiftCardSummaryDto> Items,
    int Total,
    int Page,
    int PageSize);

/// <summary>نتیجهٔ صدور؛ DisplayCode فقط یک‌بار برمی‌گردد.</summary>
public sealed record GiftCardIssueResultDto(
    GiftCardSummaryDto Card,
    string DisplayCode,
    bool IdempotentReplay);

/// <summary>ورودی صدور Admin.</summary>
public sealed record IssueGiftCardCommand(
    decimal InitialAmount,
    string? Currency,
    DateTimeOffset? ExpiresAt,
    Guid? RecipientActorUserId,
    string IdempotencyKey);

/// <summary>فیلتر فهرست Admin.</summary>
public sealed record AdminGiftCardListQuery(
    string? Status,
    string? Q,
    int Page,
    int PageSize);

/// <summary>ورودی تعدیل Admin.</summary>
public sealed record AdminWalletAdjustmentCommand(
    decimal Amount,
    string Direction,
    string Reason,
    string IdempotencyKey);

/// <summary>نتیجهٔ تعدیل.</summary>
public sealed record AdminWalletAdjustmentResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);

/// <summary>نتیجهٔ بدهکار پرداخت سفارش از کیف پول.</summary>
public sealed record WalletSpendResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);

/// <summary>نتیجهٔ اعتبار refund به کیف پول.</summary>
public sealed record WalletCreditResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);

/// <summary>snapshot پیش‌نمایش توسعه (matches prior Host demo JSON shape).</summary>
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
