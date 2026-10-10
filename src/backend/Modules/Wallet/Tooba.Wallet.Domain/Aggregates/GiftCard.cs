using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Domain.Aggregates;

/// <summary>کارت هدیه؛ فقط CodeHash ذخیره می‌شود.</summary>
public sealed class GiftCard
{
    /// <summary>حداکثر طول هش کد.</summary>
    public const int CodeHashMaxLength = 128;

    /// <summary>حداکثر طول کلید idempotency.</summary>
    public const int IdempotencyKeyMaxLength = 128;

    private GiftCard()
    {
    }

    /// <summary>شناسهٔ کارت.</summary>
    public Guid CardId { get; init; }

    /// <summary>هش یک‌طرفهٔ کد؛ plaintext ذخیره نمی‌شود.</summary>
    public string CodeHash { get; init; } = string.Empty;

    /// <summary>ارز.</summary>
    public string Currency { get; init; } = WalletAccount.DefaultCurrency;

    /// <summary>مبلغ اولیه.</summary>
    public decimal InitialAmount { get; init; }

    /// <summary>ماندهٔ قابل بازخرید.</summary>
    public decimal RemainingAmount { get; private set; }

    /// <summary>وضعیت.</summary>
    public GiftCardStatus Status { get; private set; }

    /// <summary>زمان صدور.</summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>انقضا اختیاری.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>گیرندهٔ اختیاری.</summary>
    public Guid? RecipientActorUserId { get; init; }

    /// <summary>Actor صادرکننده.</summary>
    public Guid CreatedByActorUserId { get; init; }

    /// <summary>کلید idempotency صدور.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>کارت جدید صادر می‌کند و کد نمایشی را برمی‌گرداند.</summary>
    public static (GiftCard Card, string DisplayCode) Issue(
        Guid cardId,
        decimal initialAmount,
        string currency,
        Guid createdByActorUserId,
        string idempotencyKey,
        DateTimeOffset now,
        DateTimeOffset? expiresAt = null,
        Guid? recipientActorUserId = null,
        string? plaintextCode = null)
    {
        if (cardId == Guid.Empty)
            throw new ContractOperationException(WalletErrorCodes.GiftCardIdsRequired);
        if (initialAmount <= 0)
            throw new ContractOperationException(WalletErrorCodes.GiftCardAmountPositive);
        if (createdByActorUserId == Guid.Empty)
            throw new ContractOperationException(WalletErrorCodes.GiftCardIssuerRequired);
        if (expiresAt is { } exp && exp <= now)
            throw new ContractOperationException(WalletErrorCodes.GiftCardExpiryFuture);
        var key = NormalizeIdempotency(idempotencyKey)
                  ?? throw new ContractOperationException(WalletErrorCodes.IdempotencyRequired);
        var display = string.IsNullOrWhiteSpace(plaintextCode)
            ? GenerateDisplayCode()
            : NormalizeCode(plaintextCode);
        var amount = decimal.Round(initialAmount, 0, MidpointRounding.AwayFromZero);
        var card = new GiftCard
        {
            CardId = cardId,
            CodeHash = HashCode(display),
            Currency = WalletAccount.NormalizeCurrency(currency),
            InitialAmount = amount,
            RemainingAmount = amount,
            Status = GiftCardStatus.Active,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            RecipientActorUserId = recipientActorUserId is { } r && r != Guid.Empty ? r : null,
            CreatedByActorUserId = createdByActorUserId,
            IdempotencyKey = key,
        };
        return (card, display);
    }

