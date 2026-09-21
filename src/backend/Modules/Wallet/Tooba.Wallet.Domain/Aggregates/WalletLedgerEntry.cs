using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Domain.Aggregates;

/// <summary>سطر immutable دفتر کیف پول؛ موجودی فقط از مجموع این سطرها مشتق می‌شود.</summary>
public sealed class WalletLedgerEntry
{
    /// <summary>حداکثر طول کلید idempotency.</summary>
    public const int IdempotencyKeyMaxLength = 128;

    /// <summary>حداکثر طول نوع منبع.</summary>
    public const int SourceTypeMaxLength = 64;

    /// <summary>حداکثر طول metadata JSON.</summary>
    public const int MetadataMaxLength = 2000;

    private WalletLedgerEntry()
    {
    }

    /// <summary>شناسهٔ سطر.</summary>
    public Guid EntryId { get; init; }

    /// <summary>حساب مالک.</summary>
    public Guid AccountId { get; init; }

    /// <summary>نوع معنایی.</summary>
    public LedgerEntryType Type { get; init; }

    /// <summary>مبلغ مثبت.</summary>
    public decimal Amount { get; init; }

    /// <summary>ارز.</summary>
    public string Currency { get; init; } = WalletAccount.DefaultCurrency;

    /// <summary>جهت Credit/Debit.</summary>
    public LedgerDirection Direction { get; init; }

    /// <summary>نوع منبع بدون FK.</summary>
    public string SourceType { get; init; } = string.Empty;

    /// <summary>شناسهٔ منبع بدون FK.</summary>
    public Guid SourceId { get; init; }

    /// <summary>کلید یکتای idempotency.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>metadata امن اختیاری.</summary>
    public string? Metadata { get; init; }

    /// <summary>سطر Credit از کارت هدیه می‌سازد.</summary>
    public static WalletLedgerEntry PostGiftCardCredit(
        Guid entryId,
        Guid accountId,
        Guid giftCardId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
            entryId,
            accountId,
            LedgerEntryType.GiftCardCredit,
            amount,
            currency,
            LedgerDirection.Credit,
            "gift_card",
            giftCardId,
            idempotencyKey,
            now,
            metadata);

    /// <summary>سطر تعدیل Admin می‌سازد.</summary>
    public static WalletLedgerEntry PostAdminAdjustment(
        Guid entryId,
        Guid accountId,
        Guid adjustmentId,
        decimal amount,
        string currency,
        LedgerDirection direction,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
            entryId,
            accountId,
            LedgerEntryType.AdminAdjustment,
            amount,
            currency,
            direction,
            "admin_adjustment",
            adjustmentId,
            idempotencyKey,
            now,
            metadata);

    /// <summary>سطر بدهکار پرداخت سفارش می‌سازد؛ SourceType=payment.</summary>
    public static WalletLedgerEntry PostOrderPaymentDebit(
        Guid entryId,
        Guid accountId,
        Guid paymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
            entryId,
            accountId,
            LedgerEntryType.OrderPaymentDebit,
            amount,
            currency,
            LedgerDirection.Debit,
            "payment",
            paymentId,
            idempotencyKey,
            now,
            metadata);

    /// <summary>سطر اعتبار refund به کیف پول می‌سازد؛ SourceType=refund.</summary>
    public static WalletLedgerEntry PostRefundCredit(
        Guid entryId,
        Guid accountId,
        Guid returnRequestId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
            entryId,
            accountId,
            LedgerEntryType.RefundCredit,
            amount,
            currency,
            LedgerDirection.Credit,
            "refund",
            returnRequestId,
            idempotencyKey,
            now,
            metadata);

    /// <summary>سطر با شناسهٔ ثابت برای دانه.</summary>
    public static WalletLedgerEntry CreateSeeded(
        Guid entryId,
        Guid accountId,
        LedgerEntryType type,
        decimal amount,
        string currency,
        LedgerDirection direction,
        string sourceType,
        Guid sourceId,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null)
    {
        var entry = Create(entryId, accountId, type, amount, currency, direction, sourceType, sourceId, idempotencyKey, now, metadata);
        return new WalletLedgerEntry
        {
            EntryId = entryId,
            AccountId = entry.AccountId,
            Type = entry.Type,
            Amount = entry.Amount,
            Currency = entry.Currency,
            Direction = entry.Direction,
            SourceType = entry.SourceType,
            SourceId = entry.SourceId,
            IdempotencyKey = entry.IdempotencyKey,
            CreatedAt = entry.CreatedAt,
            Metadata = entry.Metadata,
        };
    }

    private static WalletLedgerEntry Create(
        Guid entryId,
        Guid accountId,
        LedgerEntryType type,
        decimal amount,
        string currency,
        LedgerDirection direction,
        string sourceType,
        Guid sourceId,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata)
    {
        if (entryId == Guid.Empty || accountId == Guid.Empty || sourceId == Guid.Empty)
            throw new InvalidOperationException("wallet.ledger.ids_required");
        if (amount <= 0)
            throw new InvalidOperationException("wallet.ledger.amount_positive");
        if (string.IsNullOrWhiteSpace(sourceType) || sourceType.Trim().Length > SourceTypeMaxLength)
            throw new InvalidOperationException("wallet.ledger.source_type_invalid");
        var key = NormalizeIdempotency(idempotencyKey)
                  ?? throw new InvalidOperationException("wallet.idempotency_required");
        string? meta = null;
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            meta = metadata.Trim();
            if (meta.Length > MetadataMaxLength)
                throw new InvalidOperationException("wallet.metadata_too_long");
        }

        return new WalletLedgerEntry
        {
            EntryId = entryId,
            AccountId = accountId,
            Type = type,
            Amount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero),
            Currency = WalletAccount.NormalizeCurrency(currency),
            Direction = direction,
            SourceType = sourceType.Trim(),
            SourceId = sourceId,
            IdempotencyKey = key,
            CreatedAt = now,
            Metadata = meta,
        };
    }

    /// <summary>سهم سطر در موجودی مشتق‌شده.</summary>
    public decimal SignedAmount => Direction == LedgerDirection.Credit ? Amount : -Amount;

    private static string? NormalizeIdempotency(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
            throw new InvalidOperationException("wallet.idempotency_invalid");
        return trimmed;
    }
}
