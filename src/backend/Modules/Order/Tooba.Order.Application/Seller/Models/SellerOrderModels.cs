namespace Tooba.Order.Application.Seller.Models;

/// <summary>ردیف فهرست سفارش فروشنده. خطوط فروشندهٔ دیگر دیده نمی‌شود.</summary>
public sealed record SellerOrderListItem(
    Guid SellerOrderId,
    string OrderNumber,
    DateTimeOffset SubmittedAt,
    string RecipientName,
    int LineCount,
    decimal PayableAmount,
    string Currency,
    string PaymentState,
    string Status);

/// <summary>خط سفارش فروشنده با عنوان snapshot/عرضه‌شده.</summary>
public sealed record SellerOrderLineView(
    Guid OfferId,
    string Title,
    decimal Quantity,
    decimal UnitAmount,
    decimal LinePayable,
    string Currency);

/// <summary>جزئیات سفارش متعلق به همان فروشنده.</summary>
public sealed record SellerOrderDetailPage(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
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
    IReadOnlyList<SellerOrderLineView> Lines);

/// <summary>خلاصهٔ شمارش سفارش برای داشبورد فروشنده (فقط Order-owned).</summary>
public sealed record SellerOrderDashboardSummary(int OpenOrders, int PaidOrders);
