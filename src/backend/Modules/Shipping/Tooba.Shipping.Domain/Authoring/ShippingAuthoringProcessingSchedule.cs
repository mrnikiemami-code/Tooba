namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// TB-264-R1: یک روز هفتهٔ کاری <b>پردازش انبار</b>: پرچم صریح کاری/تعطیل و بازهٔ محلی همان روز.
/// </summary>
/// <remarks>
/// <para>
/// این ردیف مستقل از تقویم تحویل مشتری و تقویم پذیرش حامل است: هیچ ساعتی بین سه برنامه کپی/همگام
/// نمی‌شود و هویت روز همان روز هفتهٔ ISO است (<c>1=Monday … 7=Sunday</c>).
/// </para>
/// <para>
/// <b>نبود ≠ ۲۴/۷.</b> نبود این برنامه یعنی «زمان پردازش تنظیم نشده»؛ کارکرد پیوستهٔ شبانه‌روزی فقط با
/// انتخاب صریح اپراتور معنا دارد و هرگز پیش‌فرض نیست.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringProcessingScheduleDay
{
    private ShippingAuthoringProcessingScheduleDay()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک این روز (کلید مرکب با روز هفته).</summary>
    public Guid DraftId { get; private set; }

    /// <summary>روز هفتهٔ ISO: ۱ دوشنبه تا ۷ یکشنبه.</summary>
    public int Weekday { get; private set; }

    /// <summary>آیا انبار این روز کار می‌کند.</summary>
    public bool IsWorking { get; private set; }

    /// <summary>شروع بازهٔ کاری؛ شامل.</summary>
    public TimeOnly? OpensAt { get; private set; }

    /// <summary>پایان بازهٔ کاری؛ <b>شامل نیست</b>.</summary>
    public TimeOnly? ClosesAt { get; private set; }

    internal static ShippingAuthoringProcessingScheduleDay Create(
        Guid draftId,
        int weekday,
        bool isWorking,
        TimeOnly? opensAt,
        TimeOnly? closesAt)
        => new()
        {
            DraftId = draftId,
            Weekday = weekday,
            IsWorking = isWorking,
            OpensAt = opensAt,
            ClosesAt = closesAt,
        };

    internal void Apply(bool isWorking, TimeOnly? opensAt, TimeOnly? closesAt)
    {
        IsWorking = isWorking;
        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    internal bool Matches(bool isWorking, TimeOnly? opensAt, TimeOnly? closesAt)
        => IsWorking == isWorking && OpensAt == opensAt && ClosesAt == closesAt;
}

/// <summary>
/// TB-264-R1: هفتهٔ کاری <b>پردازش انبار</b> یک پیش‌نویس روش ارسال: منطقهٔ زمانی صریح و دقیقاً هفت ردیف.
/// </summary>
/// <remarks>
/// <para>
/// <b>مستقل، نه مشتق.</b> ساعت‌های تحویل مشتری و پذیرش حامل هرگز ساعت انبار نمی‌شوند؛ کپی یک‌بارهٔ
/// محلی (اگر ارائه شود) فقط یک مقدار اولیهٔ صریح است و هیچ پیوند پنهانی نمی‌سازد.
/// </para>
/// <para>
/// <b>نبود ≠ باز بودن.</b> نبود این رکورد یعنی «پیکربندی‌نشده»؛ هفت روز تعطیل یک پیکربندی صریح و
/// متفاوت است و آماده‌سازی هرگز تمام نمی‌شود (دلیل پایدار، بدون جست‌وجوی بی‌پایان).
/// </para>
/// </remarks>
public sealed class ShippingAuthoringProcessingSchedule
{
    /// <summary>تعداد روزهای هفته.</summary>
    public const int DaysPerWeek = 7;

    private readonly List<ShippingAuthoringProcessingScheduleDay> _days = [];

    private ShippingAuthoringProcessingSchedule()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک؛ همان کلید یکتای برنامه.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>شناسهٔ صریح و اعتبارسنجی‌شدهٔ منطقهٔ زمانی پردازش.</summary>
    public string TimeZoneId { get; private set; } = string.Empty;

    /// <summary>زمان آخرین جایگزینی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>هفت ردیف روز.</summary>
    public IReadOnlyList<ShippingAuthoringProcessingScheduleDay> Days => _days;

    internal static ShippingAuthoringProcessingSchedule Create(
        Guid draftId,
        string timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.ProcessingScheduleDayAssignment> assignments,
        DateTimeOffset now)
    {
        var schedule = new ShippingAuthoringProcessingSchedule
        {
            DraftId = draftId,
            TimeZoneId = timeZoneId,
            UpdatedAt = now,
        };
        schedule._days.AddRange(assignments.Select(assignment => ShippingAuthoringProcessingScheduleDay.Create(
            draftId,
            assignment.Weekday,
            assignment.IsWorking,
            assignment.StartTime,
            assignment.EndTime)));
        return schedule;
    }

    internal void Apply(
        string timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.ProcessingScheduleDayAssignment> assignments,
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

            _days.Add(ShippingAuthoringProcessingScheduleDay.Create(
                DraftId,
                assignment.Weekday,
                assignment.IsWorking,
                assignment.StartTime,
                assignment.EndTime));
        }

        UpdatedAt = now;
    }
}
