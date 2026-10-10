using Tooba.Wallet.Application.Customer.Models;

namespace Tooba.Wallet.Application.Admin.Models;

/// <summary>Gift-card summary for Admin (no plaintext code).</summary>
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

/// <summary>Admin issue input (transport-shaped command input).</summary>
public sealed record IssueGiftCardCommand(
    decimal InitialAmount,
    string? Currency,
    DateTimeOffset? ExpiresAt,
    Guid? RecipientActorUserId,
    string IdempotencyKey);

/// <summary>Admin list filter (transport-shaped query input).</summary>
public sealed record AdminGiftCardListQuery(
    string? Status,
    string? Q,
    int Page,
    int PageSize);

/// <summary>Admin adjustment input (transport-shaped command input).</summary>
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
