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

    /// <summary>برای همین تاریخ تحویل، هیچ موعد پذیرش حاملی با قاعدهٔ رو‌به‌جلوی معتبر پیدا نشد.</summary>
    public const string NoFeasibleHandoff = "storefront.dispatch_plan.no_feasible_handoff";

    /// <summary>
    /// برنامهٔ ارسال تاریخ تحویل انتخاب‌شده را resolve می‌کند. موفقیت با <paramref name="plan"/> تهی
    /// یعنی «تقویم حامل پیکربندی نشده» (بستهٔ قدیمی)، نه شکست.
    /// </summary>
    /// <param name="quote">نتیجهٔ کامل قیمت capture‌شده (تقویم حامل/مشتری/MAX/snapshot).</param>
    /// <param name="receivingDate">تاریخ تحویل انتخاب‌شدهٔ همان گروه.</param>
    /// <param name="plan">برنامهٔ ارسال یا تهی.</param>
    /// <param name="reasonCode">کد دلیل پایدار شکست یا رشتهٔ خالی در موفقیت.</param>
    public static bool TryResolve(
        ShippingCapturedQuoteResult quote,
        DateOnly receivingDate,
        out ShippingGroupPlannedDispatch? plan,
        out string reasonCode)
    {
        plan = null;
        reasonCode = string.Empty;

        if (quote.CarrierWeek is not { } carrierWeek)
        {
            return true;
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
        plan = ShippingGroupPlannedDispatch.Create(
            handoff.Date,
            handoff.OpensAt.Ticks,
            handoff.ClosesAt.Ticks,
            carrierWeek.TimeZoneId,
            maximumDays,
            ShippingDispatchSchedulingAuthority.PolicyVersion,
            snapshotId,
            sourceRevision);

        return true;
    }
}
