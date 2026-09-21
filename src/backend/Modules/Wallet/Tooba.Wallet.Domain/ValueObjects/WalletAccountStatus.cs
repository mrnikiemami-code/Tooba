namespace Tooba.Wallet.Domain.ValueObjects;

/// <summary>وضعیت حساب کیف پول مشتری.</summary>
public enum WalletAccountStatus
{
    /// <summary>فعال و قابل اعتبار/بدهکار.</summary>
    Active = 0,

    /// <summary>مسدود؛ فقط خواندن.</summary>
    Frozen = 1,
}
