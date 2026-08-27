using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;

namespace Tooba.Wallet.Domain;

/// <summary>وضعیت حساب کیف پول مشتری.</summary>
public enum WalletAccountStatus
{
    /// <summary>فعال و قابل اعتبار/بدهکار.</summary>
    Active = 0,

    /// <summary>مسدود؛ فقط خواندن.</summary>
    Frozen = 1,
}

/// <summary>جهت سطر دفتر کیف پول؛ مبلغ همیشه مثبت است.</summary>
public enum LedgerDirection
{
    /// <summary>افزایش موجودی مشتق‌شده.</summary>
    Credit = 0,

    /// <summary>کاهش موجودی مشتق‌شده.</summary>
    Debit = 1,
}

/// <summary>نوع معنایی سطر دفتر.</summary>
public enum LedgerEntryType
{
    /// <summary>اعتبار از بازخرید کارت هدیه.</summary>
    GiftCardCredit = 0,

    /// <summary>تعدیل ممیزی‌شدهٔ Admin.</summary>
    AdminAdjustment = 1,

    /// <summary>بدهکار پرداخت سفارش (ATOMIC_DEBIT_AT_PAID).</summary>
    OrderPaymentDebit = 2,

    /// <summary>اعتبار بازگشت وجه به کیف پول.</summary>
    RefundCredit = 3,
}

/// <summary>وضعیت کارت هدیه.</summary>
public enum GiftCardStatus
{
    /// <summary>فعال و قابل بازخرید.</summary>
    Active = 0,

    /// <summary>کاملاً مصرف‌شده.</summary>
    Redeemed = 1,

    /// <summary>بخشی مصرف‌شده.</summary>
    PartiallyRedeemed = 2,

    /// <summary>منقضی.</summary>
    Expired = 3,

    /// <summary>باطل‌شده توسط Admin.</summary>
    Revoked = 4,
}

/// <summary>حساب کیف پول مشتری؛ موجودی در خود حساب ذخیره نمی‌شود.</summary>
public sealed class WalletAccount
{
    /// <summary>ارز پیش‌فرض IRR.</summary>
    public const string DefaultCurrency = "IRR";

    private WalletAccount()
    {
    }

    /// <summary>شناسهٔ پایدار حساب.</summary>
    public Guid AccountId { get; init; }

    /// <summary>Actor مشتری مالک.</summary>
    public Guid CustomerActorUserId { get; init; }

    /// <summary>ارز حساب.</summary>
    public string Currency { get; init; } = DefaultCurrency;

