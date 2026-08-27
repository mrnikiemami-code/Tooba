using System.Text.Json;
using Tooba.BuildingBlocks;
using Tooba.Notification.Domain;

namespace Tooba.Notification.Application;

/// <summary>
/// فرمان ایجاد idempotent اعلان.
/// </summary>
public sealed record CreateNotificationCommand(
    NotificationRecipientKind RecipientKind,
    Guid RecipientPartyId,
    Guid? RecipientActorUserId,
    string Type,
    object Payload,
    string TargetRoute,
    string SourceEventId,
    string SourceType);

/// <summary>
/// فیلتر فهرست اعلان‌های یک گیرنده.
/// </summary>
public sealed record NotificationRecipientQuery(
    NotificationRecipientKind RecipientKind,
    Guid RecipientPartyId,
    Guid? RecipientActorUserId,
    int Skip,
    int Take,
    string Locale);

/// <summary>
/// آیتم فهرست اعلان با عنوان/متن محلی‌سازی‌شده در زمان خواندن.
/// </summary>
public sealed record NotificationListItemDto(
    Guid NotificationId,
    string Type,
    string Category,
    string Title,
    string Body,
    string PayloadJson,
    string TargetRoute,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt,
    string SourceType);

/// <summary>
/// صفحهٔ فهرست.
/// </summary>
public sealed record NotificationListPage(
    IReadOnlyList<NotificationListItemDto> Items,
    int Skip,
    int Take,
    long TotalCount,
    long UnreadCount);

/// <summary>
/// دایرکتوری اعلان‌های تراکنشی پایدار.
/// </summary>
public interface INotificationDirectory
{
    /// <summary>اگر SourceEventId تکراری باشد ایجاد نمی‌کند.</summary>
    Task<UserNotification?> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken);

    /// <summary>فهرست گیرنده به‌ترتیب جدیدترین.</summary>
    Task<NotificationListPage> ListAsync(NotificationRecipientQuery query, CancellationToken cancellationToken);

    /// <summary>تعداد خوانده‌نشدهٔ گیرنده.</summary>
    Task<long> UnreadCountAsync(
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);

    /// <summary>علامت خوانده‌شدن یک اعلان متعلق به گیرنده؛ idempotent.</summary>
    Task<bool> MarkReadAsync(
        Guid notificationId,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);

    /// <summary>همهٔ خوانده‌نشده‌های گیرنده را می‌خواند؛ idempotent.</summary>
    Task<int> MarkAllReadAsync(
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);

    /// <summary>حذف نرم اعلان متعلق به گیرنده.</summary>
    Task<bool> SoftDeleteAsync(
        Guid notificationId,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken);
}

/// <summary>
/// مسیرهای نسبی امن برای deep-link اعلان.
/// </summary>
public static class NotificationTargetRoutes
{
    private static readonly HashSet<string> AllowedPrefixes =
    [
        "/customer-panel",
        "/vendor-panel",
        "/payment",
        "/checkout",
        "/cart",
    ];

    /// <summary>مسیر نسبی allowlist را اعتبارسنجی و نرمال می‌کند.</summary>
    public static string RequireAllowed(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            throw new InvalidOperationException("TargetRoute خالی است.");
        }

        var trimmed = route.Trim();
        if (!trimmed.StartsWith('/')
            || trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal)
            || trimmed.Contains(':', StringComparison.Ordinal)
            || trimmed.Contains('<', StringComparison.Ordinal)
            || trimmed.Contains('>', StringComparison.Ordinal)
            || trimmed.Contains('"', StringComparison.Ordinal)
            || trimmed.Contains('\'', StringComparison.Ordinal)
            || trimmed.Contains("javascript", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("TargetRoute ناامن است.");
        }

        if (!AllowedPrefixes.Any(prefix =>
                trimmed.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("TargetRoute خارج از allowlist است.");
        }

        return trimmed;
    }

    /// <summary>مسیر سفارش مشتری.</summary>
    public static string CustomerOrder(Guid checkoutId) =>
        RequireAllowed($"/customer-panel/orders/{checkoutId:D}");

    /// <summary>مسیر نتیجهٔ پرداخت مشتری.</summary>
    public static string CustomerPaymentResult(Guid checkoutId) =>
        RequireAllowed($"/payment/result?checkoutId={checkoutId:D}");

    /// <summary>مسیر سفارش فروشنده.</summary>
    public static string SellerOrder(Guid sellerOrderId) =>
        RequireAllowed($"/vendor-panel/orders/{sellerOrderId:D}");

    /// <summary>مسیر مرجوعی فروشنده.</summary>
    public static string SellerReturn(Guid returnRequestId) =>
        RequireAllowed($"/vendor-panel/returns/{returnRequestId:D}");

    /// <summary>مسیر مرجوعی مشتری.</summary>
    public static string CustomerReturn(Guid returnRequestId) =>
        RequireAllowed($"/customer-panel/returns/{returnRequestId:D}");

    /// <summary>مسیر تیکت پشتیبانی مشتری.</summary>
    public static string CustomerTicket(Guid ticketId) =>
        RequireAllowed($"/customer-panel/tickets/{ticketId:D}");

    /// <summary>مسیر تیکت پشتیبانی فروشنده.</summary>
    public static string SellerTicket(Guid ticketId) =>
        RequireAllowed($"/vendor-panel/tickets/{ticketId:D}");

    /// <summary>مسیر کیف پول مشتری.</summary>
    public static string CustomerWallet() =>
        RequireAllowed("/customer-panel/wallet");
}

/// <summary>
/// انواع معنایی اعلان و کپی محلی در زمان خواندن.
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
    public const string SupportAdminReply = "support.admin_reply";

    /// <summary>بازخرید کارت هدیه به کیف پول.</summary>
    public const string WalletGiftCardRedeemed = "wallet.gift_card.redeemed";

    /// <summary>تعدیل Admin روی کیف پول مشتری.</summary>
    public const string WalletAdminAdjustment = "wallet.admin_adjustment";

    /// <summary>پرداخت موفق سفارش از کیف پول.</summary>
    public const string WalletPaymentSucceeded = "wallet.payment.succeeded";

    /// <summary>اعتبار refund به کیف پول.</summary>
    public const string WalletRefundCredited = "wallet.refund.credited";

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
