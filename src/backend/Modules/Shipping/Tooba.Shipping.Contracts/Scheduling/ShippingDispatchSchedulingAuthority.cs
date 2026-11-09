namespace Tooba.Shipping.Contracts.Scheduling;

/// <summary>
/// یک روز هفتهٔ یک برنامهٔ عملیاتی در درز Contracts: پرچم صریح کاری/تعطیل و ساعت‌های محلی.
/// </summary>
/// <remarks>
/// هویت روز همان روز هفتهٔ ISO است (<c>1=Monday … 7=Sunday</c>). ساعت‌ها فقط برای روز کاری معنا
/// دارند و بازهٔ <b>نیم‌باز</b> <c>[open, close)</c> است: خودِ لحظهٔ بسته‌شدن «دیر» است.
/// </remarks>
/// <param name="Weekday">روز هفتهٔ ISO (۱ تا ۷).</param>
/// <param name="IsWorking">آیا این روز صریحاً روز کاری است.</param>
/// <param name="OpensAt">شروع بازهٔ محلی یا تهی برای روز تعطیل.</param>
/// <param name="ClosesAt">پایان بازهٔ محلی یا تهی برای روز تعطیل.</param>
public sealed record ShippingSchedulingDayView(
    int Weekday,
    bool IsWorking,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt);

/// <summary>یک هفتهٔ عملیاتی کامل با منطقهٔ زمانی صریح.</summary>
/// <param name="TimeZoneId">شناسهٔ منطقهٔ زمانی.</param>
/// <param name="Days">روزهای هفته (هر تعداد؛ کامل‌بودن/یکتایی اینجا سنجیده می‌شود).</param>
public sealed record ShippingSchedulingWeekView(
    string TimeZoneId,
    IReadOnlyList<ShippingSchedulingDayView> Days);

/// <summary>یک بازهٔ محلی روی یک تاریخ محلی.</summary>
/// <param name="Date">تاریخ محلی.</param>
/// <param name="OpensAt">شروع؛ شامل.</param>
/// <param name="ClosesAt">پایان؛ <b>شامل نیست</b>.</param>
public sealed record ShippingSchedulingWindow(DateOnly Date, TimeOnly OpensAt, TimeOnly ClosesAt);

/// <summary>
/// موعد پذیرش/تحویل حامل: تاریخ و بازهٔ محلی همان روز که حامل بسته را می‌پذیرد.
/// </summary>
/// <param name="Date">تاریخ محلی پذیرش.</param>
/// <param name="OpensAt">شروع بازهٔ پذیرش.</param>
/// <param name="ClosesAt">پایان بازهٔ پذیرش (لحظهٔ آخرین پذیرش ممکن).</param>
public sealed record ShippingHandoffPlan(DateOnly Date, TimeOnly OpensAt, TimeOnly ClosesAt);

/// <summary>یک تاریخ قابل تحویل به مشتری با نوبت‌های همان روز.</summary>
/// <param name="Date">تاریخ محلی تحویل.</param>
/// <param name="Slots">نوبت‌های همان روز (حداقل یکی).</param>
public sealed record ShippingReceivingDateAvailability(
    DateOnly Date,
    IReadOnlyList<ShippingSchedulingWindow> Slots);

/// <summary>تقویم تحویل مشتری از یک مرجع معتبر (اولین تاریخ قابل ارائه + فهرست تاریخ‌ها).</summary>
/// <param name="EarliestEligibleDate">نخستین تاریخ قابل ارائه یا تهی.</param>
/// <param name="Dates">تاریخ‌های قابل ارائه به ترتیب صعودی.</param>
public sealed record ShippingReceivingCalendarPlan(
    DateOnly? EarliestEligibleDate,
    IReadOnlyList<ShippingReceivingDateAvailability> Dates);

/// <summary>دلیل قطعی یک محاسبهٔ زمان‌بندی؛ رشتهٔ پایدار، نه متن کاربر.</summary>
public static class ShippingDispatchSchedulingReasons
{
    /// <summary>هفتهٔ حامل ناقص/تکراری/خارج از محدوده یا بازهٔ معکوس است.</summary>
    public const string CarrierWeekInvalid = "shipping.scheduling.carrier_week_invalid";

    /// <summary>هفتهٔ مشتری ناقص/تکراری/خارج از محدوده یا بازهٔ معکوس است.</summary>
    public const string CustomerWeekInvalid = "shipping.scheduling.customer_week_invalid";

