using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Domain.Scheduling;

/// <summary>
/// یک پنجرهٔ عملیاتی محلی روی یک روز هفته: بازهٔ نیم‌باز <c>[StartTime, EndTime)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>معنای دامنه.</b> این پنجره «در دسترس بودن تحویل به حامل» است، نه «زمان وعده‌داده‌شده به
/// مشتری». مالک وعدهٔ مشتری Order است؛ Shipping اینجا فقط می‌گوید حامل در این بازهٔ محلی می‌تواند
/// مرسوله را بپذیرد.
/// </para>
/// <para>
/// <b>چرا نیم‌باز.</b> با <c>[start,end)</c> دو پنجرهٔ چسبیده (13:00–14:00 و 14:00–15:00) هم‌پوشانی
/// ندارند و مرز دقیقاً یک‌بار پوشش داده می‌شود. اگر مرز بسته بود، هر پنجرهٔ چسبیده یک نقطهٔ اشتراک
/// ساختگی پیدا می‌کرد و هم‌پوشانی‌سنجی به‌غلط رد می‌کرد.
/// </para>
/// <para>
/// <b>پنجرهٔ شبانه پشتیبانی نمی‌شود.</b> <c>end &gt; start</c> شرط سخت است، پس عبور از نیمه‌شب
/// (مثلاً 22:00–02:00) رد می‌شود. پشتیبانی از آن نیازمند تقسیم به دو پنجرهٔ همان روز و روز بعد
/// است که تصمیم صریحی است و در این نسخه گرفته نشده؛ ابهام خاموش بهتر از پشتیبانی نصفه نیست.
/// </para>
/// </remarks>
public sealed class ShippingOperatingWindow
{
    /// <summary>کمترین روز هفتهٔ ISO (دوشنبه).</summary>
    public const int MinWeekday = 1;

    /// <summary>بیشترین روز هفتهٔ ISO (یکشنبه).</summary>
    public const int MaxWeekday = 7;

    /// <summary>بیشترین ظرفیت قابل تنظیم برای یک پنجره.</summary>
    public const int CapacityMaxValue = 1_000_000;

    private ShippingOperatingWindow()
    {
    }

    /// <summary>شناسهٔ پایدار پنجره.</summary>
    public Guid WindowId { get; init; }

    /// <summary>روز هفتهٔ ISO: ۱ دوشنبه تا ۷ یکشنبه.</summary>
    public int Weekday { get; private set; }

    /// <summary>شروع پنجره به وقت محلی منطقهٔ زمانی برنامه؛ شامل.</summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>پایان پنجره به وقت محلی منطقهٔ زمانی برنامه؛ <b>شامل نیست</b>.</summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>
    /// ظرفیت اختیاری این پنجره. تنها یک <em>پیکربندی</em> است: هیچ شمارش/رزرو اتمی پشت آن نیست،
    /// پس این مقدار ظرفیت را اجرا نمی‌کند.
    /// </summary>
    public int? Capacity { get; private set; }

    /// <summary>یک پنجرهٔ عملیاتی معتبر می‌سازد.</summary>
    /// <param name="weekday">روز هفتهٔ ISO ۱..۷.</param>
    /// <param name="startTime">شروع؛ شامل.</param>
    /// <param name="endTime">پایان؛ شامل نیست و باید اکیداً بعد از شروع باشد.</param>
    /// <param name="capacity">ظرفیت اختیاری؛ در صورت وجود باید مثبت باشد.</param>
    /// <param name="windowId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    public static ShippingOperatingWindow Create(
        int weekday,
        TimeOnly startTime,
        TimeOnly endTime,
        int? capacity = null,
        Guid? windowId = null)
        => new()
        {
            WindowId = windowId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            Weekday = NormalizeWeekday(weekday),
            StartTime = startTime,
            EndTime = NormalizeRange(startTime, endTime),
            Capacity = NormalizeCapacity(capacity),
        };

    /// <summary>
    /// آیا این پنجره با پنجرهٔ دیگری روی همان روز هفته هم‌پوشانی دارد. با معنای نیم‌باز
    /// <c>[start,end)</c> دو پنجرهٔ چسبیده هم‌پوشانی ندارند.
    /// </summary>
    /// <param name="other">پنجرهٔ دیگر.</param>
    public bool Overlaps(ShippingOperatingWindow other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Weekday == other.Weekday
               && StartTime < other.EndTime
               && other.StartTime < EndTime;
    }

    /// <summary>آیا لحظهٔ محلی داده‌شده داخل این پنجره است (شروع شامل، پایان غیرشامل).</summary>
    /// <param name="localTime">زمان محلی منطقهٔ زمانی برنامه.</param>
    public bool ContainsLocal(TimeOnly localTime)
        => localTime >= StartTime && localTime < EndTime;

    internal static int NormalizeWeekday(int weekday)
        => weekday is < MinWeekday or > MaxWeekday
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleWeekdayInvalid))
            : weekday;

    /// <summary>
    /// بازهٔ <c>[start,end)</c> را اعتبارسنجی می‌کند: پایان باید اکیداً بعد از شروع باشد. این یک
    /// قاعده است، نه سلیقه: پنجرهٔ معکوس، پنجرهٔ با طول صفر و پنجرهٔ عبورکننده از نیمه‌شب همه
    /// یک کد پایدار می‌گیرند تا هیچ پنجرهٔ بی‌معنا ذخیره نشود.
    /// </summary>
    internal static TimeOnly NormalizeRange(TimeOnly startTime, TimeOnly endTime)
        => endTime <= startTime
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleWindowRangeInvalid))
            : endTime;

    internal static int? NormalizeCapacity(int? capacity)
        => capacity is not { } value
            ? null
            : value <= 0 || value > CapacityMaxValue
                ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleWindowCapacityInvalid))
                : value;
}

/// <summary>
/// ورودی خالص دامنه برای ساخت یک پنجرهٔ عملیاتی. Domain نباید به شکل بدنهٔ HTTP وابسته شود،
/// پس Infrastructure مدل قرارداد را به این رکورد نگاشت می‌کند.
/// </summary>
/// <param name="Weekday">روز هفتهٔ ISO ۱..۷.</param>
/// <param name="StartTime">شروع؛ شامل.</param>
/// <param name="EndTime">پایان؛ شامل نیست.</param>
/// <param name="Capacity">ظرفیت اختیاری پیکربندی‌شده.</param>
public sealed record ShippingOperatingWindowDefinition(
    int Weekday,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? Capacity = null);
