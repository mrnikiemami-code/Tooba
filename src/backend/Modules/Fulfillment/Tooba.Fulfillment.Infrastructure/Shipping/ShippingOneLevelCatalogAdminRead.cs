using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// درز خواندن ادمین کاتالوگ یک‌سطحی — فقط‌خواندنی و fail-closed.
/// </summary>
/// <remarks>
/// <para>
/// دروازه از پیکربندی معتبر سرور خوانده می‌شود (<see cref="ShippingOneLevelCatalogOptions"/>) و
/// فقط مقدار صریح <c>true</c> آن را روشن می‌کند. دروازهٔ خاموش ⇒ هیچ پرس‌وجویی به دیتابیس زده
/// نمی‌شود و نتیجه <c>Disabled/not-ready</c> است.
/// </para>
/// <para>
/// جدول مهاجرت‌نشده یک نتیجهٔ کنترل‌شدهٔ <c>NOT_READY</c> است، نه خطای ۵۰۰ و نه ادعای موفقیت با
/// لیست خالی: <c>catalogReady=false</c>. تنها خطای شناخته‌شدهٔ «رابط وجود ندارد» مهار می‌شود؛
/// هر خطای دیگر دست‌نخورده بالا می‌رود تا پنهان نشود.
/// </para>
/// <para>
/// این درز <c>Tooba:ShippingRateAuthority</c> و <c>Tooba:ShippingOperationalShadow</c> را
/// نمی‌خواند، القا نمی‌کند و تغییر نمی‌دهد و هیچ نوشتنی ندارد.
/// </para>
/// </remarks>
public sealed class ShippingOneLevelCatalogAdminRead : IShippingOneLevelCatalogAdminRead
{
    private readonly IOptions<ShippingOneLevelCatalogOptions> _options;
    private readonly IShippingOneLevelCatalogReader _reader;

    /// <summary>درز را به پیکربندی و خوانندهٔ فقط‌خواندنی وصل می‌کند.</summary>
    public ShippingOneLevelCatalogAdminRead(
        IOptions<ShippingOneLevelCatalogOptions> options,
        IShippingOneLevelCatalogReader reader)
    {
        _options = options;
        _reader = reader;
    }

    /// <inheritdoc />
    public async Task<ShippingOneLevelCatalogAdminSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        var gate = ShippingOneLevelCatalogPolicy.Evaluate(_options.Value);
        if (gate != ShippingOneLevelCatalogGate.Enabled)
        {
            return new ShippingOneLevelCatalogAdminSnapshot(gate, CatalogReady: false, Methods: []);
        }

        try
        {
            var methods = await _reader.ListAsync(cancellationToken);
            return new ShippingOneLevelCatalogAdminSnapshot(gate, CatalogReady: true, Methods: methods);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // جدول کاتالوگ یک‌سطحی هنوز مهاجرت نشده است. «آماده‌نبودن» صادقانه گزارش می‌شود.
            return new ShippingOneLevelCatalogAdminSnapshot(gate, CatalogReady: false, Methods: []);
        }
    }
}