    /// <summary>شناسهٔ منطقهٔ زمانی حامل روی این runtime حل نمی‌شود.</summary>
    public const string CarrierTimeZoneUnresolvable = "shipping.scheduling.carrier_time_zone_unresolvable";

    /// <summary>شناسهٔ منطقهٔ زمانی مشتری روی این runtime حل نمی‌شود.</summary>
    public const string CustomerTimeZoneUnresolvable = "shipping.scheduling.customer_time_zone_unresolvable";

    /// <summary>هیچ روز کاری پذیرش در هفتهٔ حامل نیست.</summary>
    public const string NoCarrierWorkingDay = "shipping.scheduling.no_carrier_working_day";

    /// <summary>هیچ روز کاری تحویل در هفتهٔ مشتری نیست.</summary>
    public const string NoCustomerWorkingDay = "shipping.scheduling.no_customer_working_day";

    /// <summary>مدت نوبت مشتری خارج از بازهٔ صریح ۱..۱۴۴۰ دقیقه است.</summary>
    public const string SlotDurationInvalid = "shipping.scheduling.slot_duration_invalid";

    /// <summary>یک روز کاری مشتری نمی‌تواند حتی یک نوبت کامل را در خود جا دهد.</summary>
    public const string SlotDurationUnfit = "shipping.scheduling.slot_duration_unfit";

    /// <summary>تعداد روزهای حمل منفی یا بزرگ‌تر از کران صریح است.</summary>
    public const string TransportDaysInvalid = "shipping.scheduling.transport_days_invalid";

    /// <summary>افق تقویم منفی یا بزرگ‌تر از کران صریح است.</summary>
    public const string HorizonInvalid = "shipping.scheduling.horizon_invalid";

    /// <summary>در افق جست‌وجو هیچ بازهٔ پذیرش معتبری پیدا نشد.</summary>
    public const string NoFeasibleHandoff = "shipping.scheduling.no_feasible_handoff";

    /// <summary>هیچ تاریخ تحویل معتبری با نوبت باقی‌مانده وجود ندارد.</summary>
    public const string NoReceivingDate = "shipping.scheduling.no_receiving_date";
}

/// <summary>نتیجهٔ قطعی یک محاسبهٔ زمان‌بندی: مقدار یا کد دلیل پایدار، هرگز هر دو/هیچ‌کدام.</summary>
/// <typeparam name="T">نوع مقدار موفق.</typeparam>
/// <param name="Value">مقدار موفق یا تهی.</param>
/// <param name="ReasonCode">کد دلیل شکست یا رشتهٔ خالی در موفقیت.</param>
public sealed record ShippingSchedulingResult<T>(T? Value, string ReasonCode)
    where T : class
{
    /// <summary>آیا محاسبه موفق است.</summary>
    public bool IsSuccess => Value is not null;

    internal static ShippingSchedulingResult<T> Success(T value) => new(value, string.Empty);

    internal static ShippingSchedulingResult<T> Failure(string reasonCode) => new(null, reasonCode);
}

/// <summary>
/// مرجع <b>یکتا</b> زمان‌بندی ارسال در درز Contracts: پذیرش حامل، شمارش روزهای حمل و ساخت تقویم
/// تحویل مشتری. کاملاً خالص است (بدون I/O، بدون ساعت سیستم، بدون منطقهٔ زمانی پیش‌فرض).
/// </summary>
/// <remarks>
/// <para>
/// <b>سه مفهوم جدا.</b> بازهٔ تحویل مشتری از هفتهٔ مشتری می‌آید؛ بازهٔ پذیرش بسته از هفتهٔ حامل؛ و
/// زمان واقعی ارسال/تحویل یک رویداد واقعی است که این کلاس هرگز نمی‌سازد.
/// </para>
/// <para>
/// <b>نیم‌باز.</b> بازه‌ها <c>[open, close)</c> هستند: ورود پیش از گشایش همان روز را هدف می‌گیرد،
/// ورود دقیقاً در لحظهٔ بسته‌شدن یا پس از آن به روز کاری بعد می‌رود و روز تعطیل هرگز پذیرش ندارد.
/// </para>
/// <para>
/// <b>شمارش حمل.</b> روزهای حمل <b>بعد از</b> روز پذیرش شمرده می‌شوند و فقط با
/// <see cref="ShippingSchedulingWeekView"/> حامل تعیین می‌شوند (این نسخه تقویم حمل جداگانه ندارد و
/// همین قاعده صریح و مستند است). صفر روز یک حالت صریح است و فقط با نوبت مشتریِ هنوز شروع‌نشده پس از
/// پذیرش معنا دارد.
/// </para>
/// <para>
/// <b>بدون ادعای ساعت دقیق.</b> خروجی یک تاریخ/بازهٔ تعهد مشتری و یک موعد پذیرش است؛ هیچ تضمین
/// ساعت‌به‌ساعت حمل از این محاسبه استنتاج نمی‌شود.
/// </para>
/// </remarks>
public static class ShippingDispatchSchedulingAuthority
{
    /// <summary>نسخهٔ سیاست محاسبه؛ روی شاهد پذیرش ذخیره می‌شود تا بازتولید ممکن باشد.</summary>
    public const int PolicyVersion = 1;

