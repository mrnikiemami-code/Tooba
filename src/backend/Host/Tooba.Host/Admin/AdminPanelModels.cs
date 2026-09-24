namespace Tooba.Host.Admin;

/// <summary>
/// شمارنده‌های عملیاتی داشبورد مدیر که فقط از دادهٔ واقعی ماژول‌ها ساخته می‌شوند.
/// </summary>
public sealed record AdminDashboardSummary(
    int PublishedProducts,
    int ActiveOffers,
    int OpenOrders,
    int PaidOrders,
    int PendingOrders,
    int Sellers,
    int Customers);

// R3: AdminOrderListItem moved to Tooba.Order.Application.Admin.OrdersGrid.Models.
// R6: Admin order detail DTOs moved to Tooba.Order.Application.Admin.Detail.Models.
// R11: AdminCustomerListItem moved to Tooba.Order.Application.Admin.Customers.Models.
// R11: AdminReservationCycleMapper / Host reservation summary DTOs removed (Order Detail owns mapping).
// R12: AdminReceiptListItem removed — the admin payments grid row is Payment-owned (AdminPaymentGridItemDto).

/// <summary>
/// ردیف فروشنده از Party و شمارنده‌های جداگانهٔ Offer/Order.
/// </summary>
public sealed record AdminSellerListItem(
    Guid SellerPartyId,
    string DisplayName,
    string Status,
    int ActiveOffers,
    int OrderCount);