    /// <summary>کارت دانه‌شده با کد شناخته‌شده.</summary>
    public static GiftCard CreateSeeded(
        Guid cardId,
        string plaintextCode,
        decimal initialAmount,
        decimal remainingAmount,
        string currency,
        GiftCardStatus status,
        Guid createdByActorUserId,
        string idempotencyKey,
        DateTimeOffset issuedAt,
        DateTimeOffset? expiresAt = null,
        Guid? recipientActorUserId = null)
    {
        if (cardId == Guid.Empty || createdByActorUserId == Guid.Empty)
            throw new ContractOperationException(WalletErrorCodes.GiftCardIdsRequired);
        if (initialAmount <= 0 || remainingAmount < 0 || remainingAmount > initialAmount)
            throw new ContractOperationException(WalletErrorCodes.GiftCardAmountsInvalid);
        return new GiftCard
        {
            CardId = cardId,
            CodeHash = HashCode(plaintextCode),
            Currency = WalletAccount.NormalizeCurrency(currency),
            InitialAmount = decimal.Round(initialAmount, 0, MidpointRounding.AwayFromZero),
            RemainingAmount = decimal.Round(remainingAmount, 0, MidpointRounding.AwayFromZero),
            Status = status,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            RecipientActorUserId = recipientActorUserId,
            CreatedByActorUserId = createdByActorUserId,
            IdempotencyKey = NormalizeIdempotency(idempotencyKey)
                             ?? throw new ContractOperationException(WalletErrorCodes.IdempotencyRequired),
        };
    }

    /// <summary>باطل‌کردن کارت توسط Admin.</summary>
    public void Revoke(DateTimeOffset now)
    {
        _ = now;
        if (Status is GiftCardStatus.Revoked or GiftCardStatus.Redeemed)
            throw new ContractOperationException(WalletErrorCodes.GiftCardNotRevocable);
        Status = GiftCardStatus.Revoked;
        RemainingAmount = 0;
    }

    /// <summary>بازخرید مبلغ از کارت؛ وضعیت را به‌روز می‌کند.</summary>
    public void ApplyRedemption(decimal amount, DateTimeOffset now)
    {
        EnsureRedeemable(now);
        if (amount <= 0 || amount > RemainingAmount)
            throw new ContractOperationException(WalletErrorCodes.GiftCardRedeemAmountInvalid);
        RemainingAmount -= decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        Status = RemainingAmount == 0 ? GiftCardStatus.Redeemed : GiftCardStatus.PartiallyRedeemed;
    }

    /// <summary>اعتبارسنجی قبل از بازخرید.</summary>
    public void EnsureRedeemable(DateTimeOffset now)
    {
        if (Status is GiftCardStatus.Revoked)
            throw new ContractOperationException(WalletErrorCodes.GiftCardRevoked);
        if (Status is GiftCardStatus.Redeemed)
            throw new ContractOperationException(WalletErrorCodes.GiftCardFullyRedeemed);
        if (Status is GiftCardStatus.Expired || (ExpiresAt is { } exp && exp <= now))
        {
            Status = GiftCardStatus.Expired;
            throw new ContractOperationException(WalletErrorCodes.GiftCardExpired);
        }

        if (Status is not (GiftCardStatus.Active or GiftCardStatus.PartiallyRedeemed))
            throw new ContractOperationException(WalletErrorCodes.GiftCardStatusInvalid);
        if (RemainingAmount <= 0)
            throw new ContractOperationException(WalletErrorCodes.GiftCardZeroRemaining);
    }

    /// <summary>هش پایدار کد نرمال‌شده.</summary>
    public static string HashCode(string plaintextCode)
    {
        var normalized = NormalizeCode(plaintextCode);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash);
    }

    /// <summary>کد را برای مقایسه نرمال می‌کند.</summary>
    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ContractOperationException(WalletErrorCodes.GiftCardCodeRequired);
        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length is < 6 or > 64)
            throw new ContractOperationException(WalletErrorCodes.GiftCardCodeLength);
        return trimmed;
    }

    private static string GenerateDisplayCode()
    {
        Span<byte> bytes = stackalloc byte[9];
        RandomNumberGenerator.Fill(bytes);
        var raw = Convert.ToHexString(bytes);
        return $"GC-{raw[..6]}-{raw[6..12]}-{raw[12..18]}";
    }

    private static string? NormalizeIdempotency(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
            throw new ContractOperationException(WalletErrorCodes.IdempotencyInvalid);
        return trimmed;
    }
}
