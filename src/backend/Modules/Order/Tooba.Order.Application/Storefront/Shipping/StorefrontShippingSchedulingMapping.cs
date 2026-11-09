using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Order.Application.Storefront.Shipping;

/// <summary>
/// TB-256: نگاشت <b>خالص</b> تقویم‌های capture‌شدهٔ Shipping به درز زمان‌بندی Contracts.
/// </summary>
/// <remarks>
/// <para>
/// این کلاس هیچ قاعدهٔ زمانی ندارد: نه ساعت ثابتی می‌سازد، نه روز تعطیل را کاری می‌کند و نه منطقهٔ
/// زمانی را تغییر می‌دهد. تنها کارش این است که همان مقدارهای ذخیره‌شده را به مدل مشترک برساند تا
/// projection و نویسندهٔ تعهد از <b>یک</b> مرجع محاسبه استفاده کنند.
/// </para>
/// <para>
/// برای روز تعطیلِ تقویم مشتری، ساعت ذخیره‌شده معنایی ندارد و صریحاً تهی می‌شود؛ ساعت حامل همان مقدار
/// ذخیره‌شده است (اعتبار کامل بودن/ترتیب در خود مرجع سنجیده می‌شود).
/// </para>
/// </remarks>
public static class StorefrontShippingSchedulingMapping
{
    /// <summary>تقویم مستقل پذیرش حامل.</summary>
    /// <param name="week">تقویم ذخیره‌شدهٔ حامل.</param>
    public static ShippingSchedulingWeekView ToSchedulingWeek(ShippingCapturedCarrierWeek week) =>
        new(
            week.TimeZoneId,
            week.Days
                .Select(day => new ShippingSchedulingDayView(day.Weekday, day.IsWorking, day.OpensAt, day.ClosesAt))
                .ToArray());

    /// <summary>تقویم تحویل مشتری.</summary>
    /// <param name="week">تقویم ذخیره‌شدهٔ مشتری.</param>
    public static ShippingSchedulingWeekView ToSchedulingWeek(ShippingCapturedReceivingWeek week) =>
        new(
            week.TimeZoneId,
            week.Days
                .Select(day => new ShippingSchedulingDayView(
                    day.Weekday,
                    day.IsWorking,
                    day.IsWorking ? day.StartTime : null,
                    day.IsWorking ? day.EndTime : null))
                .ToArray());
}
