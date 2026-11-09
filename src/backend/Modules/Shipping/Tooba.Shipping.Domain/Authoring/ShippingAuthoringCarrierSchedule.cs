namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// یک روز هفتهٔ برنامهٔ <b>پذیرش حامل</b>: پرچم صریح کاری/تعطیل و بازهٔ محلی همان روز.
/// </summary>
/// <remarks>
/// این ردیف مستقل از برنامهٔ تحویل مشتری است: شناسهٔ مالک همان پیش‌نویس/روش است، اما هیچ ساعتی بین
/// دو برنامه کپی/همگام نمی‌شود. هویت روز روز هفتهٔ ISO است (<c>1=Monday … 7=Sunday</c>).
/// </remarks>
public sealed class ShippingAuthoringCarrierScheduleDay
{
    private ShippingAuthoringCarrierScheduleDay()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک این روز (کلید مرکب با روز هفته).</summary>
    public Guid DraftId { get; private set; }

    /// <summary>روز هفتهٔ ISO: ۱ دوشنبه تا ۷ یکشنبه.</summary>
    public int Weekday { get; private set; }

    /// <summary>آیا حامل در این روز فعالانه بسته می‌پذیرد.</summary>
    public bool IsWorking { get; private set; }

    /// <summary>شروع بازهٔ پذیرش؛ شامل.</summary>
    public TimeOnly? OpensAt { get; private set; }

    /// <summary>پایان بازهٔ پذیرش؛ <b>شامل نیست</b>.</summary>
    public TimeOnly? ClosesAt { get; private set; }

    internal static ShippingAuthoringCarrierScheduleDay Create(
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
/// برنامهٔ هفتگی <b>پذیرش حامل</b> یک پیش‌نویس روش ارسال: منطقهٔ زمانی صریح و دقیقاً هفت ردیف روز.
/// </summary>
/// <remarks>
/// <para>
/// <b>مستقل، نه مشتق.</b> این برنامه جدا از ساعت‌های تحویل مشتری ذخیره و ویرایش می‌شود؛ دکمهٔ
/// «کپی از ساعت‌های تحویل مشتری» فقط یک کپی یک‌بارهٔ محلی است و هیچ پیوند پنهانی نمی‌سازد.
/// </para>
/// <para>
/// <b>نبود ≠ باز بودن.</b> نبود این رکورد یعنی «پیکربندی‌نشده» و هیچ ساعت پذیرشی استنتاج نمی‌شود؛
/// هفت روز تعطیل یک پیکربندی صریح و متفاوت است. هیچ مدت نوبتی روی پذیرش حامل تحمیل نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringCarrierSchedule
{
    /// <summary>تعداد روزهای هفته.</summary>
    public const int DaysPerWeek = 7;

    private readonly List<ShippingAuthoringCarrierScheduleDay> _days = [];

    private ShippingAuthoringCarrierSchedule()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک؛ همان کلید یکتای برنامه.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>شناسهٔ صریح و اعتبارسنجی‌شدهٔ منطقهٔ زمانی پذیرش.</summary>
    public string TimeZoneId { get; private set; } = string.Empty;

    /// <summary>زمان آخرین جایگزینی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>هفت ردیف روز.</summary>
    public IReadOnlyList<ShippingAuthoringCarrierScheduleDay> Days => _days;

    internal static ShippingAuthoringCarrierSchedule Create(
        Guid draftId,
        string timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.CarrierScheduleDayAssignment> assignments,
        DateTimeOffset now)
    {
        var schedule = new ShippingAuthoringCarrierSchedule
        {
            DraftId = draftId,
            TimeZoneId = timeZoneId,
            UpdatedAt = now,
        };
        schedule._days.AddRange(assignments.Select(assignment => ShippingAuthoringCarrierScheduleDay.Create(
            draftId,
            assignment.Weekday,
            assignment.IsWorking,
            assignment.StartTime,
            assignment.EndTime)));
        return schedule;
    }

    internal void Apply(
        string timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.CarrierScheduleDayAssignment> assignments,
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

            _days.Add(ShippingAuthoringCarrierScheduleDay.Create(
                DraftId,
                assignment.Weekday,
                assignment.IsWorking,
                assignment.StartTime,
                assignment.EndTime));
        }

        UpdatedAt = now;
    }
}
