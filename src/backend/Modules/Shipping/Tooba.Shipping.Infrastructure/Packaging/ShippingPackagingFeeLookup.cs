using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Domain.Packaging;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Packaging;

/// <summary>
/// پیاده‌سازی فقط-خواندنی <see cref="IShippingPackagingFeeLookup"/> روی سیاست‌های بسته‌بندی همین
/// ماژول.
/// </summary>
/// <remarks>
/// این adapter فقط یک مجموعه ردیف را می‌خواند و بلافاصله به resolver قطعی
/// <see cref="ShippingPackagingFeeResolver"/> می‌سپارد؛ هیچ قاعدهٔ تجاری‌ای اینجا بازنویسی نمی‌شود،
/// هیچ نوشتنی انجام نمی‌شود و هیچ تراکنش/قفلی باز نمی‌شود.
/// </remarks>
public sealed class ShippingPackagingFeeLookup : IShippingPackagingFeeLookup
{
    private readonly ShippingDbContext _db;

    /// <summary>نمونه‌ساز با DbContext همین ماژول.</summary>
    /// <param name="db">DbContext ارسال.</param>
    public ShippingPackagingFeeLookup(ShippingDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<ShippingPackagingFeeComponent> ResolveAsync(
        ShippingPackagingFeeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var countryId = request.Destination.CountryId;

        // The SQL filter is intentionally the narrowest slice (service + country) and the exact scope
        // match stays in the resolver, so "deepest destination wins" lives in one testable place.
        var policies = await _db.PackagingPolicies.AsNoTracking()
            .Where(x => x.ServiceCode == serviceCode && x.CountryId == countryId)
            .ToListAsync(cancellationToken);

        return ShippingPackagingFeeResolver.Resolve(request, policies);
    }
}
