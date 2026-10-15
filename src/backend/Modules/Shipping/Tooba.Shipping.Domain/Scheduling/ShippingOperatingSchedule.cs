using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Domain.Scheduling;

/// <summary>
/// برنامهٔ عملیاتی هفتگی Shipping برای یک کد خدمت و گزینهٔ اختیاری آن: کدام روزهای هفته و در کدام
/// بازه‌های محلی، حامل می‌تواند مرسوله را تحویل بگیرد.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ownership.</b> Fulfillment مالک هویت خدمت و گزینه است؛ Shipping مالک برنامه/پوشش/نرخ و
/// سیاست عملیاتی است. <see cref="ServiceCode"/> و <see cref="OptionCode"/> فقط <em>کلید سازگاری</em>
/// هستند و هیچ ردیف کاتالوگی اینجا تکرار نمی‌شود. وعدهٔ مشتری و اسنپ‌شات آن دارایی Order است.
/// </para>
/// <para>
/// <b>برنامه ≠ وعده.</b> این رکورد یعنی «در این بازه‌ها تحویل به حامل ممکن است»، نه «مشتری در این
/// بازه تحویل می‌گیرد». هیچ وعده‌ای اینجا ساخته یا تغییر داده نمی‌شود.
/// </para>
/// <para>
/// <b>Fail-open نسبت به گذشته.</b> نبود یا غیرفعال بودن برنامه هرگز Checkout فعلی را نمی‌بندد؛
/// این aggregate فقط داده است و مصرف‌کننده‌های امروزی آن را نمی‌خوانند.
/// </para>
/// <para>
/// <b>ظرفیت.</b> ظرفیت هر پنجره فقط یک پیکربندی است. هیچ شمارش، رزرو یا قفل اتمی در این نسخه
/// وجود ندارد، پس این مقدار ظرفیت را <em>اجرا نمی‌کند</em>.
/// </para>
/// </remarks>
public sealed class ShippingOperatingSchedule
{
    /// <summary>حداکثر تعداد پنجره‌های یک برنامه.</summary>
    public const int MaxWindows = 64;

    private readonly List<ShippingOperatingWindow> _windows = [];

    private ShippingOperatingSchedule()
    {
    }

    /// <summary>شناسهٔ پایدار برنامه.</summary>
    public Guid ScheduleId { get; init; }

    /// <summary>کد خدمت ارسال (کلید سازگاری؛ بدون تکرار ردیف کاتالوگ).</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>
    /// گزینهٔ روش ارسال در صورت وجود؛ تهی یعنی برنامه در سطح خود خدمت است.
    /// </summary>
    /// <remarks>
    /// این یک هویت است، نه بخشی از کد خدمت: «tipax:express» خدمت tipax با گزینهٔ express است.
    /// برنامهٔ گزینه‌دار فقط همان گزینه را توصیف می‌کند و روی گزینهٔ دیگر اثر نمی‌گذارد.
    /// </remarks>
    public string? OptionCode { get; private set; }

    /// <summary>
    /// شناسهٔ منطقهٔ زمانی (ترجیحاً IANA، مثلاً <c>Asia/Tehran</c>) که بازه‌ها به وقت محلی آن
    /// تفسیر می‌شوند. بدون این مقدار، «13:00» بی‌معنا است، پس اجباری و صریح است.
    /// </summary>
    public string TimeZoneId { get; private set; } = string.Empty;

    /// <summary>آیا برنامه فعال است؛ رکورد هرگز فیزیکی حذف نمی‌شود.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// پنجره‌های عملیاتی به‌ترتیبی که ثبت شده‌اند. مرتب‌سازی برای نمایش در لایهٔ نگاشت انجام می‌شود
    /// تا EF بتواند همین نمونهٔ مجموعه را ردیابی کند.
    /// </summary>
    public IReadOnlyList<ShippingOperatingWindow> Windows => _windows;

    /// <summary>آیا این برنامه یک گزینهٔ مشخص را توصیف می‌کند (در مقابل برنامهٔ سطح‌خدمت).</summary>
    public bool IsOptionSpecific => OptionCode is not null;

