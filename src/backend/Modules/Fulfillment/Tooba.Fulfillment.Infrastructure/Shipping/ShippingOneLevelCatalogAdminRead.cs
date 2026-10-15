using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;

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

    /// <inheritdoc />
    public async Task<ShippingOneLevelCatalogAdminBindingSnapshot> ResolveBindingAsync(
        string methodCode,
        CancellationToken cancellationToken)
    {
        var gate = ShippingOneLevelCatalogPolicy.Evaluate(_options.Value);
        if (gate != ShippingOneLevelCatalogGate.Enabled)
        {
            // دروازهٔ خاموش ⇒ هیچ پرس‌وجوی دیتابیسی و هیچ پیوندی.
            return Unresolved(gate, CatalogReady: false);
        }

        var normalized = ShippingMethod.TryNormalizeCode(methodCode);
        if (normalized is null)
        {
            // کد خام/نامعتبر هرگز به سرویس یا گزینه تبدیل نمی‌شود.
            return Unresolved(gate, CatalogReady: true);
        }

        try
        {
            var method = await _reader.GetByCodeAsync(normalized, cancellationToken);
            if (method is null)
            {
                return Unresolved(gate, CatalogReady: true);
            }

            // پیوند فقط از رکورد واقعی کاتالوگ می‌آید؛ هیچ بخشی از method_code در مرورگر یا
            // اینجا به سرویس/گزینه تجزیه نمی‌شود.
            return new ShippingOneLevelCatalogAdminBindingSnapshot(
                gate,
                CatalogReady: true,
                Resolved: true,
                MethodCode: method.MethodCode,
                ServiceCode: method.LegacyServiceCode,
                OptionCode: method.LegacyOptionCode,
                IsActive: method.IsActive);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return Unresolved(gate, CatalogReady: false);
        }
    }

    private static ShippingOneLevelCatalogAdminBindingSnapshot Unresolved(
        ShippingOneLevelCatalogGate gate,
        bool CatalogReady) =>
        new(gate, CatalogReady, Resolved: false, MethodCode: string.Empty, ServiceCode: string.Empty,
            OptionCode: null, IsActive: false);
}
