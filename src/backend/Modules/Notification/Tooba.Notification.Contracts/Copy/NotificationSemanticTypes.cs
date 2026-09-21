namespace Tooba.Notification.Contracts.Copy;

/// <summary>
/// انواع معنایی اعلان که Wallet (و سایر مصرف‌کنندگان Contracts) برای Type استفاده می‌کنند.
/// کپی محلی‌سازی کامل در Application می‌ماند؛ این فقط قرارداد Type است.
/// </summary>
public static class NotificationSemanticTypes
{
    /// <summary>بازخرید کارت هدیه به کیف پول.</summary>
    public const string WalletGiftCardRedeemed = "wallet.gift_card.redeemed";

    /// <summary>تعدیل Admin روی کیف پول مشتری.</summary>
    public const string WalletAdminAdjustment = "wallet.admin_adjustment";

    /// <summary>پرداخت موفق سفارش از کیف پول.</summary>
    public const string WalletPaymentSucceeded = "wallet.payment.succeeded";

    /// <summary>اعتبار refund به کیف پول.</summary>
    public const string WalletRefundCredited = "wallet.refund.credited";

    /// <summary>پاسخ عمومی پشتیبانی از Admin.</summary>
    public const string SupportAdminReply = "support.admin_reply";
}
