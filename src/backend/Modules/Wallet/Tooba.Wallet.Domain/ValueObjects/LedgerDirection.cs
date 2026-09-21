namespace Tooba.Wallet.Domain.ValueObjects;

/// <summary>جهت سطر دفتر کیف پول؛ مبلغ همیشه مثبت است.</summary>
public enum LedgerDirection
{
    /// <summary>افزایش موجودی مشتق‌شده.</summary>
    Credit = 0,

    /// <summary>کاهش موجودی مشتق‌شده.</summary>
    Debit = 1,
}
