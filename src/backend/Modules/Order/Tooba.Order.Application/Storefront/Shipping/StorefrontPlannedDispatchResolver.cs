using Tooba.Order.Domain.Checkout;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Order.Application.Storefront.Shipping;

/// <summary>
/// TB-256: تبدیل انتخاب تأییدشدهٔ مشتری به <b>برنامهٔ ارسال</b> (پذیرش حامل) با همان مرجع زمان‌بندی.
/// </summary>
/// <remarks>
/// <para>
/// <b>یک محاسبه، دو مصرف.</b> نویسندهٔ تعهد از این کلاس برای ساخت شاهد استفاده می‌کند و
/// بازاعتبارسنجی checkout همان تابع را برای مقایسه صدا می‌زند؛ هیچ‌کدام محاسبهٔ تقویمی جداگانه ندارد.
/// </para>
/// <para>
/// <b>غیبت صریح.</b> نبود تقویم حامل یعنی بستهٔ قدیمی/تنظیم‌نشده: خروجی <c>null</c> با موفقیت است، نه
/// برنامهٔ حدسی از ساعت تحویل مشتری.
/// </para>
/// </remarks>
public static class StorefrontPlannedDispatchResolver
{
    /// <summary>تقویم حامل موجود است اما هویت snapshot/بازبینی/MAX معتبر برای بازتولید نیست.</summary>
    public const string Unavailable = "storefront.dispatch_plan.unavailable";

    /// <summary>برنامهٔ پذیرش حامل پیکربندی نشده است؛ تعهد جدید بدون آن پذیرفته نمی‌شود.</summary>
    public const string CarrierScheduleMissing = "storefront.dispatch_plan.carrier_schedule_missing";

    /// <summary>برای همین تاریخ تحویل، هیچ موعد پذیرش حاملی با قاعدهٔ رو‌به‌جلوی معتبر پیدا نشد.</summary>
    public const string NoFeasibleHandoff = "storefront.dispatch_plan.no_feasible_handoff";

    /// <summary>
    /// برنامهٔ ارسال تاریخ تحویل انتخاب‌شده را resolve می‌کند.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>تعهد جدید.</b> بدون تقویم حامل، برنامهٔ ارسال وجود ندارد و نتیجه شکست است؛ حدس زدن از ساعت
    /// تحویل مشتری یا پذیرفتن یک انتخاب ناقص ممنوع است.
    /// </para>
    /// <para>
    /// <b>خوانایی تاریخی.</b> <paramref name="allowAbsentCarrierSchedule"/> فقط برای بازاعتبارسنجی شاهدی
    /// است که خودش <c>null</c> ذخیره شده و هیچ تقویم حاملی نداشته؛ آن‌جا <c>null</c> صریح موفق است.
    /// </para>
    /// </remarks>
    /// <param name="quote">نتیجهٔ کامل قیمت capture‌شده (تقویم حامل/مشتری/MAX/snapshot).</param>
    /// <param name="receivingDate">تاریخ تحویل انتخاب‌شدهٔ همان گروه.</param>
    /// <param name="plan">برنامهٔ ارسال یا تهی.</param>
    /// <param name="reasonCode">کد دلیل پایدار شکست یا رشتهٔ خالی در موفقیت.</param>
    /// <param name="allowAbsentCarrierSchedule">فقط خوانایی شاهد تاریخی/بدون‌تقویم را مجاز می‌کند.</param>
    public static bool TryResolve(
        ShippingCapturedQuoteResult quote,
        DateOnly receivingDate,
        out ShippingGroupPlannedDispatch? plan,
        out string reasonCode,
        bool allowAbsentCarrierSchedule = false)
    {
        plan = null;
        reasonCode = string.Empty;

        if (quote.CarrierWeek is not { } carrierWeek)
        {
            if (allowAbsentCarrierSchedule)
            {
                return true;
            }

            reasonCode = CarrierScheduleMissing;
            return false;
        }

        if (quote.SnapshotId is not { } snapshotId
            || snapshotId == Guid.Empty
            || quote.SourceRevision is not { } sourceRevision
            || sourceRevision <= 0
            || quote.MaxDeliveryDays is not { } maximumDays)
        {
            reasonCode = Unavailable;
            return false;
        }

        var latest = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
            receivingDate,
            StorefrontShippingSchedulingMapping.ToSchedulingWeek(carrierWeek),
            maximumDays);

        if (!latest.IsSuccess)
        {
            reasonCode = NoFeasibleHandoff;
            return false;
        }

        var handoff = latest.Value!;

        // TB-264: when the method declares its physical preparation/transfer allowance, the durable plan
        // also carries the backward-derived deadlines. They are purely date+policy facts, so a later
        // revalidation of the SAME accepted plan reproduces them exactly.
        var preparationMinutes = quote.Settings?.PreparationMinutes;
        var handoffMinutes = quote.Settings?.HandoffMinutes;
        var timingConfigured = preparationMinutes is not null && handoffMinutes is not null;

        ShippingHandoffDeadlines? deadlines = null;
        if (timingConfigured)
        {
            var resolved = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
                receivingDate,
                StorefrontShippingSchedulingMapping.ToSchedulingWeek(carrierWeek),
                maximumDays,
                preparationMinutes!.Value,
                handoffMinutes!.Value);

            if (!resolved.IsSuccess)
            {
                reasonCode = NoFeasibleHandoff;
                return false;
            }

            deadlines = resolved.Value!;
        }

        plan = ShippingGroupPlannedDispatch.Create(
            handoff.Date,
            handoff.OpensAt.Ticks,
            handoff.ClosesAt.Ticks,
            carrierWeek.TimeZoneId,
            maximumDays,
            timingConfigured
                ? ShippingDispatchSchedulingAuthority.PreparationAwarePolicyVersion
                : ShippingDispatchSchedulingAuthority.PolicyVersion,
            snapshotId,
            sourceRevision,
            deadlines?.PreparationStartUtc,
            deadlines?.TransferDepartureUtc,
            deadlines?.HandoffDeadlineUtc,
            timingConfigured ? preparationMinutes : null,
            timingConfigured ? handoffMinutes : null);

        return true;
    }
}
