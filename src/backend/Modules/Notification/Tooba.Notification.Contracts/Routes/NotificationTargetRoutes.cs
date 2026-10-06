using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Errors;

namespace Tooba.Notification.Contracts.Routes;

/// <summary>
/// مسیرهای نسبی امن برای deep-link اعلان (قرارداد عمومی).
/// نقض allowlist یک خطای تایپ‌شده با کد پایدار ماژول است، نه استثنای خام.
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
            throw new ContractOperationException(NotificationErrorCodes.TargetRouteEmpty);

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
            throw new ContractOperationException(NotificationErrorCodes.TargetRouteUnsafe);
        }

        if (!AllowedPrefixes.Any(prefix =>
                trimmed.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ContractOperationException(NotificationErrorCodes.TargetRouteNotAllowed);
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
