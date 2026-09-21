using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Domain.Aggregates;

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
    public static WalletAccount Create(Guid accountId, Guid customerActorUserId, string currency, DateTimeOffset now)
    {
        if (accountId == Guid.Empty || customerActorUserId == Guid.Empty)
            throw new InvalidOperationException("wallet.account.ids_required");
        var cur = NormalizeCurrency(currency);
        return new WalletAccount
        {
            AccountId = accountId,
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
            throw new InvalidOperationException("wallet.ids_required");
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

    /// <summary>ارز را نرمال و اعتبارسنجی می‌کند (delegate به Wallet.Contracts).</summary>
    public static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidOperationException("wallet.currency_required");
        var trimmed = currency.Trim().ToUpperInvariant();
        if (trimmed.Length is < 3 or > 8)
            throw new InvalidOperationException("wallet.currency_invalid");
        return trimmed;
    }
}
