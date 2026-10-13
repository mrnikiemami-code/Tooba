#pragma warning disable CS1591
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Storefront;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>
/// رزولوشن انتخاب‌گر روش پرداخت روی همان کاتالوگ درگاه که <c>StorefrontPaymentOrchestrator</c> استفاده می‌کند.
/// این آداپتر هیچ قاعدهٔ جدید، Provider پیش‌فرض سخت‌کد، مدت یا fallback نمی‌سازد؛ فقط همان تصمیم authority
/// موجود را زودتر (پیش از commit) و امن‌بسته آشکار می‌کند تا سیاست مهلت ۰۱۸ با Provider نهایی Payment یکی باشد.
/// تنها منبع Provider: <see cref="IPaymentGatewayCatalogPort.DefaultProvider"/>.
/// </summary>
public sealed class PaymentProviderSelectionAdapter(IPaymentGatewayCatalogPort catalog) : IPaymentProviderSelectionPort
{
    public Task<StorefrontProviderSelection?> ResolveAsync(
        string? requestedProviderCode,
        bool useWallet,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selector = requestedProviderCode?.Trim() ?? string.Empty;

        if (useWallet)
        {
            // کیف پول کد پایدار مستقل خودش را دارد و به کاتالوگ درگاه وصل نیست.
            return Task.FromResult<StorefrontProviderSelection?>(
                new StorefrontProviderSelection(selector, PaymentProviderCodes.Wallet));
        }

        if (PaymentProviderCodes.IsManual(selector))
        {
            // کارت‌به‌کارت/دستی فقط وقتی روشن است؛ در غیر این صورت امن‌بسته بدون Provider جایگزین.
            return Task.FromResult(catalog.ManualCardToCardEnabled
                ? new StorefrontProviderSelection(selector, PaymentProviderCodes.Manual)
                : null);
        }

        if (string.Equals(selector, PaymentProviderCodes.GatewayCatalog, StringComparison.OrdinalIgnoreCase))
        {
            if (!catalog.IsOnlineGatewayOffered())
            {
                return Task.FromResult<StorefrontProviderSelection?>(null);
            }

            var canonical = (catalog.DefaultProvider ?? string.Empty).Trim();
            if (canonical.Length == 0)
            {
                return Task.FromResult<StorefrontProviderSelection?>(null);
            }

            return Task.FromResult<StorefrontProviderSelection?>(
                new StorefrontProviderSelection(selector, canonical));
        }

        // انتخاب‌گر غایب یا ناشناخته: هرگز به Provider دیگری «نگاشت ساکت» نمی‌شود.
        return Task.FromResult<StorefrontProviderSelection?>(null);
    }
}
