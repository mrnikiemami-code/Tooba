using Tooba.Fulfillment.Application;

namespace Tooba.Host.Storefront;

/// <summary>
/// محاسبهٔ backend-authoritative قیمت ارسال و حداقل تحویل.
/// فرمول: minDeliveryDate = today + max(sellerPreparationDays) + methodLeadDays.
/// </summary>
public static class StorefrontShippingCalculator
{
    /// <summary>پنجره‌های ساعتی روز (برآورد روزمحور؛ نه تقویم پیچیده).</summary>
    public static readonly IReadOnlyList<(string Value, string LabelFa)> DayTimeWindows =
    [
        ("9-12", "۹ صبح تا ۱۲ ظهر"),
        ("12-15", "۱۲ ظهر تا ۳ بعدازظهر"),
        ("15-18", "۳ بعدازظهر تا ۶ عصر"),
        ("18-21", "۶ عصر تا ۹ شب"),
    ];

    /// <summary>روزهای آماده‌سازی کندترین فروشنده در سبد.</summary>
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

    /// <summary>نرخ روش را با تطبیق کد کامل سپس کد سرویس پیدا می‌کند.</summary>
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

    /// <summary>قیمت ارسال؛ رایگان فقط وقتی آستانهٔ پیکربندی برقرار باشد یا BasePrice=0.</summary>
    public static decimal QuotePrice(ShippingMethodRateOptions rate, decimal cartSubtotalExclusive)
    {
        if (rate.FreeAboveSubtotal is decimal threshold && cartSubtotalExclusive >= threshold)
        {
            return 0m;
        }

        return Math.Max(0m, rate.BasePrice);
    }

    /// <summary>آیا مقصد برای نرخ مجاز است.</summary>
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

    /// <summary>
    /// تاریخ حداقل تحویل (فقط تاریخ UTC تقویمی).
    /// min = today + maxSellerPrep + methodLead.
    /// </summary>
    public static DateOnly ComputeMinimumDeliveryDate(
        DateOnly today,
        int maxSellerPreparationDays,
        int methodLeadDays)
        => today.AddDays(Math.Max(0, maxSellerPreparationDays) + Math.Max(0, methodLeadDays));

    /// <summary>گزینه‌های تاریخ از حداقل تا افق.</summary>
    public static IReadOnlyList<DateOnly> BuildDeliveryDates(DateOnly minimum, int horizonDays)
    {
        var days = Math.Clamp(horizonDays, 1, 30);
        return Enumerable.Range(0, days).Select(offset => minimum.AddDays(offset)).ToArray();
    }

    /// <summary>رد تاریخ زودتر از حداقل.</summary>
    public static void EnsureDeliveryNotEarlier(DateOnly selected, DateOnly minimum)
    {
        if (selected < minimum)
        {
            throw new InvalidOperationException("shipping.delivery.too_early");
        }
    }

    /// <summary>پنجره‌های ساعتی معتبر برای روز انتخاب‌شده.</summary>
    public static IReadOnlyList<(string Value, string LabelFa)> ValidTimeWindows(DateOnly selected, DateOnly minimum)
    {
        if (selected < minimum)
        {
            return Array.Empty<(string, string)>();
        }

        // مدل روزمحور: در روز حداقل و بعد همهٔ پنجره‌ها مجازند؛ ساخت ساعت جعلی نمی‌کنیم.
        return DayTimeWindows;
    }
}