    /// <summary>وضعیت حساب.</summary>
    public WalletAccountStatus Status { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>حساب Active جدید می‌سازد.</summary>
    public static WalletAccount Create(Guid customerActorUserId, string currency, DateTimeOffset now)
    {
        if (customerActorUserId == Guid.Empty)
            throw new InvalidOperationException("هویت مشتری الزامی است.");
        var cur = NormalizeCurrency(currency);
        return new WalletAccount
        {
            AccountId = UuidV7.New(),
            CustomerActorUserId = customerActorUserId,
            Currency = cur,
            Status = WalletAccountStatus.Active,
            CreatedAt = now,
        };
    }

    /// <summary>حساب با شناسهٔ ثابت برای دانهٔ توسعه.</summary>
    public static WalletAccount CreateSeeded(
        Guid accountId,
        Guid customerActorUserId,
        string currency,
        WalletAccountStatus status,
        DateTimeOffset now)
    {
        if (accountId == Guid.Empty || customerActorUserId == Guid.Empty)
            throw new InvalidOperationException("شناسهٔ حساب و مشتری الزامی است.");
        return new WalletAccount
        {
            AccountId = accountId,
            CustomerActorUserId = customerActorUserId,
            Currency = NormalizeCurrency(currency),
            Status = status,
            CreatedAt = now,
        };
    }

    /// <summary>آیا حساب برای اعتبار/بدهکار باز است.</summary>
    public bool CanMutateLedger => Status == WalletAccountStatus.Active;

    /// <summary>ارز را نرمال و اعتبارسنجی می‌کند.</summary>
    public static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidOperationException("ارز الزامی است.");
        var trimmed = currency.Trim().ToUpperInvariant();
        if (trimmed.Length is < 3 or > 8)
            throw new InvalidOperationException("ارز نامعتبر است.");
        return trimmed;
    }
}

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
        Guid accountId,
        Guid giftCardId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
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
        Guid accountId,
        Guid adjustmentId,
        decimal amount,
        string currency,
        LedgerDirection direction,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
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
        Guid accountId,
        Guid paymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
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
        Guid accountId,
        Guid returnRequestId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now,
        string? metadata = null) =>
        Create(
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
        var entry = Create(accountId, type, amount, currency, direction, sourceType, sourceId, idempotencyKey, now, metadata);
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
        if (accountId == Guid.Empty || sourceId == Guid.Empty)
            throw new InvalidOperationException("شناسهٔ حساب و منبع الزامی است.");
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ دفتر باید مثبت باشد.");
        if (string.IsNullOrWhiteSpace(sourceType) || sourceType.Trim().Length > SourceTypeMaxLength)
            throw new InvalidOperationException("SourceType نامعتبر است.");
        var key = NormalizeIdempotency(idempotencyKey)
                  ?? throw new InvalidOperationException("IdempotencyKey الزامی است.");
        string? meta = null;
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            meta = metadata.Trim();
            if (meta.Length > MetadataMaxLength)
                throw new InvalidOperationException("Metadata بیش از حد طولانی است.");
        }

        return new WalletLedgerEntry
        {
            EntryId = UuidV7.New(),
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
            throw new InvalidOperationException("IdempotencyKey معتبر نیست.");
        return trimmed;
    }
}

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
        decimal initialAmount,
        string currency,
        Guid createdByActorUserId,
        string idempotencyKey,
        DateTimeOffset now,
        DateTimeOffset? expiresAt = null,
        Guid? recipientActorUserId = null,
        string? plaintextCode = null)
    {
        if (initialAmount <= 0)
            throw new InvalidOperationException("مبلغ اولیه باید مثبت باشد.");
        if (createdByActorUserId == Guid.Empty)
            throw new InvalidOperationException("صادرکننده الزامی است.");
        if (expiresAt is { } exp && exp <= now)
            throw new InvalidOperationException("تاریخ انقضا باید در آینده باشد.");
        var key = NormalizeIdempotency(idempotencyKey)
                  ?? throw new InvalidOperationException("IdempotencyKey الزامی است.");
        var display = string.IsNullOrWhiteSpace(plaintextCode)
            ? GenerateDisplayCode()
            : NormalizeCode(plaintextCode);
        var amount = decimal.Round(initialAmount, 0, MidpointRounding.AwayFromZero);
        var card = new GiftCard
        {
            CardId = UuidV7.New(),
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
            throw new InvalidOperationException("شناسهٔ کارت و صادرکننده الزامی است.");
        if (initialAmount <= 0 || remainingAmount < 0 || remainingAmount > initialAmount)
            throw new InvalidOperationException("مبالغ کارت نامعتبر است.");
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
                             ?? throw new InvalidOperationException("IdempotencyKey الزامی است."),
        };
    }

    /// <summary>باطل‌کردن کارت توسط Admin.</summary>
    public void Revoke(DateTimeOffset now)
    {
        _ = now;
        if (Status is GiftCardStatus.Revoked or GiftCardStatus.Redeemed)
            throw new InvalidOperationException("کارت قابل ابطال نیست.");
        Status = GiftCardStatus.Revoked;
        RemainingAmount = 0;
    }

    /// <summary>بازخرید مبلغ از کارت؛ وضعیت را به‌روز می‌کند.</summary>
    public void ApplyRedemption(decimal amount, DateTimeOffset now)
    {
        EnsureRedeemable(now);
        if (amount <= 0 || amount > RemainingAmount)
            throw new InvalidOperationException("مبلغ بازخرید نامعتبر است.");
        RemainingAmount -= decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        Status = RemainingAmount == 0 ? GiftCardStatus.Redeemed : GiftCardStatus.PartiallyRedeemed;
    }

    /// <summary>اعتبارسنجی قبل از بازخرید.</summary>
    public void EnsureRedeemable(DateTimeOffset now)
    {
        if (Status is GiftCardStatus.Revoked)
            throw new InvalidOperationException("کارت باطل شده است.");
        if (Status is GiftCardStatus.Redeemed)
            throw new InvalidOperationException("کارت کاملاً مصرف شده است.");
        if (Status is GiftCardStatus.Expired || (ExpiresAt is { } exp && exp <= now))
        {
            Status = GiftCardStatus.Expired;
            throw new InvalidOperationException("کارت منقضی شده است.");
        }

        if (Status is not (GiftCardStatus.Active or GiftCardStatus.PartiallyRedeemed))
            throw new InvalidOperationException("وضعیت کارت برای بازخرید نامعتبر است.");
        if (RemainingAmount <= 0)
            throw new InvalidOperationException("ماندهٔ کارت صفر است.");
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
            throw new InvalidOperationException("کد کارت الزامی است.");
        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length is < 6 or > 64)
            throw new InvalidOperationException("طول کد کارت نامعتبر است.");
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
            throw new InvalidOperationException("IdempotencyKey معتبر نیست.");
        return trimmed;
    }
}

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
        Guid cardId,
        Guid accountId,
        decimal amount,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (cardId == Guid.Empty || accountId == Guid.Empty)
            throw new InvalidOperationException("کارت و حساب الزامی است.");
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ بازخرید باید مثبت باشد.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > IdempotencyKeyMaxLength)
            throw new InvalidOperationException("IdempotencyKey معتبر نیست.");
        return new GiftCardRedemption
        {
            RedemptionId = UuidV7.New(),
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
            throw new InvalidOperationException("RedemptionId الزامی است.");
        var created = Create(cardId, accountId, amount, idempotencyKey, now);
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
