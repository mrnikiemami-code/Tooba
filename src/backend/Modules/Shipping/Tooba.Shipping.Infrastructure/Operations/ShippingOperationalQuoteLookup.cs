using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Domain.Operations;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Operations;

/// <summary>
/// پیاده‌سازی فقط-خواندنی <see cref="IShippingOperationalQuoteLookup"/> روی دادهٔ خودِ همین ماژول.
/// </summary>
/// <remarks>
/// <para>
/// این adapter فقط چهار مجموعه ردیف را با برش باریک (کد خدمت + کشور) می‌خواند و بلافاصله به
/// ارزیاب قطعی <see cref="ShippingOperationalQuoteEvaluator"/> می‌سپارد؛ هیچ قاعدهٔ تجاری‌ای اینجا
/// بازنویسی نمی‌شود، هیچ نوشتنی انجام نمی‌شود، هیچ تراکنش/قفلی باز نمی‌شود و هیچ جدول ماژول
/// دیگری خوانده نمی‌شود.
/// </para>
/// <para>
/// ظرفیت عمداً خوانده و اجرا نمی‌شود: در این برش هیچ رزرو/شمارش اتمی وجود ندارد، پس ارزیاب همیشه
/// «اجرا نمی‌شود» را گزارش می‌کند و عدد ظرفیت فقط به‌عنوان یک پیکربندی برگردانده می‌شود.
/// </para>
/// </remarks>
public sealed class ShippingOperationalQuoteLookup(ShippingDbContext db) : IShippingOperationalQuoteLookup
{
    /// <inheritdoc />
    public async Task<ShippingOperationalQuote> QuoteAsync(
        ShippingOperationalQuoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var countryId = request.Destination.CountryId;

        var policies = await db.RatePolicies.AsNoTracking()
            .Where(x => x.ServiceCode == serviceCode && x.CountryId == countryId)
            .ToListAsync(cancellationToken);

        var grants = await db.CoverageGrants.AsNoTracking()
            .Where(x => x.ServiceCode == serviceCode && x.CountryId == countryId)
            .ToListAsync(cancellationToken);

        var packagingPolicies = await db.PackagingPolicies.AsNoTracking()
            .Where(x => x.ServiceCode == serviceCode && x.CountryId == countryId)
            .ToListAsync(cancellationToken);

        // Windows are needed for the half-open local time check, so the schedule aggregate is loaded with
        // its owned children; still a single module-owned query, no cross-schema read.
        var schedules = await db.OperatingSchedules.AsNoTracking()
            .Include(x => x.Windows)
            .Where(x => x.ServiceCode == serviceCode)
            .ToListAsync(cancellationToken);

        return ShippingOperationalQuoteEvaluator.Evaluate(
            request,
            policies,
            grants,
            packagingPolicies,
            schedules);
    }
}
