namespace Tooba.Order.Application.Admin.Dashboard.Models;

/// <summary>فقط شمارنده‌های Order برای داشبورد مدیر (ترکیب Host با Catalog/Offer).</summary>
public sealed record AdminOrderDashboardMetrics(
    int OpenOrders,
    int PaidOrders,
    int PendingOrders,
    int Customers);
