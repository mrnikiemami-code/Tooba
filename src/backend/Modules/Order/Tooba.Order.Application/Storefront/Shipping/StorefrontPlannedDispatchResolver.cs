using Tooba.Order.Domain.Checkout;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Order.Application.Storefront.Shipping;

/// <summary>
/// TB-256/TB-264: تبدیل انتخاب تأییدشدهٔ مشتری به <b>برنامهٔ ارسال</b> (پذیرش حامل) با همان مرجع زمان‌بندی.
/// </summary>
/// <remarks>
/// <para>
/// <b>یک محاسبه، چند مصرف.</b> نویسندهٔ تعهد از این کلاس برای ساخت شاهد استفاده می‌کند و بازاعتبارسنجی
/// checkout همان تابع را با <b>همان نسخهٔ سیاست ثبت‌شده</b> صدا می‌زند؛ هیچ‌کدام محاسبهٔ تقویمی جداگانه
/// ندارد و شاهد قدیمی هرگز با معنای تازه بازتفسیر نمی‌شود.
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
    /// TB-264-R1: سیاست زمانی فیزیکی (آماده‌سازی + انتقال + معنای زمان پردازش) کامل نیست. تعهد جدید
    /// بدون آن ساخته نمی‌شود؛ «آمادهٔ همین لحظه» یک پیش‌فرض نیست.
    /// </summary>
    public const string TimingPolicyUnconfigured = "storefront.dispatch_plan.timing_unconfigured";

    /// <summary>
    /// TB-264-R1: نسخهٔ سیاست مؤثر برای تعهدهای <b>جدید</b>. شاهدهای ثبت‌شده با نسخهٔ خودشان
    /// بازاعتبارسنجی می‌شوند و هرگز به این نسخه ارتقا نمی‌یابند.
    /// </summary>
    public const int CurrentPolicyVersion = ShippingDispatchSchedulingAuthority.WindowAwarePolicyVersion;

    /// <summary>
    /// برنامهٔ ارسال یک <b>تعهد واقعی</b> (تاریخ + بازه + منطقه) را resolve می‌کند.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>تعهد جدید.</b> بدون تقویم حامل، بدون جفت زمان فیزیکی و بدون معنای صریح زمان پردازش، برنامهٔ
    /// ارسال وجود ندارد و نتیجه شکست است؛ حدس زدن از ساعت تحویل مشتری یا «آمادهٔ همین لحظه» ممنوع است.
    /// </para>
    /// <para>
    /// <b>خوانایی تاریخی.</b> <paramref name="policyVersion"/> همان نسخهٔ <b>ثبت‌شده</b> در شاهد موجود
    /// است؛ نسخهٔ ۱ بدون موعد، نسخهٔ ۲ با معنای تاریخ‌محور ۲۶۴ و نسخهٔ ۳ با تعهد پنجره/هفتهٔ پردازش.
    /// <paramref name="allowAbsentCarrierSchedule"/> فقط برای بازاعتبارسنجی شاهدی است که خودش <c>null</c>
    /// ذخیره شده و هیچ تقویم حاملی نداشته.
    /// </para>
    /// </remarks>
    /// <param name="quote">نتیجهٔ کامل قیمت capture‌شده (تقویم حامل/مشتری/MAX/snapshot).</param>
    /// <param name="receivingDate">تاریخ تحویل انتخاب‌شدهٔ همان گروه.</param>
    /// <param name="receivingOpenTicks">تیک شروع بازهٔ انتخاب‌شده در همان تاریخ.</param>
    /// <param name="receivingCloseTicks">تیک پایان بازهٔ انتخاب‌شده (بزرگ‌تر از شروع).</param>
    /// <param name="policyVersion">نسخهٔ سیاست مؤثر (ثبت‌شده برای شاهد، جاری برای تعهد جدید).</param>
    /// <param name="plan">برنامهٔ ارسال یا تهی.</param>
    /// <param name="reasonCode">کد دلیل پایدار شکست یا رشتهٔ خالی در موفقیت.</param>
    /// <param name="allowAbsentCarrierSchedule">فقط خوانایی شاهد تاریخی/بدون‌تقویم را مجاز می‌کند.</param>
    public static bool TryResolve(
        ShippingCapturedQuoteResult quote,
        DateOnly receivingDate,
        long receivingOpenTicks,
        long receivingCloseTicks,
        int policyVersion,
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

        if (policyVersion < ShippingDispatchSchedulingAuthority.PolicyVersion)
        {
            reasonCode = Unavailable;
            return false;
        }

        var carrier = StorefrontShippingSchedulingMapping.ToSchedulingWeek(carrierWeek);

        // Policy v1 is the historical «no physical timing» plan: the carrier handoff is the only fact and
        // no deadline is invented for it.
        if (policyVersion == ShippingDispatchSchedulingAuthority.PolicyVersion)
        {
            var legacy = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
                receivingDate,
                carrier,
                maximumDays);

            if (!legacy.IsSuccess)
            {
                reasonCode = NoFeasibleHandoff;
                return false;
            }

            plan = ShippingGroupPlannedDispatch.Create(
                legacy.Value!.Date,
                legacy.Value.OpensAt.Ticks,
                legacy.Value.ClosesAt.Ticks,
                carrierWeek.TimeZoneId,
                maximumDays,
                ShippingDispatchSchedulingAuthority.PolicyVersion,
                snapshotId,
                sourceRevision);

            return true;
        }

        var preparationMinutes = quote.Settings?.PreparationMinutes;
        var handoffMinutes = quote.Settings?.HandoffMinutes;
        if (preparationMinutes is null || handoffMinutes is null)
        {
            reasonCode = TimingPolicyUnconfigured;
            return false;
        }

        ShippingHandoffDeadlines? deadlines;
        ShippingHandoffPlan handoff;

        if (policyVersion == ShippingDispatchSchedulingAuthority.PreparationAwarePolicyVersion)
        {
            // TB-264 semantics, preserved verbatim for already-accepted v2 evidence: date-only backward
            // derivation with elapsed preparation (no processing week existed in that policy).
            var resolved = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
                receivingDate,
                carrier,
                maximumDays,
                preparationMinutes.Value,
                handoffMinutes.Value);

            if (!resolved.IsSuccess)
            {
                reasonCode = NoFeasibleHandoff;
                return false;
            }

            deadlines = resolved.Value!;
            handoff = deadlines.Handoff;
        }
        else
        {
            // TB-264-R1: new commitments are measured against the customer's REAL window, and preparation
            // is counted inside the explicitly configured processing semantics. A missing processing policy
            // is a configuration blocker, never an instant-ready default.
            var processingMode = quote.Settings?.ProcessingMode
                                 ?? ShippingAuthoringPreparationProcessingMode.Unspecified;
            if (processingMode == ShippingAuthoringPreparationProcessingMode.Unspecified)
            {
                reasonCode = TimingPolicyUnconfigured;
                return false;
            }

            var zone = quote.ReceivingWeek?.TimeZoneId;
            if (string.IsNullOrWhiteSpace(zone))
            {
                reasonCode = Unavailable;
                return false;
            }

            var commitment = new ShippingReceivingCommitment(
                receivingDate,
                new TimeOnly(receivingOpenTicks),
                new TimeOnly(receivingCloseTicks),
                zone);

            var processedWeek = processingMode == ShippingAuthoringPreparationProcessingMode.WorkingWeek
                ? quote.ProcessingWeek is { } week
                    ? StorefrontShippingSchedulingMapping.ToSchedulingWeek(week)
                    : null
                : null;

            if (processingMode == ShippingAuthoringPreparationProcessingMode.WorkingWeek && processedWeek is null)
            {
                reasonCode = TimingPolicyUnconfigured;
                return false;
            }

            var resolved = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
                commitment,
                carrier,
                maximumDays,
                preparationMinutes.Value,
                handoffMinutes.Value,
                processedWeek,
                processingContinuous: processingMode == ShippingAuthoringPreparationProcessingMode.Continuous);

            if (!resolved.IsSuccess)
            {
                reasonCode = MapSchedulingReason(resolved.ReasonCode);
                return false;
            }

            deadlines = resolved.Value!;
            handoff = deadlines.Handoff;
        }

        plan = ShippingGroupPlannedDispatch.Create(
            handoff.Date,
            handoff.OpensAt.Ticks,
            handoff.ClosesAt.Ticks,
            carrierWeek.TimeZoneId,
            maximumDays,
            policyVersion,
            snapshotId,
            sourceRevision,
            deadlines.PreparationStartUtc,
            deadlines.TransferDepartureUtc,
            deadlines.HandoffDeadlineUtc,
            preparationMinutes,
            handoffMinutes);

        return true;
    }

    /// <summary>
    /// کدهای پایدار «یک مرجع زمان‌بندی» را به واژگان پایدار همین درز نگاشت می‌کند تا مصرف‌کننده برای هر
    /// علت دقیقاً یک مانع قابل‌اقدام داشته باشد (بدون کد خام ماژول دیگر).
    /// </summary>
    private static string MapSchedulingReason(string reasonCode) => reasonCode switch
    {
        ShippingDispatchSchedulingReasons.TimingPolicyUnconfigured => TimingPolicyUnconfigured,
        ShippingDispatchSchedulingReasons.NoFeasibleHandoff => NoFeasibleHandoff,
        _ => Unavailable,
    };
}