    /// <summary>کران صریح تعداد روز حمل (هم‌ارز کران پذیرفته‌شدهٔ دامنه).</summary>
    public const int MaxTransportWorkingDays = 365;

    /// <summary>کران صریح افق تقویم تحویل.</summary>
    public const int MaxHorizonDates = 90;

    /// <summary>بیشینهٔ مدت یک نوبت تحویل.</summary>
    public const int MaxSlotDurationMinutes = 1440;

    /// <summary>بیشینهٔ جست‌وجوی روز پذیرش؛ از کران حمل بزرگ‌تر است تا هفتهٔ تعطیل بی‌پایان نشود.</summary>
    public const int MaxHandoffSearchDays = 120;

    /// <summary>
    /// نخستین بازهٔ پذیرش حامل که در/پس از <paramref name="readinessUtc"/> ممکن است.
    /// </summary>
    /// <param name="readinessUtc">لحظهٔ آماده‌بودن واقعی (سفارش/تحویل به حامل) به UTC.</param>
    /// <param name="carrierWeek">برنامهٔ پذیرش حامل.</param>
    /// <param name="searchDays">افق جست‌وجو؛ بیش از کران رد می‌شود.</param>
    public static ShippingSchedulingResult<ShippingHandoffPlan> ResolveFirstHandoff(
        DateTimeOffset readinessUtc,
        ShippingSchedulingWeekView carrierWeek,
        int searchDays = 30)
    {
        if (searchDays < 1 || searchDays > MaxHandoffSearchDays)
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
                ShippingDispatchSchedulingReasons.HorizonInvalid);
        }

        if (!TryBuildWeek(carrierWeek, out var week, out var weekReason))
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(weekReason);
        }

        if (!week.HasWorkingDay)
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
                ShippingDispatchSchedulingReasons.NoCarrierWorkingDay);
        }

        if (!TryResolveCarrierZone(carrierWeek.TimeZoneId, out var timeZone))
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
                ShippingDispatchSchedulingReasons.CarrierTimeZoneUnresolvable);
        }

        var local = TimeZoneInfo.ConvertTime(readinessUtc, timeZone);
        var localDate = DateOnly.FromDateTime(local.DateTime);
        var localTime = TimeOnly.FromDateTime(local.DateTime);

        for (var offset = 0; offset <= searchDays; offset++)
        {
            var date = localDate.AddDays(offset);
            if (!week.TryGet(date.DayOfWeek, out var day) || !day.IsWorking)
            {
                continue;
            }

            // Half-open: before opening targets opening; exactly closing (or later) is too late.
            if (offset == 0)
            {
                if (localTime >= day.ClosesAt)
                {
                    continue;
                }

                return ShippingSchedulingResult<ShippingHandoffPlan>.Success(
                    new ShippingHandoffPlan(date, day.OpensAt, day.ClosesAt));
            }

            return ShippingSchedulingResult<ShippingHandoffPlan>.Success(
                new ShippingHandoffPlan(date, day.OpensAt, day.ClosesAt));
        }

        return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
            ShippingDispatchSchedulingReasons.NoFeasibleHandoff);
    }

    /// <summary>
    /// آخرین روز پذیرش ممکن برای یک تاریخ تحویل انتخاب‌شده، با همان مرجع رو‌به‌جلوی حمل.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>«در یا پیش از» تاریخ تحویل.</b> کاندیدی پذیرفته می‌شود که رسیدن محافظه‌کارانهٔ رو‌به‌جلویش
    /// (<see cref="AdvanceTransportDays"/>) <b>روی یا پیش از</b> <paramref name="receivingDate"/>
    /// باشد. بنابراین روزی که حامل در همان تاریخ تحویل تعطیل است رد نمی‌شود؛ بسته زودتر رسیده و در
    /// روز تحویل مشتری آزاد می‌شود. تفریق تقویمی ساده هرگز انجام نمی‌شود.
    /// </para>
    /// <para>
    /// <b>جست‌وجوی یکنوا.</b> از آخرین روز کاری حامل پیش از تاریخ تحویل شروع می‌کنیم و هر قدم، هم
    /// کاندید و هم تاریخ رسیدن را یک روز کاری به عقب می‌برد (نگاشت دقیق بین دو دنبالهٔ روز کاری).
    /// رسیدن نسبت به روز پذیرش یکنواست، پس نخستین کاندیدی که شرط «در یا پیش از» را برآورده کند
    /// آخرین موعد معتبر است. تعداد قدم‌ها حداکثر همان روزهای حمل است و هیچ اسکن درهم‌تنیدهٔ
    /// O(N²) لازم نیست.
    /// </para>
    /// </remarks>
    /// <param name="receivingDate">تاریخ تحویل انتخاب‌شده.</param>
    /// <param name="carrierWeek">برنامهٔ پذیرش حامل.</param>
    /// <param name="transportWorkingDays">تعداد روز کاری حمل بعد از روز پذیرش (۰ مجاز).</param>
    public static ShippingSchedulingResult<ShippingHandoffPlan> ResolveLatestHandoffForReceivingDate(
        DateOnly receivingDate,
        ShippingSchedulingWeekView carrierWeek,
        int transportWorkingDays)
    {
        if (transportWorkingDays < 0 || transportWorkingDays > MaxTransportWorkingDays)
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
                ShippingDispatchSchedulingReasons.TransportDaysInvalid);
        }

        if (!TryBuildWeek(carrierWeek, out var week, out var weekReason))
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(weekReason);
        }

        if (!week.HasWorkingDay)
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
                ShippingDispatchSchedulingReasons.NoCarrierWorkingDay);
        }

        // Latest carrier working day on or before the chosen receiving date.
        if (!TryFindWorkingDay(week, receivingDate, minDaysBack: 0, out var candidate, out var candidateDay))
        {
            return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
                ShippingDispatchSchedulingReasons.NoFeasibleHandoff);
        }

        var arrival = AdvanceTransportDays(week, candidate, transportWorkingDays);

        // Arrival is monotonic in the handoff day: stepping the handoff back one working day steps the
        // arrival back exactly one working day. At most MAX transport days are needed, and the guard keeps
        // the loop bounded even for a hostile week shape.
        var guard = transportWorkingDays + 7;
        for (var step = 0; step <= guard; step++)
        {
            if (arrival <= receivingDate)
            {
                return ShippingSchedulingResult<ShippingHandoffPlan>.Success(
                    new ShippingHandoffPlan(candidate, candidateDay.OpensAt, candidateDay.ClosesAt));
            }

            if (!TryFindWorkingDay(week, candidate, minDaysBack: 1, out var previousCandidate, out var previousDay)
                || !TryFindWorkingDay(week, arrival, minDaysBack: 1, out var previousArrival, out _))
            {
                return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
                    ShippingDispatchSchedulingReasons.NoFeasibleHandoff);
            }

            candidate = previousCandidate;
            candidateDay = previousDay;
            arrival = previousArrival;
        }

        return ShippingSchedulingResult<ShippingHandoffPlan>.Failure(
            ShippingDispatchSchedulingReasons.NoFeasibleHandoff);
    }

    /// <summary>
    /// نزدیک‌ترین روز کاری حامل در بازهٔ صفر تا هفت روز <b>پیش از</b> تاریخ مرجع؛
    /// <paramref name="minDaysBack"/> صفر یعنی «همان روز یا پیش‌تر» و یک یعنی «فقط پیش‌تر».
    /// </summary>
    private static bool TryFindWorkingDay(
        Week week,
        DateOnly date,
        int minDaysBack,
        out DateOnly found,
        out DayWindow window)
    {
        // A cyclic week always contains a working day within seven consecutive dates.
        for (var offset = minDaysBack; offset <= 7; offset++)
        {
            var candidate = date.AddDays(-offset);
            if (week.TryGet(candidate.DayOfWeek, out window))
            {
                found = candidate;
                return true;
            }
        }

        found = default;
        window = default;
        return false;
    }

    /// <summary>
    /// تقویم تحویل مشتری: اولین پذیرش ممکن + N روز کاری حمل (با هفتهٔ حامل) و سپس تاریخ‌های کاری مشتری
    /// که هر کدام با همان مرجع بازاعتبارسنجی می‌شوند.
    /// </summary>
    /// <param name="nowUtc">لحظهٔ مرجع به UTC (آماده‌بودن واقعی همین لحظه).</param>
    /// <param name="carrierWeek">برنامهٔ پذیرش حامل.</param>
    /// <param name="customerWeek">برنامهٔ تحویل مشتری (بازه/مدت نوبت از همین است).</param>
    /// <param name="transportWorkingDays">روزهای کاری حمل بعد از روز پذیرش.</param>
    /// <param name="horizonDates">تعداد تاریخ‌های قابل ارائه (۰ = فقط اولین تاریخ).</param>
    /// <param name="customerSlotMinutes">مدت نوبت مشتری یا تهی برای یک بازهٔ کامل روز.</param>
    public static ShippingSchedulingResult<ShippingReceivingCalendarPlan> BuildReceivingCalendar(
        DateTimeOffset nowUtc,
        ShippingSchedulingWeekView carrierWeek,
        ShippingSchedulingWeekView customerWeek,
        int transportWorkingDays,
        int horizonDates,
        int? customerSlotMinutes)
    {
        if (transportWorkingDays < 0 || transportWorkingDays > MaxTransportWorkingDays)
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(
                ShippingDispatchSchedulingReasons.TransportDaysInvalid);
        }

        if (horizonDates < 0 || horizonDates > MaxHorizonDates)
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(
                ShippingDispatchSchedulingReasons.HorizonInvalid);
        }

        if (customerSlotMinutes is { } minutes && (minutes < 1 || minutes > MaxSlotDurationMinutes))
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(
                ShippingDispatchSchedulingReasons.SlotDurationInvalid);
        }

        if (!TryBuildWeek(
                customerWeek,
                out var customer,
                out var customerReason,
                ShippingDispatchSchedulingReasons.CustomerWeekInvalid))
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(customerReason);
        }

        if (!customer.HasWorkingDay)
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(
                ShippingDispatchSchedulingReasons.NoCustomerWorkingDay);
        }

        if (!TryResolveCustomerZone(customerWeek.TimeZoneId, out var customerZone))
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(
                ShippingDispatchSchedulingReasons.CustomerTimeZoneUnresolvable);
        }

        var handoff = ResolveFirstHandoff(nowUtc, carrierWeek);
        if (!handoff.IsSuccess)
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(handoff.ReasonCode);
        }

        if (!TryBuildWeek(carrierWeek, out var carrier, out var carrierReason))
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(carrierReason);
        }

        var handoffPlan = handoff.Value!;
        var earliest = AdvanceTransportDays(carrier, handoffPlan.Date, transportWorkingDays);

        if (horizonDates == 0)
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Success(
                new ShippingReceivingCalendarPlan(earliest, Array.Empty<ShippingReceivingDateAvailability>()));
        }

        var dates = new List<ShippingReceivingDateAvailability>(horizonDates);
        var cursor = earliest;
        var scanned = 0;
        var scanBound = MaxHandoffSearchDays + (MaxTransportWorkingDays * 2) + MaxHorizonDates;
        while (dates.Count < horizonDates && scanned <= scanBound)
        {
            scanned++;
            var date = cursor;
            cursor = cursor.AddDays(1);

            if (date < earliest)
            {
                continue;
            }

            if (!customer.TryGet(date.DayOfWeek, out var day) || !day.IsWorking)
            {
                continue;
            }

            var latest = ResolveLatestHandoffForReceivingDate(date, carrierWeek, transportWorkingDays);
            if (!latest.IsSuccess || latest.Value!.Date < handoffPlan.Date)
            {
                // The chosen date is not reachable with a feasible handoff from now (stale/closed calendar).
                continue;
            }

            if (!TryBuildSlots(date, day, customerSlotMinutes, out var slots, out var slotReason))
            {
                return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(slotReason);
            }

            if (transportWorkingDays == 0 && date == handoffPlan.Date)
            {
                // Same-day: never offer a customer slot that already started before readiness/handoff.
                // The carrier window and the customer week may use different time zones, so real instants
                // are compared — carrier clock values are never compared with customer clock values.
                if (!TryResolveCarrierZone(carrierWeek.TimeZoneId, out var carrierZone)
                    || !TryToUtcInstant(handoffPlan.Date, handoffPlan.OpensAt, carrierZone, out var handoffOpensUtc))
                {
                    return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(
                        ShippingDispatchSchedulingReasons.CarrierTimeZoneUnresolvable);
                }

                var floorUtc = handoffOpensUtc > nowUtc ? handoffOpensUtc : nowUtc;
                var offered = new List<ShippingSchedulingWindow>(slots.Count);
                foreach (var slot in slots)
                {
                    // An unresolvable local slot instant (DST gap) is never offered.
                    if (TryToUtcInstant(slot.Date, slot.OpensAt, customerZone, out var slotOpensUtc)
                        && slotOpensUtc >= floorUtc)
                    {
                        offered.Add(slot);
                    }
                }

                slots = offered;
            }

            if (slots.Count == 0)
            {
                continue;
            }

            dates.Add(new ShippingReceivingDateAvailability(date, slots));
        }

        if (dates.Count == 0)
        {
            return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Failure(
                ShippingDispatchSchedulingReasons.NoReceivingDate);
        }

        return ShippingSchedulingResult<ShippingReceivingCalendarPlan>.Success(
            new ShippingReceivingCalendarPlan(dates[0].Date, dates));
    }

    /// <summary>
    /// N-اُمین روز کاری حمل <b>بعد از</b> <paramref name="fromLocalDate"/>؛ خودِ روز پذیرش شمرده نمی‌شود.
    /// صفر همان روز پذیرش است و روز تعطیل هرگز شمرده نمی‌شود.
    /// </summary>
    /// <param name="carrierWeek">هفتهٔ حامل.</param>
    /// <param name="fromLocalDate">روز پذیرش محلی.</param>
    /// <param name="transportWorkingDays">تعداد روز کاری بعد از پذیرش.</param>
    public static DateOnly AdvanceTransportDays(
        ShippingSchedulingWeekView carrierWeek,
        DateOnly fromLocalDate,
        int transportWorkingDays)
    {
        if (!TryBuildWeek(carrierWeek, out var week, out _))
        {
            throw new ArgumentException("carrier_week_invalid", nameof(carrierWeek));
        }

        if (transportWorkingDays < 0 || transportWorkingDays > MaxTransportWorkingDays)
        {
            throw new ArgumentOutOfRangeException(nameof(transportWorkingDays));
        }

        return AdvanceTransportDays(week, fromLocalDate, transportWorkingDays);
    }

    private static DateOnly AdvanceTransportDays(Week week, DateOnly fromLocalDate, int transportWorkingDays)
    {
        if (transportWorkingDays == 0)
        {
            return fromLocalDate;
        }

        if (!week.HasWorkingDay)
        {
            throw new ArgumentException("carrier_week_without_working_day", nameof(week));
        }

        // Derived bound, not an arbitrary widening: a cyclic week has at least one working day in any
        // seven consecutive dates, so N counted days need at most 7*N calendar days (plus a small delta).
        var guard = checked((transportWorkingDays * 7L) + 8L);
        var counted = 0;
        var cursor = fromLocalDate.AddDays(1);
        for (long steps = 0; steps <= guard; steps++)
        {
            if (week.TryGet(cursor.DayOfWeek, out var day) && day.IsWorking)
            {
                counted++;
                if (counted == transportWorkingDays)
                {
                    return cursor;
                }
            }

            cursor = cursor.AddDays(1);
        }

        throw new ArgumentOutOfRangeException(nameof(transportWorkingDays));
    }

    /// <summary>
    /// یک تاریخ/ساعت محلی را به لحظهٔ واقعی UTC تبدیل می‌کند؛ ساعت ناموجود (شکاف DST) قابل اثبات نیست
    /// و <c>false</c> برمی‌گرداند تا مصرف‌کننده محافظه‌کارانه آن نوبت را ارائه نکند.
    /// </summary>
    private static bool TryToUtcInstant(
        DateOnly date,
        TimeOnly time,
        TimeZoneInfo timeZone,
        out DateTimeOffset instant)
    {
        instant = default;
        try
        {
            var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
            var utc = TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
            instant = new DateTimeOffset(utc, TimeSpan.Zero);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// یک بازهٔ کامل روز یا نوبت‌های هم‌اندازهٔ همان بازه با حساب صحیح tick (بدون wrap نیمه‌شب).
    /// </summary>
    private static bool TryBuildSlots(
        DateOnly date,
        DayWindow day,
        int? slotMinutes,
        out IReadOnlyList<ShippingSchedulingWindow> slots,
        out string reason)
    {
        slots = Array.Empty<ShippingSchedulingWindow>();
        reason = string.Empty;

        if (slotMinutes is not { } minutes)
        {
            slots = [new ShippingSchedulingWindow(date, day.OpensAt, day.ClosesAt)];
            return true;
        }

        var lengthTicks = checked((long)minutes * TimeSpan.TicksPerMinute);
        var openTicks = day.OpensAt.Ticks;
        var closeTicks = day.ClosesAt.Ticks;
        var windowTicks = closeTicks > openTicks ? closeTicks - openTicks : 0L;
        var slotCount = lengthTicks <= 0 ? 0 : (int)(windowTicks / lengthTicks);
        if (slotCount <= 0)
        {
            reason = ShippingDispatchSchedulingReasons.SlotDurationUnfit;
            return false;
        }

        var built = new List<ShippingSchedulingWindow>(slotCount);
        for (var index = 0; index < slotCount; index++)
        {
            var startTicks = openTicks + (index * lengthTicks);
            var endTicks = startTicks + lengthTicks;
            built.Add(new ShippingSchedulingWindow(
                date,
                new TimeOnly(startTicks),
                new TimeOnly(endTicks)));
        }

        slots = built;
        return true;
    }

    private static bool TryResolveCarrierZone(string? timeZoneId, out TimeZoneInfo timeZone)
        => TryResolveZone(timeZoneId, out timeZone);

    private static bool TryResolveCustomerZone(string? timeZoneId, out TimeZoneInfo timeZone)
        => TryResolveZone(timeZoneId, out timeZone);

    private static bool TryResolveZone(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;
        var id = timeZoneId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool TryBuildWeek(
        ShippingSchedulingWeekView? source,
        out Week week,
        out string reason,
        string? invalidReason = null)
    {
        week = Week.Empty;
        reason = string.Empty;
        var invalid = invalidReason ?? ShippingDispatchSchedulingReasons.CarrierWeekInvalid;

        var days = source?.Days;
        if (days is null || days.Count != 7)
        {
            reason = invalid;
            return false;
        }

        var byDay = new DayWindow[8];
        var seen = new HashSet<int>();
        var hasWorking = false;
        foreach (var day in days)
        {
            if (day is null
                || day.Weekday is < 1 or > 7
                || !seen.Add(day.Weekday))
            {
                reason = invalid;
                return false;
            }

            if (day.IsWorking)
            {
                if (day.OpensAt is not { } opens
                    || day.ClosesAt is not { } closes
                    || closes <= opens)
                {
                    reason = invalid;
                    return false;
                }

                hasWorking = true;
                byDay[day.Weekday] = new DayWindow(true, opens, closes);
                continue;
            }

            // A closed day has no active interval. Remembered hours are irrelevant work here.
            byDay[day.Weekday] = new DayWindow(false, TimeOnly.MinValue, TimeOnly.MinValue);
        }

        if (seen.Count != 7)
        {
            reason = invalid;
            return false;
        }

        week = new Week(byDay, hasWorking);
        return true;
    }

    private readonly record struct DayWindow(bool IsWorking, TimeOnly OpensAt, TimeOnly ClosesAt);

    private sealed class Week(DayWindow[] byDay, bool hasWorkingDay)
    {
        public static Week Empty { get; } = new([], false);

        public bool HasWorkingDay { get; } = hasWorkingDay;

        public bool TryGet(DayOfWeek dayOfWeek, out DayWindow window)
        {
            var iso = dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
            if (iso >= 1 && iso < byDay.Length && byDay[iso].IsWorking)
            {
                window = byDay[iso];
                return true;
            }

            if (iso >= 1 && iso < byDay.Length)
            {
                window = byDay[iso];
                return false;
            }

            window = default;
            return false;
        }
    }
}
