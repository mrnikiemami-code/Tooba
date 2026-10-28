using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Domain.Scheduling;

/// <summary>
/// قواعد مشترک زمان‌بندی Shipping: نرمال‌سازی شناسهٔ منطقهٔ زمانی و روزهای هفتهٔ ISO.
/// </summary>
/// <remarks>
/// <para>
/// این helper فقط <b>استخراج</b> همان قاعده‌ای است که پیش‌تر به‌صورت private داخل
/// <see cref="ShippingOperatingSchedule"/> بود: نبود/خالی بودن یا طول بیش از حد یک کد پایدار می‌گیرد و
/// هر شناسه‌ای که runtime واقعاً نمی‌تواند تفسیر کند با <c>shipping.schedule.timezone.invalid</c> رد
/// می‌شود. هیچ allow-list دستی، منطقهٔ پیش‌فرض یا استنتاج از locale/کشور ساخته نمی‌شود.
/// </para>
/// <para>
/// <b>یک منبع حقیقت.</b> برنامهٔ قدیمی (خدمت/گزینه) و برنامهٔ هفتگی مالک‌محور هر دو از همین متد استفاده
/// می‌کنند تا دو سیاست واگرا برای یک مفهوم ساخته نشود. رفتار و کدهای خطای برنامهٔ قدیمی تغییر نمی‌کند.
/// </para>
/// </remarks>
public static class ShippingScheduleRules
{
    /// <summary>
    /// منطقهٔ زمانی را نرمال و اعتبارسنجی می‌کند. یک شناسهٔ غیرقابل‌حل یعنی بازه‌ها به وقت محلی
    /// نامعلومی تفسیر می‌شوند؛ چنین برنامه‌ای ذخیره نمی‌شود.
    /// </summary>
    /// <param name="timeZoneId">شناسهٔ منطقهٔ زمانی (ترجیحاً IANA).</param>
    /// <exception cref="SemanticException">
    /// نبود/خالی‌بودن (<c>shipping.schedule.timezone.required</c>)، طول بیش از حد یا شناسهٔ غیرقابل‌حل
    /// (<c>shipping.schedule.timezone.invalid</c>).
    /// </exception>
    public static string NormalizeTimeZone(string? timeZoneId)
    {
        var trimmed = timeZoneId?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleTimeZoneRequired));
        }

        if (trimmed.Length > ShippingRatePolicyLimits.TimeZoneIdMaxLength)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleTimeZoneInvalid));
        }

        try
        {
            // IANA preferred; Windows ids still resolve on .NET, so the check accepts exactly what the
            // runtime can actually interpret rather than a hand-maintained allow-list that would rot.
            _ = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleTimeZoneInvalid));
        }
        catch (InvalidTimeZoneException)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleTimeZoneInvalid));
        }

        return trimmed;
    }

    /// <summary>
    /// TB-241: مدت اختیاری هر نوبت تحویل را نرمال می‌کند. تهی یعنی «یک پنجرهٔ کامل روز» و هرگز به
    /// ۶۰ دقیقه استنتاج نمی‌شود؛ مقدار غیرصحیح/صفر/منفی/بزرگ‌تر از ۱۴۴۰ پیش از هر نوشتن رد می‌شود.
    /// </summary>
    /// <param name="slotDurationMinutes">مدت پیشنهادی به دقیقه یا تهی.</param>
    /// <exception cref="SemanticException">مقدار خارج از ۱..۱۴۴۰ با کد پایدار <c>shipping.schedule.slot_duration.invalid</c>.</exception>
    public static int? NormalizeSlotDuration(int? slotDurationMinutes)
    {
        if (slotDurationMinutes is not { } minutes)
        {
            return null;
        }

        if (minutes <= 0 || minutes > MaxSlotDurationMinutes)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleSlotDurationInvalid));
        }

        return minutes;
    }

    /// <summary>بیشینهٔ صریح مدت هر نوبت تحویل: یک شبانه‌روز کامل.</summary>
    public const int MaxSlotDurationMinutes = 1440;

    /// <summary>
    /// TB-241-R1: تعداد نوبت‌های کاملِ هم‌اندازه در یک بازهٔ هم‌روز با حساب تیک صحیح (نه
    /// <see cref="TimeOnly.Add"/>). باقی‌مانده هرگز نوبت کوتاه‌تر نمی‌سازد؛ بازهٔ معکوس/تهی صفر است.
    /// </summary>
    /// <param name="opensAt">شروع شامل بازهٔ محلی.</param>
    /// <param name="closesAt">پایان غیرشامل بازهٔ محلی.</param>
    /// <param name="slotDurationMinutes">مدت هر نوبت (۱..۱۴۴۰).</param>
    /// <returns>تعداد نوبت کامل؛ صفر وقتی حتی یک نوبت در بازه جا نمی‌شود.</returns>
    public static int CountCompleteSlots(TimeOnly opensAt, TimeOnly closesAt, int slotDurationMinutes)
    {
        if (slotDurationMinutes < 1 || slotDurationMinutes > MaxSlotDurationMinutes)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleSlotDurationInvalid));
        }

        var lengthTicks = checked((long)slotDurationMinutes * TimeSpan.TicksPerMinute);
        var openTicks = opensAt.Ticks;
        var closeTicks = closesAt.Ticks;
        var windowTicks = closeTicks >= openTicks ? closeTicks - openTicks : 0L;
        return (int)(windowTicks / lengthTicks);
    }
}
