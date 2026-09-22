using Tooba.Fulfillment.Contracts.Shipping;

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
            .Select(id =>
            {
                if (options.SellerPreparationDaysByPartyId.TryGetValue(id.ToString("D"), out var overrideDays)
                    || options.SellerPreparationDaysByPartyId.TryGetValue(id.ToString("N"), out overrideDays))
                {
                    return Math.Max(0, overrideDays);
                }

                return Math.Max(0, options.DefaultSellerPreparationDays);
            })
            .DefaultIfEmpty(Math.Max(0, options.DefaultSellerPreparationDays))
            .ToArray();
        return days.Length == 0 ? Math.Max(0, options.DefaultSellerPreparationDays) : days.Max();
    }

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
