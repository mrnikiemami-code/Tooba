using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// خواندن هویت روش‌های یک‌سطحیِ بدون ردیف گزینهٔ قدیمی — فقط‌خواندنی و fail-closed.
/// </summary>
/// <remarks>
/// <para>
/// «بدون ردیف گزینهٔ قدیمی» یعنی روشی که <c>shipping_methods</c> آن را دارد اما هیچ ردیف
/// <c>shipping_service_options</c> با همان سرویس/کد گزینه وجود ندارد. این همان تفاوت واقعی میان
/// روش‌های قدیمیِ نگاشت‌شده و روش تازه‌ساختهٔ canonical است؛ هیچ ردیف قدیمی برای عبور از نگهبان
/// ساخته نمی‌شود.
/// </para>
/// <para>
/// سرویس والد باید در کاتالوگ قدیمی وجود داشته و فعال باشد؛ در غیر این صورت هویت اعلام نمی‌شود
/// (ثبت ارائه‌دهنده/حامل یک پیش‌نیاز واقعی است و حدس زده نمی‌شود).
/// </para>
/// <para>
/// دروازهٔ کاتالوگ یک‌سطحی خاموش ⇒ صفر پرس‌وجو. جدول مهاجرت‌نشده ⇒ نتیجهٔ خالی کنترل‌شده.
/// این درز هیچ قیمت، پوشش، فعال‌سازی، سفارش یا اسنپ‌شاتی را نمی‌خواند یا تغییر نمی‌دهد.
/// </para>
/// </remarks>
public sealed class ShippingOneLevelCanonicalMethodRead : IShippingOneLevelCanonicalMethodReader
{
    private readonly IOptions<ShippingOneLevelCatalogOptions> _options;
    private readonly FulfillmentDbContext _db;

    /// <summary>درز را به پیکربندی دروازه و DbContext مالک ماژول وصل می‌کند.</summary>
    public ShippingOneLevelCanonicalMethodRead(
        IOptions<ShippingOneLevelCatalogOptions> options,
        FulfillmentDbContext db)
    {
        _options = options;
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingOneLevelCanonicalMethodSnapshot>> ListActiveUnboundAsync(
        CancellationToken cancellationToken)
    {
        if (ShippingOneLevelCatalogPolicy.Evaluate(_options.Value) != ShippingOneLevelCatalogGate.Enabled)
        {
            return [];
        }

        try
        {
            var methods = await _db.ShippingMethods.AsNoTracking()
                .Where(x => x.IsActive && x.LegacyOptionCode != null)
                .ToListAsync(cancellationToken);
            if (methods.Count == 0)
            {
                return [];
            }

            var serviceCodes = methods
                .Select(x => x.LegacyServiceCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var services = await _db.ShippingServices.AsNoTracking()
                .Where(x => serviceCodes.Contains(x.Code) && x.IsActive)
                .ToListAsync(cancellationToken);
            if (services.Count == 0)
            {
                return [];
            }

            var serviceIds = services.Select(x => x.ShippingServiceId).ToArray();
            var options = await _db.ShippingServiceOptions.AsNoTracking()
                .Where(x => serviceIds.Contains(x.ShippingServiceId))
                .Select(x => new { x.ShippingServiceId, x.Code })
                .ToListAsync(cancellationToken);
            var bound = new HashSet<string>(
                options.Select(o => Key(o.ShippingServiceId, o.Code)),
                StringComparer.OrdinalIgnoreCase);

            var result = new List<ShippingOneLevelCanonicalMethodSnapshot>();
            foreach (var method in methods)
            {
                var service = services.FirstOrDefault(s =>
                    string.Equals(s.Code, method.LegacyServiceCode, StringComparison.OrdinalIgnoreCase));
                if (service is null)
                {
                    continue;
                }

                if (bound.Contains(Key(service.ShippingServiceId, method.LegacyOptionCode!)))
                {
                    // روش واقعاً به ردیف گزینهٔ موجود نگاشت شده است؛ مسیر قدیمی صاحب آن است.
                    continue;
                }

                result.Add(new ShippingOneLevelCanonicalMethodSnapshot(
                    method.MethodCode,
                    method.LegacyServiceCode,
                    method.LegacyOptionCode!,
                    string.IsNullOrWhiteSpace(service.IconKey) ? method.ProviderKind : service.IconKey,
                    method.SortOrder));
            }

            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsCanonicalMethodBindingAsync(
        string serviceCode,
        string? optionCode,
        CancellationToken cancellationToken)
    {
        if (ShippingOneLevelCatalogPolicy.Evaluate(_options.Value) != ShippingOneLevelCatalogGate.Enabled)
        {
            return false;
        }

        var service = serviceCode?.Trim() ?? string.Empty;
        var option = optionCode?.Trim() ?? string.Empty;
        if (service.Length == 0 || option.Length == 0)
        {
            // سرویس بدون گزینه وضعیت سطح سرویس است و این پرسش دربارهٔ گزینهٔ روش است.
            return false;
        }

        try
        {
            return await _db.ShippingMethods.AsNoTracking()
                .AnyAsync(
                    x => x.LegacyServiceCode == service && x.LegacyOptionCode == option,
                    cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return false;
        }
    }

    private static string Key(Guid serviceId, string code) => $"{serviceId:N}|{code}";
}
