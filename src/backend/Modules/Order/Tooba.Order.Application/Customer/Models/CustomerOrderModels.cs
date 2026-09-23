namespace Tooba.Order.Application.Customer.Models;

/// <summary>
/// یک سفارش تجمیعی مشتری که ممکن است چند سفارش فروشنده داشته باشد.
/// </summary>
public sealed record CustomerOrderListItem(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    int SellerCount,
    int ItemCount,
    decimal PayableAmount,
    string Currency,
    string PaymentState,
    string Status);

/// <summary>
/// خط سفارش مشتری با مبلغ snapshot تاریخی و نام فروشنده.
/// </summary>
public sealed record CustomerOrderLineView(
    Guid OfferId,
    string Title,
    string SellerDisplayName,
    decimal Quantity,
    decimal UnitAmount,
    decimal LinePayable,
    string Currency);

/// <summary>
/// بخش فروشنده در جزئیات سفارش مشتری.
/// </summary>
public sealed record CustomerSellerOrderView(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    string PaymentState,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<CustomerOrderLineView> Lines);

/// <summary>
/// جزئیات سفارش فقط برای اصل احراز‌شده، همراه تصویر ارسال checkout.
/// </summary>
public sealed record CustomerOrderDetailPage(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    string Status,
    string PaymentState,
    decimal Subtotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Currency,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string ShippingMethodLabel,
    IReadOnlyList<CustomerSellerOrderView> SellerOrders,
    Guid? PaymentId = null,
    bool CanRetryUnpaid = false);

/// <summary>
/// خلاصهٔ سفارش‌های مشتری برای داشبورد Host (شمارنده‌ها + اخیر + کمک پروفایل).
/// </summary>
public sealed record CustomerOrderDashboardSummary(
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    IReadOnlyList<CustomerOrderListItem> RecentOrders,
    string? LatestOrderRecipientDisplayName,
    string? LatestShippingAddress,
    string? LatestContactMobile);
