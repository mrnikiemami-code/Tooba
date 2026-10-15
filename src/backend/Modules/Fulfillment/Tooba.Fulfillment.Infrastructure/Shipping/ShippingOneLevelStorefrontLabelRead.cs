using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// برچسب نمایشی فروشگاه برای روش‌های یک‌سطحی — فقط‌خواندنی، باریک و fail-closed.
/// </summary>
/// <remarks>
/// <para>
/// منبع نام همان ردیف ترجمهٔ مالک این ماژول است (<c>shipping_method_translations</c>) و خواندن
/// کاتالوگ از طریق همان <see cref="IShippingOneLevelCatalogReader"/> موجود انجام می‌شود؛ خوانندهٔ
/// دومی ساخته نمی‌شود.
/// </para>
/// <para>
/// دروازهٔ خاموش ⇒ هیچ پرس‌وجویی به دیتابیس و نگاشت خالی. جدول مهاجرت‌نشده یک نتیجهٔ کنترل‌شدهٔ
/// خالی است، نه خطای ۵۰۰. کد ناشناخته، کد نامعتبر و نبود ترجمهٔ دقیق زبان درخواستی هم خالی
/// می‌مانند؛ هیچ زبانی حدس زده نمی‌شود و هیچ برچسبی ساخته نمی‌شود.
/// </para>
/// <para>
/// این درز هیچ قیمت، واجد شرایط بودن، فعال‌سازی، سفارش یا اسنپ‌شات را نمی‌خواند و تغییر نمی‌دهد و
/// <c>Tooba:ShippingRateAuthority</c> / <c>Tooba:ShippingOperationalShadow</c> را لمس نمی‌کند.
/// </para>
/// </remarks>
public sealed class ShippingOneLevelStorefrontLabelRead : IShippingOneLevelStorefrontLabelReader
{
    private readonly IOptions<ShippingOneLevelCatalogOptions> _options;
    private readonly IShippingOneLevelCatalogReader _reader;

    /// <summary>درز را به پیکربندی و خوانندهٔ کاتالوگ یک‌سطحی وصل می‌کند.</summary>
    public ShippingOneLevelStorefrontLabelRead(
        IOptions<ShippingOneLevelCatalogOptions> options,
        IShippingOneLevelCatalogReader reader)
    {
        _options = options;
        _reader = reader;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IReadOnlyCollection<string> methodCodes,
        Guid languageId,
        CancellationToken cancellationToken)
    {
        if (methodCodes is null || methodCodes.Count == 0 || languageId == Guid.Empty)
        {
            return Empty;
        }

        // دروازهٔ خاموش ⇒ هیچ پرس‌وجو/برداشتی؛ فروشگاه برچسب قدیمی خود را نگه می‌دارد.
        if (ShippingOneLevelCatalogPolicy.Evaluate(_options.Value) != ShippingOneLevelCatalogGate.Enabled)
        {
            return Empty;
        }

        // فقط کدهای معتبر و یکتا؛ هیچ بخشی از کد به سرویس/گزینه تجزیه نمی‌شود. کد خالی/سفیدسفید
        // هرگز به دیتابیس نمی‌رسد.
        var requested = new HashSet<string>(
            methodCodes
                .Select(ShippingMethod.TryNormalizeCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!),
            StringComparer.Ordinal);
        if (requested.Count == 0)
        {
            return Empty;
        }

        try
        {
            var methods = await _reader.ListAsync(cancellationToken);
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var method in methods)
            {
                if (!requested.Contains(method.MethodCode))
                {
                    continue;
                }

                var name = method.Translations
                    .FirstOrDefault(t => t.LanguageId == languageId && !string.IsNullOrWhiteSpace(t.Name))
                    ?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names[method.MethodCode] = name;
                }
            }

            return names.Count == 0 ? Empty : names;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // کاتالوگ یک‌سطحی هنوز مهاجرت نشده است؛ برچسب قدیمی دست‌نخورده می‌ماند.
            return Empty;
        }
    }

    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
