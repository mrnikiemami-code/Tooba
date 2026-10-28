using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application.Storefront.Models;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>
/// محاسبهٔ backend-authoritative قیمت ارسال و حداقل تحویل.
/// فرمول: minDeliveryDate = today + max(sellerPreparationDays) + methodLeadDays.
/// </summary>
public static class StorefrontShippingCalculator
{
    public static readonly IReadOnlyList<(string Value, string LabelFa)> DayTimeWindows =
    [
        ("9-12", "۹ صبح تا ۱۲ ظهر"),
        ("12-15", "۱۲ ظهر تا ۳ بعدازظهر"),
        ("15-18", "۳ بعدازظهر تا ۶ عصر"),
        ("18-21", "۶ عصر تا ۹ شب"),
    ];

    public static int MaxSellerPreparationDays(
        IEnumerable<Guid> sellerPartyIds,
        ShippingMethodsOptions options)
    {
        var days = sellerPartyIds
            .Select(id => SellerPreparationDays(id, options))
            .DefaultIfEmpty(Math.Max(0, options.DefaultSellerPreparationDays))
            .ToArray();
        return days.Length == 0 ? Math.Max(0, options.DefaultSellerPreparationDays) : days.Max();
    }

    /// <summary>
    /// TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: روزهای آماده‌سازی <b>یک فروشندهٔ مشخص</b>.
    /// کلید نگاشت همان دو شکل پایدار قبلی است و در نبود override همان پیش‌فرض سرور برمی‌گردد.
    /// این درز هیچ آماده‌سازی‌ای از کلاینت نمی‌پذیرد و فقط پیکربندی سرور را می‌خواند.
    /// </summary>
    public static int SellerPreparationDays(Guid sellerPartyId, ShippingMethodsOptions options)
    {
        if (options.SellerPreparationDaysByPartyId.TryGetValue(sellerPartyId.ToString("D"), out var exact)
            || options.SellerPreparationDaysByPartyId.TryGetValue(sellerPartyId.ToString("N"), out exact))
        {
            return Math.Max(0, exact);
        }

        return Math.Max(0, options.DefaultSellerPreparationDays);
    }

    /// <summary>
    /// حداقل تاریخ تحویلِ یک گروه: امروز + آماده‌سازی <b>فروشندهٔ همان گروه</b> + روزهای حمل همان روش.
    /// هیچ دیرترین-حداقل یا اشتراک بین گروه‌ها اینجا اعمال نمی‌شود.
    /// </summary>
    public static DateOnly ComputeGroupMinimumDeliveryDate(
        DateOnly today,
        Guid sellerPartyId,
        int methodLeadDays,
        ShippingMethodsOptions options)
        => ComputeMinimumDeliveryDate(today, SellerPreparationDays(sellerPartyId, options), methodLeadDays);

