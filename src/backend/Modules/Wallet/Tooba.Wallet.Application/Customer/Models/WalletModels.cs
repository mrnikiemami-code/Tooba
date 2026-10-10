namespace Tooba.Wallet.Application.Customer.Models;

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

/// <summary>Customer gift-card redemption input (transport-shaped command input).</summary>
public sealed record RedeemGiftCardCommand(string Code, string IdempotencyKey);

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
