namespace Tooba.Order.Application.Admin.OrdersGrid.Models;

/// <summary>
/// ردیف سفارش تجمیعی مدیر بر پایهٔ Checkout و snapshotهای سفارش.
/// ترتیب و نام فیلدها قرارداد wire گرید است.
/// </summary>
public sealed record AdminOrderListItem(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    string CustomerDisplayName,
    int SellerCount,
    string SellerDisplayNames,
    int LineCount,
    decimal PayableAmount,
    string Currency,
    string PaymentState,
    string Status,
    string SupplyStatus = "NotApplicable",
    string ReservationLabel = "—",
    string ReservationLabelEn = "—",
    string ReservationState = "none",
    int? ReservationCycleNumber = null,
    bool ReservationRetryPossible = false,
    bool ReservationNeedsReacquire = false,
    bool ReservationRetryLimitReached = false);

/// <summary>خلاصهٔ فشرده چرخه رزرو برای گرید سفارش.</summary>
public sealed record OrderReservationCycleSummary(
    string CompactLabelFa,
    string CompactLabelEn,
    string State,
    int? CycleNumber,
    bool RetryPossible,
    bool NeedsReacquire,
    bool RetryLimitReached);
