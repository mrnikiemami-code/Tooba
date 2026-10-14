using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Rates;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Rates;

/// <summary>
/// پیاده‌سازی فقط-خواندنی <see cref="IShippingRateQuoteLookup"/> روی سیاست‌های همین ماژول.
/// </summary>
/// <remarks>
/// <para>
/// این adapter فقط دو مجموعه ردیف را می‌خواند و بلافاصله به موتور قطعی
/// <see cref="ShippingRatePolicyEngine"/> می‌سپارد؛ هیچ قاعدهٔ تجاری‌ای اینجا بازنویسی نمی‌شود،
/// هیچ نوشتنی انجام نمی‌شود و هیچ تراکنش/قفلی باز نمی‌شود.
/// </para>
/// <para>
/// فیلتر SQL عمداً فقط بر پایهٔ کد خدمت و کشور است (کمترین بریدگی)، و تطبیق دقیق دامنه در موتور
/// انجام می‌شود تا منطق «عمیق‌ترین دامنه برنده است» یک‌جا و تست‌پذیر بماند. نبود هر ردیف به
/// نتیجهٔ fail-closed موتور منتهی می‌شود، نه به نرخ پیش‌فرض.
/// </para>
/// </remarks>
public sealed class ShippingRateQuoteLookup : IShippingRateQuoteLookup
{
    private readonly ShippingDbContext _db;

    /// <summary>نمونه‌ساز با DbContext همین ماژول.</summary>
    /// <param name="db">DbContext ارسال.</param>
    public ShippingRateQuoteLookup(ShippingDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<ShippingQuote> QuoteAsync(
        ShippingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var countryId = request.Destination.CountryId;

        var policies = await _db.RatePolicies.AsNoTracking()
            .Where(x => x.ServiceCode == serviceCode && x.CountryId == countryId)
            .ToListAsync(cancellationToken);
        var grants = await _db.CoverageGrants.AsNoTracking()
            .Where(x => x.ServiceCode == serviceCode && x.CountryId == countryId)
            .ToListAsync(cancellationToken);

        return ShippingRatePolicyEngine.Quote(request, policies, grants);
    }
}
