namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// یک روز هفتهٔ برنامهٔ عملیاتی مالک: پرچم صریح کاری/تعطیل و بازهٔ محلی همان روز.
/// </summary>
/// <remarks>
/// <para>
/// <b>هویت روز = روز هفتهٔ ISO.</b> <c>1=Monday … 7=Sunday</c>؛ پس شنبه <c>6</c> و جمعه <c>5</c>
/// است. شناسهٔ نمایشی/برچسب محلی هرگز ذخیره نمی‌شود، بنابراین ترتیب نمایش در رابط کاربری بعداً
/// می‌تواند بدون تغییر داده تغییر کند.
/// </para>
/// <para>
/// <b>ساعت به‌یادماندهٔ روز تعطیل.</b> وقتی روز تعطیل است و هر دو ساعت ارائه نشوند، جفتِ ذخیره‌شدهٔ
/// قبلی همین روز حفظ می‌شود تا رابط کاربری بتواند همان ساعت‌ها را دوباره استفاده کند؛ در نخستین
/// پیکربندی می‌توانند تهی بمانند. این «ساعت پیشنهادی» است، نه پنجرهٔ فعال و نه ادعای دسترس‌پذیری.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringScheduleDay
{
    private ShippingAuthoringScheduleDay()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک این روز (کلید مرکب با روز هفته).</summary>
    public Guid DraftId { get; private set; }

    /// <summary>روز هفتهٔ ISO: ۱ دوشنبه تا ۷ یکشنبه.</summary>
    public int Weekday { get; private set; }

    /// <summary>آیا این روز صریحاً روز کاری است (در مقابل صریحاً تعطیل).</summary>
    public bool IsWorking { get; private set; }

    /// <summary>شروع بازهٔ محلی؛ شامل. تهی یعنی هیچ ساعتی برای این روز ثبت نشده است.</summary>
    public TimeOnly? StartTime { get; private set; }

    /// <summary>پایان بازهٔ محلی؛ <b>شامل نیست</b>.</summary>
    public TimeOnly? EndTime { get; private set; }

    internal static ShippingAuthoringScheduleDay Create(
        Guid draftId,
        int weekday,
        bool isWorking,
        TimeOnly? startTime,
        TimeOnly? endTime)
        => new()
        {
            DraftId = draftId,
            Weekday = weekday,
            IsWorking = isWorking,
            StartTime = startTime,
            EndTime = endTime,
        };

    internal void Apply(bool isWorking, TimeOnly? startTime, TimeOnly? endTime)
    {
        IsWorking = isWorking;
        StartTime = startTime;
        EndTime = endTime;
    }

    internal bool Matches(bool isWorking, TimeOnly? startTime, TimeOnly? endTime)
        => IsWorking == isWorking && StartTime == startTime && EndTime == endTime;
}

/// <summary>
/// برنامهٔ عملیاتی هفتگی <b>یک</b> پیش‌نویس روش ارسال: یک منطقهٔ زمانی صریح و هفت ردیف روز.
/// </summary>
/// <remarks>
/// <para>
/// <b>روی روش، نه روی گروه.</b> این برنامه یک‌بار به خود پیش‌نویس/روش وصل است و همهٔ گروه‌های منطقه‌ای
/// همان پیش‌نویس آن را به اشتراک می‌گذارند؛ هیچ انتخاب جغرافیایی، زبان، ارز یا actor دوباره اینجا
/// تکرار نمی‌شود. مالک/روش دیگر آن را به ارث نمی‌برد.
/// </para>
/// <para>
/// <b>نبود ≠ همه‌تعطیل.</b> نبود این رکورد یعنی «پیکربندی‌نشده» و هیچ‌گاه به‌صورت هفت روز کاری،
/// هفت روز تعطیل، تعطیلی رسمی یا دسترس‌پذیری آزاد تفسیر نمی‌شود. هفت روز تعطیل یک پیکربندی صریح و
/// متمایز است.
/// </para>
/// <para>
/// <b>هنوز زمان‌اجرا نیست.</b> این رکورد فقط ساعت‌های محلی هفته را نگه می‌دارد؛ مهلت (cutoff)، تعطیلات،
/// ظرفیت، اسلات تحویل مشتری و ترکیب تقویم runtime خارج از این برش‌اند.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringWeeklySchedule
{
    /// <summary>تعداد روزهای یک هفته و در نتیجه تعداد دقیق ردیف‌های یک برنامهٔ پیکربندی‌شده.</summary>
    public const int DaysPerWeek = 7;

    private readonly List<ShippingAuthoringScheduleDay> _days = [];

    private ShippingAuthoringWeeklySchedule()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک؛ همان کلید یکتای برنامه.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>شناسهٔ صریح و اعتبارسنجی‌شدهٔ منطقهٔ زمانی که بازه‌ها به وقت محلی آن تفسیر می‌شوند.</summary>
    public string TimeZoneId { get; private set; } = string.Empty;

    /// <summary>زمان آخرین جایگزینی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>هفت ردیف روز؛ ترتیب نمایش در لایهٔ خواندن تعیین می‌شود.</summary>
    public IReadOnlyList<ShippingAuthoringScheduleDay> Days => _days;

    internal static ShippingAuthoringWeeklySchedule Create(
        Guid draftId,
        string timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.ScheduleDayAssignment> assignments,
        DateTimeOffset now)
    {
        var schedule = new ShippingAuthoringWeeklySchedule
        {
            DraftId = draftId,
            TimeZoneId = timeZoneId,
            UpdatedAt = now,
        };
        schedule._days.AddRange(assignments.Select(assignment => ShippingAuthoringScheduleDay.Create(
            draftId,
            assignment.Weekday,
            assignment.IsWorking,
            assignment.StartTime,
            assignment.EndTime)));
        return schedule;
    }

    internal void Apply(
        string timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.ScheduleDayAssignment> assignments,
        DateTimeOffset now)
    {
        TimeZoneId = timeZoneId;
        foreach (var assignment in assignments)
        {
            if (assignment.Existing is { } existing)
            {
                existing.Apply(assignment.IsWorking, assignment.StartTime, assignment.EndTime);
                continue;
            }

            _days.Add(ShippingAuthoringScheduleDay.Create(
                DraftId,
                assignment.Weekday,
                assignment.IsWorking,
                assignment.StartTime,
                assignment.EndTime));
        }

        UpdatedAt = now;
    }
}
