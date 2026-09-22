using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Admin.OrdersGrid;

/// <summary>برچسب‌های فشردهٔ چرخه رزرو برای گرید؛ enum خام به UI نمی‌رود.</summary>
public static class OrderReservationCycleSummaryMapper
{
    /// <summary>برچسب فشرده گرید فارسی.</summary>
    public static string CompactFa(ReservationCycleStatus? status, int? cycleNumber) => status switch
    {
        ReservationCycleStatus.Active => cycleNumber is int n ? $"فعال #{n}" : "فعال",
        ReservationCycleStatus.Expired => cycleNumber is int n ? $"پایان‌یافته #{n}" : "پایان‌یافته",
        ReservationCycleStatus.CommittedPaid => "نهایی‌شده",
        ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy =>
            cycleNumber is int n ? $"آزادشده #{n}" : "آزادشده",
        ReservationCycleStatus.ReacquireFailed => "رزرو مجدد ناموفق",
        _ => "—",
    };

    /// <summary>برچسب فشرده گرید انگلیسی.</summary>
    public static string CompactEn(ReservationCycleStatus? status, int? cycleNumber) => status switch
    {
        ReservationCycleStatus.Active => cycleNumber is int n ? $"Active #{n}" : "Active",
        ReservationCycleStatus.Expired => cycleNumber is int n ? $"Expired #{n}" : "Expired",
        ReservationCycleStatus.CommittedPaid => "Committed",
        ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy =>
            cycleNumber is int n ? $"Released #{n}" : "Released",
        ReservationCycleStatus.ReacquireFailed => "Reacquire failed",
        _ => "—",
    };

    /// <summary>کلید فیلتر پایدار.</summary>
    public static string StateKey(ReservationCycleStatus? status) => status switch
    {
        ReservationCycleStatus.Active => "active",
        ReservationCycleStatus.Expired => "expired",
        ReservationCycleStatus.CommittedPaid => "committed",
        ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy => "released",
        ReservationCycleStatus.ReacquireFailed => "reacquireFailed",
        _ => "none",
    };

    /// <summary>خلاصهٔ خالی گرید.</summary>
    public static OrderReservationCycleSummary EmptySummary() =>
        new("—", "—", "none", null, false, false, false);

    /// <summary>خلاصهٔ فشرده برای گرید سفارش/پرداخت.</summary>
    public static OrderReservationCycleSummary ToSummary(ReservationCycleProjection? projection)
    {
        if (projection is null || (projection.CurrentStatus is null && projection.TotalCyclesCreated == 0))
        {
            return EmptySummary();
        }

        var status = projection.CurrentStatus;
        var retryLimit = projection.RetryCountRemaining <= 0
            && status is not ReservationCycleStatus.Active
            && status is not ReservationCycleStatus.CommittedPaid
            && projection.TotalCyclesCreated > 0;
        var retryPossible = projection.RetryCountRemaining > 0
            && status is ReservationCycleStatus.Expired or ReservationCycleStatus.ReleasedByPolicy;
        var needsReacquire = status is not ReservationCycleStatus.Active
            && status is not ReservationCycleStatus.CommittedPaid
            && projection.SupplyStatus is "AvailableForReacquire" or "Unavailable" or "PartiallyUnavailable";

        return new OrderReservationCycleSummary(
            CompactFa(status, projection.CurrentCycleNumber),
            CompactEn(status, projection.CurrentCycleNumber),
            StateKey(status),
            projection.CurrentCycleNumber,
            retryPossible,
            needsReacquire,
            retryLimit);
    }
}
