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

/// <summary>
/// ردیف دریافت مشتری (پرداخت) برای گرید Admin.
/// </summary>
public sealed record AdminReceiptListItem(
    Guid PaymentId,
    Guid CheckoutId,
    string OrderReference,
    string CustomerDisplayName,
    decimal Amount,
    string Currency,
    string Status,
    string ProviderCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string SupplyStatus = "NotApplicable",
    string ReservationLabel = "—",
    string ReservationLabelEn = "—",
    string ReservationState = "none",
    int? ReservationCycleNumber = null,
    bool ReservationRetryPossible = false,
    bool ReservationNeedsReacquire = false,
    bool ReservationRetryLimitReached = false);

/// <summary>
/// ردیف فروشنده از Party و شمارنده‌های جداگانهٔ Offer/Order.
/// </summary>
public sealed record AdminSellerListItem(
    Guid SellerPartyId,
    string DisplayName,
    string Status,
    int ActiveOffers,
    int OrderCount);
