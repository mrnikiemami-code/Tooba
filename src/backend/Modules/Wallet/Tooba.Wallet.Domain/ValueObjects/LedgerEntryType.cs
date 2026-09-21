namespace Tooba.Wallet.Domain.ValueObjects;

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
