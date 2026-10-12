using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.ReservationCycle.Policies;

/// <summary>
/// صرفهٔ پرداخت یک سفارش unpaid بر پایهٔ تصویر چرخهٔ رزرو.
/// تنها منبع حقیقت برای پرسش «آیا این سفارش هنوز قابل پرداخت است؟» است تا تصویر
/// «در انتظار پرداخت» و سقف سفارش‌های باز، یک سیاست را دوبار پیاده نکنند.
/// </summary>
public static class ReservationCyclePayabilityPolicy
{
    /// <summary>چرخهٔ فعال با مهلت باقی‌مانده؛ سفارش هنوز قابل پرداخت است.</summary>
    public static bool IsActive(ReservationCycleProjection? cycle) =>
        cycle is { CurrentStatus: ReservationCycleStatus.Active, SecondsRemaining: > 0 };

    /// <summary>
    /// سقف رزرو مجدد مصرف شده است: نه چرخهٔ فعالی مانده و نه شمارهٔ چرخهٔ تازه‌ای.
    /// چنین سفارشی دیگر قابل پرداخت یا بازگردانی نیست و نباید سهم «سفارش باز» بگیرد.
    /// </summary>
    public static bool HasReachedRetryLimit(ReservationCycleProjection? cycle) =>
        cycle is { } current
        && !IsActive(current)
        && current.RetryCountRemaining <= 0
        && current.TotalCyclesCreated >= current.EffectiveMaxCycles;
}
