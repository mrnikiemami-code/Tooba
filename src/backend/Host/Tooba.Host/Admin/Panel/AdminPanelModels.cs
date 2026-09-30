namespace Tooba.Host.Admin.Panel;

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
// TB-TMAR-HOST-GRID-AMC-001-R4: AdminSellerListItem moved to Tooba.Party.Contracts.