    public static ShippingMethodRateOptions ResolveRate(string methodCode, ShippingMethodsOptions options)
    {
        var code = methodCode.Trim().ToLowerInvariant();
        var exact = options.Rates.FirstOrDefault(r =>
            string.Equals(r.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        var serviceCode = code.Contains(':', StringComparison.Ordinal)
            ? code.Split(':', 2)[0]
            : code;
        var service = options.Rates.FirstOrDefault(r =>
            string.Equals(r.Code?.Trim(), serviceCode, StringComparison.OrdinalIgnoreCase));
        return service ?? new ShippingMethodRateOptions
        {
            Code = code,
            BasePrice = 0m,
            LeadDays = Math.Max(0, options.DefaultSellerPreparationDays),
        };
    }

    public static decimal QuotePrice(ShippingMethodRateOptions rate, decimal cartSubtotalExclusive)
    {
        if (rate.FreeAboveSubtotal is decimal threshold && cartSubtotalExclusive >= threshold)
        {
            return 0m;
        }

        return Math.Max(0m, rate.BasePrice);
    }

    public static bool IsDestinationAllowed(ShippingMethodRateOptions rate, string? provinceName)
    {
        if (rate.AllowedProvinces is not { Length: > 0 })
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(provinceName))
        {
            return false;
        }

        return rate.AllowedProvinces.Any(p =>
            string.Equals(p.Trim(), provinceName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static DateOnly ComputeMinimumDeliveryDate(
        DateOnly today,
        int maxSellerPreparationDays,
        int methodLeadDays)
        => today.AddDays(Math.Max(0, maxSellerPreparationDays) + Math.Max(0, methodLeadDays));

    public static IReadOnlyList<DateOnly> BuildDeliveryDates(DateOnly minimum, int horizonDays)
    {
        var days = Math.Clamp(horizonDays, 1, 30);
        return Enumerable.Range(0, days).Select(offset => minimum.AddDays(offset)).ToArray();
    }

    /// <summary>
    /// تقویم <b>دریافت مشتری</b> را از پیکربندی هفتگی مورد اعتماد سرور می‌سازد.
    /// </summary>
    /// <remarks>
    /// <para>
    /// معنی وعده: <paramref name="minimumWorkingDayOffset"/> روز <b>کاری</b> بعد از تاریخ سفارش؛
    /// تاریخ سفارش خودش شمرده نمی‌شود (offset 0 فقط وقتی همان روز کاری باشد قابل ارائه است).
    /// روزهای تعطیل نه در شمارش و نه در فهرست تاریخ‌های قابل ارائه نمی‌آیند و هر تاریخ بازهٔ
    /// <b>همان روز</b> را با خود دارد؛ هیچ بازهٔ ثابت (مثل ۹ تا ۱۲) و هیچ ساعت مرورگر اینجا نیست.
    /// </para>
    /// <para>
    /// این عضو فقط یک محاسبهٔ خالص است: نه آماده‌سازی فروشنده را دوباره اضافه می‌کند (وعده از قبل
    /// شامل آن است)، نه cutoff روز جاری، نه تفکیک بازه به slot، نه تقویم تعطیلات و نه ظرفیت را
    /// حدس می‌زند. حداکثر مدت ثبت‌شده در مدل نویسندگی هم دست‌نخورده می‌ماند.
    /// </para>
    /// <para>
    /// ورودی نامعتبر (هفتهٔ ناقص/تکراری/خارج از محدوده، بازهٔ کاری معکوس یا صفر، offset منفی، افق
    /// غیرمثبت) با کد پایدار <see cref="StorefrontOrderErrors.ShippingDeliveryInvalid"/> رد می‌شود؛
    /// هیچ روزی پیش‌فرض‌سازی یا اصلاح نمی‌شود. هفتهٔ کاملاً تعطیل «عدم دسترس‌پذیری» صریح برمی‌گرداند
    /// و هرگز حلقهٔ بی‌پایان یا هفتهٔ پیش‌فرض نمی‌سازد.
    /// </para>
    /// </remarks>
    public static StorefrontShippingReceivingCalendar BuildCustomerReceivingCalendar(
        DateOnly referenceLocalOrderDate,
        int minimumWorkingDayOffset,
        StorefrontShippingReceivingWeek week,
        int deliveryHorizonDays,
        int? slotDurationMinutes = null)
    {
        var byDay = ResolveReceivingWeek(week);
        var slotLength = NormalizeSlotDuration(slotDurationMinutes);

        if (minimumWorkingDayOffset < 0 || minimumWorkingDayOffset > MaxReceivingWorkingDayOffset)
        {
            throw new StorefrontOrderException(
                StorefrontOrderErrors.ShippingDeliveryInvalid,
                "receiving.minimum_working_day_offset");
        }

        if (deliveryHorizonDays < 0 || deliveryHorizonDays > MaxReceivingHorizonDays)
        {
            throw new StorefrontOrderException(
                StorefrontOrderErrors.ShippingDeliveryInvalid,
                "receiving.horizon_days");
        }

        var earliest = FindEarliestReceivingDate(referenceLocalOrderDate, minimumWorkingDayOffset, byDay);
        if (earliest is not DateOnly earliestDate)
        {
            // هیچ روز کاری در هفته نیست: عدم دسترس‌پذیری صریح، بدون حلقه و بدون هفتهٔ پیش‌فرض.
            return new StorefrontShippingReceivingCalendar(null, Array.Empty<StorefrontShippingReceivingSlot>());
        }

        if (deliveryHorizonDays == 0)
        {
            return new StorefrontShippingReceivingCalendar(
                earliestDate,
                Array.Empty<StorefrontShippingReceivingSlot>());
        }

        var dates = new List<StorefrontShippingReceivingSlot>(deliveryHorizonDays);
        var cursor = earliestDate;
        var distinctDates = 0;
        while (distinctDates < deliveryHorizonDays)
        {
            var day = byDay[cursor.DayOfWeek];
            if (day.IsWorking)
            {
                distinctDates++;
                // TB-241: a configured duration splits the SAME authored window into complete,
                // non-overlapping slots anchored at the real opening; the remainder is never rounded into
                // a shorter tail and a day without one complete slot contributes no slot at all.
                if (slotLength is { } length)
                {
                    var opens = day.OpensAt;
                    while (true)
                    {
                        var closes = opens.Add(TimeSpan.FromMinutes(length));
                        if (closes > day.ClosesAt)
                        {
                            break;
                        }

                        dates.Add(new StorefrontShippingReceivingSlot(cursor, opens, closes));
                        opens = closes;
                    }
                }
                else
                {
                    dates.Add(new StorefrontShippingReceivingSlot(cursor, day.OpensAt, day.ClosesAt));
                }
            }

            cursor = cursor.AddDays(1);
        }

        return new StorefrontShippingReceivingCalendar(earliestDate, dates);
    }

    /// <summary>
    /// TB-241: یکتای صریح قاعدهٔ مدت نوبت. تهی یعنی پنجرهٔ کامل روز؛ مقدار حاضر باید ۱..۱۴۴۰ دقیقه
    /// باشد و مقدار نامعتبر رد می‌شود (هرگز clamp/پیش‌فرض ۶۰ دقیقه).
    /// </summary>
    /// <param name="slotDurationMinutes">مدت نوبت به دقیقه یا تهی.</param>
    private static int? NormalizeSlotDuration(int? slotDurationMinutes)
    {
        if (slotDurationMinutes is not { } minutes)
        {
            return null;
        }

        if (minutes < 1 || minutes > MaxReceivingSlotDurationMinutes)
        {
            throw new StorefrontOrderException(
                StorefrontOrderErrors.ShippingDeliveryInvalid,
                "receiving.slot_duration");
        }

        return minutes;
    }

    /// <summary>بیشینهٔ صریح مدت هر نوبت دریافت: یک شبانه‌روز.</summary>
    public const int MaxReceivingSlotDurationMinutes = 1440;

    /// <summary>سقف صریح offset روز کاری و افق تقویم دریافت؛ ورودی خارج از آن رد می‌شود، نه clamp.</summary>
    public const int MaxReceivingWorkingDayOffset = 365;

    /// <summary>سقف صریح تعداد تاریخ‌های قابل ارائه در تقویم دریافت.</summary>
    public const int MaxReceivingHorizonDays = 90;

    private static IReadOnlyDictionary<DayOfWeek, StorefrontShippingReceivingDay> ResolveReceivingWeek(
        StorefrontShippingReceivingWeek week)
    {
        if (week?.Days is null || week.Days.Count != 7)
        {
            throw new StorefrontOrderException(
                StorefrontOrderErrors.ShippingDeliveryInvalid,
                "receiving.week.incomplete");
        }

        var byDay = new Dictionary<DayOfWeek, StorefrontShippingReceivingDay>(7);
        foreach (var day in week.Days)
        {
            if (day is null)
            {
                throw new StorefrontOrderException(
                    StorefrontOrderErrors.ShippingDeliveryInvalid,
                    "receiving.week.day_missing");
            }

            if (!Enum.IsDefined(day.DayOfWeek))
            {
                throw new StorefrontOrderException(
                    StorefrontOrderErrors.ShippingDeliveryInvalid,
                    "receiving.week.day_out_of_range");
            }

            if (!byDay.TryAdd(day.DayOfWeek, day))
            {
                throw new StorefrontOrderException(
                    StorefrontOrderErrors.ShippingDeliveryInvalid,
                    "receiving.week.day_duplicate");
            }

            // روز تعطیل بازهٔ دریافت ندارد؛ فقط روز کاری بازهٔ مثبت و غیرمعکوس لازم دارد.
            if (day.IsWorking && day.ClosesAt <= day.OpensAt)
            {
                throw new StorefrontOrderException(
                    StorefrontOrderErrors.ShippingDeliveryInvalid,
                    "receiving.week.working_interval_invalid");
            }
        }

        return byDay;
    }

    private static DateOnly? FindEarliestReceivingDate(
        DateOnly referenceLocalOrderDate,
        int minimumWorkingDayOffset,
        IReadOnlyDictionary<DayOfWeek, StorefrontShippingReceivingDay> byDay)
    {
        if (!byDay.Values.Any(x => x.IsWorking))
        {
            return null;
        }

        // offset 0: خود تاریخ سفارش فقط اگر روز کاری باشد؛ وگرنه اولین روز کاری بعد از آن.
        var cursor = referenceLocalOrderDate;
        if (minimumWorkingDayOffset == 0)
        {
            while (!byDay[cursor.DayOfWeek].IsWorking)
            {
                cursor = cursor.AddDays(1);
            }

            return cursor;
        }

        // offset N≥1: N-امین روز کاری <b>بعد از</b> تاریخ سفارش؛ تاریخ سفارش شمرده نمی‌شود.
        var counted = 0;
        cursor = referenceLocalOrderDate.AddDays(1);
        while (true)
        {
            if (byDay[cursor.DayOfWeek].IsWorking)
            {
                counted++;
                if (counted == minimumWorkingDayOffset)
                {
                    return cursor;
                }
            }

            cursor = cursor.AddDays(1);
        }
    }

    public static string FormatDeliveryDateLabelFa(DateOnly date, DateOnly today)
    {
        var delta = date.DayNumber - today.DayNumber;
        return delta switch
        {
            0 => "امروز",
            1 => "فردا",
            2 => "پس‌فردا",
            _ => PersianWeekdayName(date.DayOfWeek),
        };
    }

    public static string FormatDeliveryDateSubLabelFa(DateOnly date)
    {
        var calendar = new System.Globalization.PersianCalendar();
        var dt = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var year = calendar.GetYear(dt);
        var month = calendar.GetMonth(dt);
        var day = calendar.GetDayOfMonth(dt);
        return ToPersianDigits($"{year}/{month}/{day}");
    }

    private static string PersianWeekdayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "شنبه",
        DayOfWeek.Sunday => "یکشنبه",
        DayOfWeek.Monday => "دوشنبه",
        DayOfWeek.Tuesday => "سه‌شنبه",
        DayOfWeek.Wednesday => "چهارشنبه",
        DayOfWeek.Thursday => "پنجشنبه",
        DayOfWeek.Friday => "جمعه",
        _ => "—",
    };

    private static string ToPersianDigits(string input)
    {
        var chars = input.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c).ToArray();
        return new string(chars);
    }

    public static void EnsureDeliveryNotEarlier(DateOnly selected, DateOnly minimum)
    {
        if (selected < minimum)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingDeliveryTooEarly);
        }
    }

    public static IReadOnlyList<(string Value, string LabelFa)> ValidTimeWindows(DateOnly selected, DateOnly minimum)
    {
        if (selected < minimum)
        {
            return Array.Empty<(string, string)>();
        }

        return DayTimeWindows;
    }
}
