using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Domain.Scheduling;

/// <summary>
/// ارزیاب قطعی و فقط-محاسباتی در دسترس بودن بازهٔ تحویل به حامل. هیچ ورودی/خروجی، هیچ DbContext و
/// هیچ خواندن از ساعت سیستم ندارد: لحظهٔ ارزیابی صریحاً داده می‌شود تا نتیجه قابل بازتولید باشد.
/// </summary>
/// <remarks>
/// <para>
/// <b>برنامه ≠ وعده.</b> این ارزیاب می‌گوید «آیا حامل در این بازهٔ محلی مرسوله را می‌پذیرد»، نه
/// «مشتری چه زمانی تحویل می‌گیرد». هیچ وعده‌ای اینجا ساخته یا تغییر داده نمی‌شود.
/// </para>
/// <para>
/// <b>Fail-open نسبت به گذشته.</b> نبود برنامه و برنامهٔ غیرفعال هر دو هیچ محدودیتی اعمال
/// نمی‌کنند؛ فقط یک برنامهٔ <em>فعال</em> می‌تواند محدودیت بسازد. این تنها راهی است که افزودن
/// این ارزیاب، رفتار روش‌های قدیمی را تغییر ندهد.
/// </para>
/// <para>
/// <b>ابهام حل نمی‌شود.</b> دو برنامهٔ فعال هم‌سطح برای یک هویت یک نقص داده است و صریحاً
/// <see cref="ShippingScheduleAvailability.Ambiguous"/> گزارش می‌شود، نه انتخاب سطر اول.
/// </para>
/// <para>
/// <b>منطقهٔ زمانی غیرقابل‌حل fail-closed است.</b> اگر شناسهٔ ذخیره‌شده روی این runtime حل نشود،
/// هیچ ادعای در دسترس بودنی نمی‌شود، چون «13:00» در یک منطقهٔ نامعلوم بی‌معنا است.
/// </para>
/// <para>
/// <b>نیمه‌شب و DST.</b> تبدیل از UTC به وقت محلی قطعی است: هر لحظهٔ UTC دقیقاً یک وقت محلی دارد،
/// پس ساعت نامعتبر/مبهم (که فقط در تبدیل محلی→UTC رخ می‌دهد) اینجا قابل بروز نیست. پنجرهٔ شبانه
/// (عبور از نیمه‌شب) در Domain رد می‌شود، پس «امروز یا فردا» ابهام نمی‌سازد.
/// </para>
/// </remarks>
public static class ShippingScheduleAvailabilityEvaluator
{
    /// <summary>
    /// در دسترس بودن بازهٔ حامل را برای یک لحظهٔ درخواستی ارزیابی می‌کند.
    /// </summary>
    /// <param name="schedules">
    /// برنامه‌های همان کد خدمت؛ ممکن است خالی باشد (که یعنی هیچ برنامه‌ای ثبت نشده).
    /// </param>
    /// <param name="serviceCode">کد خدمت درخواست.</param>
    /// <param name="optionCode">گزینهٔ درخواست؛ تهی یعنی فقط در سطح خدمت.</param>
    /// <param name="requestedPickupAtUtc">لحظهٔ درخواستی به UTC؛ تهی یعنی ارزیابی انجام نشود.</param>
    /// <returns>ارزیابی قطعی؛ هرگز null.</returns>
    public static ShippingScheduleEvaluation Evaluate(
        IReadOnlyCollection<ShippingOperatingSchedule>? schedules,
        string serviceCode,
        string? optionCode,
        DateTimeOffset? requestedPickupAtUtc)
    {
        var code = serviceCode?.Trim() ?? string.Empty;
        var option = NormalizeOption(optionCode);

        var forService = (schedules ?? Array.Empty<ShippingOperatingSchedule>())
            .Where(x => string.Equals(x.ServiceCode, code, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (forService.Length == 0)
        {
            // No schedule is a real answer: NO explicit operating policy, so nothing is enforced.
            return new ShippingScheduleEvaluation(
                ShippingScheduleAvailability.NotConfigured,
                ShippingScheduleEvaluationReasonCodes.NotConfigured);
        }

        // Option identity first, exactly like the rate/packaging engines: an option-specific schedule
        // matching THIS option wins, and the service-level schedule is the documented explicit fallback.
        // A schedule belonging to a DIFFERENT option is never applied: it describes another method.
        var applicable = forService
            .Where(x => !x.IsOptionSpecific || Matches(x.OptionCode, option))
            .ToArray();

        var candidates = applicable.Any(x => x.IsOptionSpecific)
            ? applicable.Where(x => x.IsOptionSpecific).ToArray()
            : applicable.Where(x => !x.IsOptionSpecific).ToArray();

        if (candidates.Length == 0)
        {
            // An option-specific schedule exists for a DIFFERENT option: that schedule describes another
            // method and must never be applied to this request. No applicable policy means nothing to enforce.
            return new ShippingScheduleEvaluation(
                ShippingScheduleAvailability.NotConfigured,
                ShippingScheduleEvaluationReasonCodes.NotConfigured);
        }

        if (candidates.Length > 1)
        {
            // Two active schedules for the same identity. The pair is a data conflict that only a human may
            // resolve, so availability is not claimed in either direction.
            return new ShippingScheduleEvaluation(
                ShippingScheduleAvailability.Ambiguous,
                ShippingScheduleEvaluationReasonCodes.Ambiguous);
        }

        var schedule = candidates[0];

        if (!schedule.IsActive)
        {
            // Documented safe meaning of an inactive schedule: it is NOT an operational restriction.
            // Treating it as "unavailable" would silently close a method that legacy Checkout still offers.
            return new ShippingScheduleEvaluation(
                ShippingScheduleAvailability.InactiveNotEnforced,
                ShippingScheduleEvaluationReasonCodes.InactiveNotEnforced);
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return UnresolvableTimeZone();
        }
        catch (InvalidTimeZoneException)
        {
            return UnresolvableTimeZone();
        }

        if (requestedPickupAtUtc is not { } requested)
        {
            // No requested instant means no window claim at all - never a success-shaped "available".
            return new ShippingScheduleEvaluation(
                ShippingScheduleAvailability.NotEvaluated,
                ShippingScheduleEvaluationReasonCodes.NotEvaluated,
                schedule.TimeZoneId,
                ConfiguredCapacityTotal: schedule.ConfiguredCapacityTotal);
        }

        var local = TimeZoneInfo.ConvertTime(requested, timeZone);
        var localWeekday = IsoWeekday(local.DayOfWeek);
        var localTime = TimeOnly.FromDateTime(local.DateTime);

        var matched = schedule.Windows
            .Where(x => x.Weekday == localWeekday && x.ContainsLocal(localTime))
            .OrderBy(x => x.StartTime)
            .ToArray();

        return matched.Length > 0
            ? new ShippingScheduleEvaluation(
                ShippingScheduleAvailability.Available,
                ShippingScheduleEvaluationReasonCodes.Available,
                schedule.TimeZoneId,
                localWeekday,
                localTime,
                matched[0].WindowId,
                schedule.ConfiguredCapacityTotal)
            : new ShippingScheduleEvaluation(
                ShippingScheduleAvailability.Unavailable,
                ShippingScheduleEvaluationReasonCodes.OutsideWindows,
                schedule.TimeZoneId,
                localWeekday,
                localTime,
                ConfiguredCapacityTotal: schedule.ConfiguredCapacityTotal);
    }

    private static ShippingScheduleEvaluation UnresolvableTimeZone() => new(
        ShippingScheduleAvailability.Ambiguous,
        ShippingScheduleEvaluationReasonCodes.TimeZoneUnresolvable);

    /// <summary>روز هفتهٔ ISO: ۱ دوشنبه تا ۷ یکشنبه.</summary>
    /// <param name="dayOfWeek">روز هفتهٔ .NET.</param>
    private static int IsoWeekday(DayOfWeek dayOfWeek) => dayOfWeek == DayOfWeek.Sunday
        ? 7
        : (int)dayOfWeek;

    private static bool Matches(string? stored, string? requested) =>
        string.Equals(stored ?? string.Empty, requested ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeOption(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
