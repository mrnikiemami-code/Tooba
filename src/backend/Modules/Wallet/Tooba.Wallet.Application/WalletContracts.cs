using Tooba.Wallet.Domain;

namespace Tooba.Wallet.Application;

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

/// <summary>snapshot پیش‌نمایش توسعه.</summary>
public sealed record WalletDemoPreviewDto(
    Guid CustomerActorUserId,
    Guid AccountId,
    decimal Balance,
    Guid UnusedGiftCardId,
    string UnusedGiftCardDemoCode,
    Guid PartiallyRedeemedGiftCardId,
    Guid ExpiredGiftCardId,
    Guid RevokedGiftCardId,
    string Note);

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
}

/// <summary>پارس enumهای مرز Application.</summary>
public static class WalletEnumParsing
{
    /// <summary>وضعیت کارت فیلتر.</summary>
    public static GiftCardStatus? TryParseGiftCardStatus(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<GiftCardStatus>(value, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidOperationException("وضعیت کارت هدیه نامعتبر است.");

    /// <summary>جهت تعدیل.</summary>
    public static LedgerDirection ParseDirection(string value) =>
        Enum.TryParse<LedgerDirection>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("جهت تعدیل نامعتبر است.");
}
