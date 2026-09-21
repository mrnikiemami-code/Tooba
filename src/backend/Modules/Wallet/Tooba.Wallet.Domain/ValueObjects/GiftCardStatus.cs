namespace Tooba.Wallet.Domain.ValueObjects;

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
