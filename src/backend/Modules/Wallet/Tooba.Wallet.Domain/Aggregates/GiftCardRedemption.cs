using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Domain.Aggregates;

/// <summary>ثبت بازخرید کارت هدیه به حساب کیف پول.</summary>
public sealed class GiftCardRedemption
{
    /// <summary>حداکثر طول کلید idempotency.</summary>
    public const int IdempotencyKeyMaxLength = 128;

    private GiftCardRedemption()
    {
    }

    /// <summary>شناسهٔ بازخرید.</summary>
    public Guid RedemptionId { get; init; }

    /// <summary>کارت.</summary>
    public Guid CardId { get; init; }

    /// <summary>حساب اعتبارگیرنده.</summary>
    public Guid AccountId { get; init; }

    /// <summary>مبلغ بازخرید.</summary>
    public decimal Amount { get; init; }

    /// <summary>کلید یکتای idempotency.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>بازخرید جدید می‌سازد.</summary>
    public static GiftCardRedemption Create(
        Guid redemptionId,
        Guid cardId,
        Guid accountId,
        decimal amount,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (redemptionId == Guid.Empty || cardId == Guid.Empty || accountId == Guid.Empty)
            throw new InvalidOperationException("wallet.giftcard.redemption_ids");
        if (amount <= 0)
            throw new InvalidOperationException("wallet.giftcard.redemption_amount");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > IdempotencyKeyMaxLength)
            throw new InvalidOperationException("wallet.idempotency_invalid");
        return new GiftCardRedemption
        {
            RedemptionId = redemptionId,
            CardId = cardId,
            AccountId = accountId,
            Amount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero),
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedAt = now,
        };
    }

    /// <summary>بازخرید دانه‌شده.</summary>
    public static GiftCardRedemption CreateSeeded(
        Guid redemptionId,
        Guid cardId,
        Guid accountId,
        decimal amount,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (redemptionId == Guid.Empty)
            throw new InvalidOperationException("wallet.giftcard.redemption_id");
        var created = Create(redemptionId, cardId, accountId, amount, idempotencyKey, now);
        return new GiftCardRedemption
        {
            RedemptionId = redemptionId,
            CardId = created.CardId,
            AccountId = created.AccountId,
            Amount = created.Amount,
            IdempotencyKey = created.IdempotencyKey,
            CreatedAt = created.CreatedAt,
        };
    }
}