    /// <summary>برنامهٔ عملیاتی جدید می‌سازد.</summary>
    /// <param name="serviceCode">کد خدمت ارسال.</param>
    /// <param name="timeZoneId">شناسهٔ منطقهٔ زمانی؛ اجباری.</param>
    /// <param name="windows">پنجره‌ها؛ ممکن است خالی باشد (برنامهٔ بدون پنجره یعنی «هیچ بازه‌ای»).</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="optionCode">گزینهٔ روش ارسال اختیاری؛ تهی یعنی برنامهٔ سطح‌خدمت.</param>
    /// <param name="scheduleId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    public static ShippingOperatingSchedule Create(
        string serviceCode,
        string timeZoneId,
        IEnumerable<ShippingOperatingWindowDefinition> windows,
        DateTimeOffset now,
        string? optionCode = null,
        Guid? scheduleId = null)
    {
        var schedule = new ShippingOperatingSchedule
        {
            ScheduleId = scheduleId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            ServiceCode = NormalizeServiceCode(serviceCode),
            OptionCode = NormalizeOptionCode(optionCode),
            TimeZoneId = NormalizeTimeZone(timeZoneId),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        schedule.ReplaceWindows(windows);
        return schedule;
    }

    /// <summary>
    /// مجموعهٔ پنجره‌ها را به‌طور کامل جایگزین می‌کند. جایگزینی کامل (نه افزودن تدریجی) عمدی
    /// است: نویسندگی یک برنامه یک عمل اتمیک است و «ویرایش تک‌پنجره» را به دو مسیر واگرا تبدیل نمی‌کند.
    /// </summary>
    /// <param name="windows">پنجره‌های جدید.</param>
    /// <param name="now">زمان UTC ویرایش؛ تهی یعنی بدون تغییر UpdatedAt (برای ساخت اولیه).</param>
    public void ReplaceWindows(IEnumerable<ShippingOperatingWindowDefinition> windows, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(windows);

        var definitions = windows.ToList();
        if (definitions.Count > MaxWindows)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleWindowsTooMany));
        }

        var created = definitions
            .Select(x => ShippingOperatingWindow.Create(x.Weekday, x.StartTime, x.EndTime, x.Capacity))
            .ToList();

        EnsureNoOverlap(created);

        _windows.Clear();
        _windows.AddRange(created);

        if (now is { } timestamp)
        {
            UpdatedAt = timestamp;
        }
    }

    /// <summary>منطقهٔ زمانی برنامه را تغییر می‌دهد؛ بازه‌ها به وقت محلی همین منطقه تفسیر می‌شوند.</summary>
    /// <param name="timeZoneId">شناسهٔ جدید.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeTimeZone(string timeZoneId, DateTimeOffset now)
    {
        TimeZoneId = NormalizeTimeZone(timeZoneId);
        UpdatedAt = now;
    }

    /// <summary>برنامه را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>
    /// برنامه را غیرفعال می‌کند؛ شناسهٔ پایدار برای ارجاع تاریخی حفظ می‌شود.
    /// </summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>
    /// آیا این برنامه خدمت/گزینهٔ داده‌شده را توصیف می‌کند. مقایسه بدون حساسیت به بزرگی/کوچکی و
    /// با نرمال‌سازی فاصله است، دقیقاً مانند تطبیق نرخ.
    /// </summary>
    /// <param name="serviceCode">کد خدمت مسیر.</param>
    /// <param name="optionCode">گزینهٔ مسیر/بدنه؛ تهی یعنی سطح خدمت.</param>
    public bool BelongsTo(string? serviceCode, string? optionCode)
        => string.Equals(ServiceCode, serviceCode?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
           && string.Equals(
               OptionCode ?? string.Empty,
               NormalizeOptionCode(optionCode) ?? string.Empty,
               StringComparison.OrdinalIgnoreCase);

    /// <summary>مجموع ظرفیت پیکربندی‌شده؛ فقط پیکربندی، نه ظرفیت اجراشده.</summary>
    public int? ConfiguredCapacityTotal
        => _windows.Count == 0
            ? null
            : _windows.All(x => x.Capacity is null)
                ? null
                : _windows.Sum(x => x.Capacity ?? 0);

    private static void EnsureNoOverlap(IReadOnlyList<ShippingOperatingWindow> windows)
    {
        for (var i = 0; i < windows.Count; i++)
        {
            for (var j = i + 1; j < windows.Count; j++)
            {
                if (windows[i].Overlaps(windows[j]))
                {
                    throw new SemanticException(new SemanticError(ShippingErrorCodes.ScheduleWindowOverlap));
                }
            }
        }
    }

    private static string NormalizeServiceCode(string? serviceCode)
    {
        var trimmed = serviceCode?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        return trimmed.Length > ShippingRatePolicyLimits.ServiceCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeInvalid))
            : trimmed;
    }

    private static string? NormalizeOptionCode(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > ShippingRatePolicyLimits.OptionCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.OptionCodeInvalid))
            : trimmed;
    }

    /// <summary>
    /// منطقهٔ زمانی را نرمال و اعتبارسنجی می‌کند. یک شناسهٔ غیرقابل‌حل یعنی بازه‌ها به وقت محلی
    /// نامعلومی تفسیر می‌شوند؛ چنین برنامه‌ای ذخیره نمی‌شود.
    /// </summary>
    private static string NormalizeTimeZone(string? timeZoneId)
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
}

/// <summary>
/// محدودیت‌های مشترک طول کلیدهای خدمت/گزینه/منطقهٔ زمانی. عمداً در Domain تکرار نشده و از
/// <see cref="ShippingRatePolicy"/> خوانده می‌شود تا دو منبع حقیقت برای یک طول ساخته نشود.
/// </summary>
public static class ShippingRatePolicyLimits
{
    /// <summary>حداکثر طول کد خدمت.</summary>
    public const int ServiceCodeMaxLength = ShippingRatePolicy.ServiceCodeMaxLength;

    /// <summary>حداکثر طول کد گزینهٔ روش ارسال.</summary>
    public const int OptionCodeMaxLength = ShippingRatePolicy.OptionCodeMaxLength;

    /// <summary>حداکثر طول شناسهٔ منطقهٔ زمانی.</summary>
    public const int TimeZoneIdMaxLength = 64;
}
