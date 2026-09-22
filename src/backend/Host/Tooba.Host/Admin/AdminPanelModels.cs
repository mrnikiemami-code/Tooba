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

/// <summary>خلاصهٔ فشرده چرخه رزرو برای گرید.</summary>
public sealed record AdminReservationCycleSummary(
    string CompactLabelFa,
    string CompactLabelEn,
    string State,
    int? CycleNumber,
    bool RetryPossible,
    bool NeedsReacquire,
    bool RetryLimitReached);

/// <summary>
/// مشتری صادقانهٔ عملیاتی بر پایهٔ User سفارش و آخرین snapshot گیرنده.
/// </summary>
public sealed record AdminCustomerListItem(
    Guid CustomerUserId,
    string DisplayName,
    string? ContactMobile,
    int OrderCount,
    DateTimeOffset LastOrderAt,
    string Status);
