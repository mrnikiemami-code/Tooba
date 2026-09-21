using System.Text.Json;
using Tooba.Notification.Contracts.Copy;

namespace Tooba.Notification.Application.Rendering;

/// <summary>
/// انواع معنایی اعلان و کپی محلی در زمان خواندن.
/// Typeهای Wallet با Contracts هم‌منبع‌اند.
/// </summary>
public static class NotificationCopy
{
    /// <summary>پرداخت موفق.</summary>
    public const string PaymentSucceeded = "payment.succeeded";

    /// <summary>پرداخت ناموفق.</summary>
    public const string PaymentFailed = "payment.failed";

    /// <summary>سفارش پرداخت‌شده برای فروشنده.</summary>
    public const string OrderPaidSeller = "order.paid.seller";

    /// <summary>ایجاد fulfillment.</summary>
    public const string FulfillmentCreated = "fulfillment.created";

    /// <summary>ارسال محموله.</summary>
    public const string ShipmentDispatched = "shipment.dispatched";

    /// <summary>درخواست مرجوعی.</summary>
    public const string ReturnRequested = "return.requested";

    /// <summary>تأیید مرجوعی.</summary>
    public const string ReturnApproved = "return.approved";

    /// <summary>موفقیت refund.</summary>
    public const string RefundSucceeded = "refund.succeeded";

    /// <summary>پاسخ عمومی پشتیبانی از Admin.</summary>
    public const string SupportAdminReply = NotificationSemanticTypes.SupportAdminReply;

    /// <summary>بازخرید کارت هدیه به کیف پول.</summary>
    public const string WalletGiftCardRedeemed = NotificationSemanticTypes.WalletGiftCardRedeemed;

    /// <summary>تعدیل Admin روی کیف پول مشتری.</summary>
    public const string WalletAdminAdjustment = NotificationSemanticTypes.WalletAdminAdjustment;

    /// <summary>پرداخت موفق سفارش از کیف پول.</summary>
    public const string WalletPaymentSucceeded = NotificationSemanticTypes.WalletPaymentSucceeded;

    /// <summary>اعتبار refund به کیف پول.</summary>
    public const string WalletRefundCredited = NotificationSemanticTypes.WalletRefundCredited;

    /// <summary>دستهٔ فیلتر UI (order/offer/ticket) از نوع معنایی.</summary>
    public static string CategoryOf(string type) => type switch
    {
        PaymentSucceeded or PaymentFailed or OrderPaidSeller or FulfillmentCreated or ShipmentDispatched => "order",
        ReturnRequested or ReturnApproved or RefundSucceeded => "order",
        SupportAdminReply => "ticket",
        WalletGiftCardRedeemed or WalletAdminAdjustment or WalletPaymentSucceeded or WalletRefundCredited => "offer",
        _ => "order",
    };

    /// <summary>عنوان و متن را از Type و payload در locale درخواستی می‌سازد.</summary>
    public static (string Title, string Body) Resolve(string type, string payloadJson, string locale)
    {
        var fa = IsPersian(locale);
        using var doc = Parse(payloadJson);
        var root = doc.RootElement;
        return type switch
        {
            PaymentSucceeded => fa
                ? ("پرداخت موفق", FormatAmountFa(root, "پرداخت سفارش شما با موفقیت انجام شد."))
                : ("Payment succeeded", FormatAmountEn(root, "Your order payment succeeded.")),
            PaymentFailed => fa
                ? ("پرداخت ناموفق", "پرداخت سفارش شما انجام نشد. در صورت کسر وجه، وضعیت را بررسی کنید.")
                : ("Payment failed", "Your payment did not succeed. Check status if charged."),
            OrderPaidSeller => fa
                ? ("سفارش جدید پرداخت‌شده", "یک سفارش پرداخت‌شده آمادهٔ اقدام شماست.")
                : ("New paid order", "A paid order is ready for your action."),
            FulfillmentCreated => fa
                ? ("آماده‌سازی سفارش", "سفارش شما برای آماده‌سازی ثبت شد.")
                : ("Order fulfillment started", "Your order is being prepared."),
            ShipmentDispatched => fa
                ? ("ارسال محموله", "محمولهٔ سفارش شما ارسال شد.")
                : ("Shipment dispatched", "Your order shipment was dispatched."),
            ReturnRequested => fa
                ? ("درخواست مرجوعی", "یک درخواست مرجوعی ثبت شد.")
                : ("Return requested", "A return request was submitted."),
            ReturnApproved => fa
                ? ("تأیید مرجوعی", "درخواست مرجوعی تأیید شد.")
                : ("Return approved", "The return request was approved."),
            RefundSucceeded => fa
                ? ("بازگشت وجه موفق", "بازگشت وجه مرجوعی با موفقیت انجام شد.")
                : ("Refund succeeded", "The return refund succeeded."),
            SupportAdminReply => fa
                ? ("پاسخ پشتیبانی", "پاسخ جدیدی برای تیکت پشتیبانی شما ثبت شد.")
                : ("Support reply", "There is a new reply on your support ticket."),
            WalletGiftCardRedeemed => fa
                ? ("کارت هدیه", "مبلغ کارت هدیه به کیف پول شما اضافه شد.")
                : ("Gift card", "Gift card value was credited to your wallet."),
            WalletAdminAdjustment => fa
                ? ("تعدیل کیف پول", "موجودی کیف پول شما توسط پشتیبانی به‌روز شد.")
                : ("Wallet adjustment", "Your wallet balance was updated by support."),
            WalletPaymentSucceeded => fa
                ? ("پرداخت با کیف پول", FormatAmountFa(root, "سفارش شما با کیف پول پرداخت شد."))
                : ("Wallet payment", FormatAmountEn(root, "Your order was paid with wallet.")),
            WalletRefundCredited => fa
                ? ("بازگشت به کیف پول", FormatAmountFa(root, "مبلغ مرجوعی به کیف پول شما واریز شد."))
                : ("Refund to wallet", FormatAmountEn(root, "Your refund was credited to your wallet.")),
            _ => fa
                ? ("اعلان", "رویداد تجاری جدید دارید.")
                : ("Notification", "You have a new commerce event."),
        };
    }

    /// <summary>payload امن JSON می‌سازد.</summary>
    public static string ToPayloadJson(object payload) =>
        JsonSerializer.Serialize(payload, PayloadJsonOptions);

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static bool IsPersian(string locale) =>
        string.IsNullOrWhiteSpace(locale)
        || locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);

    private static JsonDocument Parse(string payloadJson)
    {
        try
        {
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("{}");
        }
    }

    private static string FormatAmountFa(JsonElement root, string fallback)
    {
        if (root.TryGetProperty("amount", out var amount) && root.TryGetProperty("currency", out var currency))
        {
            return $"پرداخت به مبلغ {amount} {currency.GetString()} با موفقیت انجام شد.";
        }

        return fallback;
    }

    private static string FormatAmountEn(JsonElement root, string fallback)
    {
        if (root.TryGetProperty("amount", out var amount) && root.TryGetProperty("currency", out var currency))
        {
            return $"Payment of {amount} {currency.GetString()} succeeded.";
        }

        return fallback;
    }
}
