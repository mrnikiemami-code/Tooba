using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Infrastructure.Packaging;

/// <summary>
/// پیاده‌سازی فقط-خواندنی و <em>آینده‌نگر</em> نمای ترکیبی هزینهٔ مرسوله: مؤلفهٔ حمل را از درز
/// موجود <see cref="IShippingRateQuoteLookup"/> می‌گیرد و مؤلفهٔ بسته‌بندی را از درز
/// <see cref="IShippingPackagingFeeLookup"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>هیچ قاعده‌ای دوباره پیاده نمی‌شود.</b> این کلاس فقط دو مؤلفهٔ موجود را کنار هم می‌گذارد؛
/// منطق نرخ و منطق بسته‌بندی در Domain خودشان می‌مانند.
/// </para>
/// <para>
/// <b>هرگز جمع خوش‌بینانه.</b> اگر حمل قیمت‌گذاری نشده باشد، یا ارز دو مؤلفه یکسان نباشد، یا
/// سیاست‌های بسته‌بندی مبهم باشند، <see cref="ShippingShipmentCost.CombinedAmount"/> تهی می‌ماند و
/// یک کد دلیل پایدار برگردانده می‌شود. یک مؤلفهٔ غایب هرگز با صفر جمع نمی‌شود.
/// </para>
/// <para>
/// <b>امروز مصرف‌کنندهٔ زنده ندارد.</b> هیچ مسیر Checkout این نمای تازه را صدا نمی‌زند، پس هیچ
/// هزینهٔ اضافه‌ای در Checkout امروز ادعا نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingShipmentCostLookup(
    IShippingRateQuoteLookup rateLookup,
    IShippingPackagingFeeLookup packagingLookup) : IShippingShipmentCostLookup
{
    /// <inheritdoc />
    public async Task<ShippingShipmentCost> ResolveAsync(
        ShippingShipmentCostRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var freight = await rateLookup.QuoteAsync(
            new ShippingQuoteRequest(
                request.ServiceCode,
                request.Destination,
                request.CartSubtotalExclusive,
                request.CurrencyCode,
                request.OptionCode,
                request.Parcel),
            cancellationToken);

        var packaging = await packagingLookup.ResolveAsync(
            new ShippingPackagingFeeRequest(
                request.ServiceCode,
                request.Destination,
                request.OptionCode),
            cancellationToken);

        if (packaging.ReasonCode == ShippingPackagingReasonCodes.Ambiguous)
        {
            return new ShippingShipmentCost(
                freight,
                packaging,
                null,
                null,
                ShippingShipmentCostReasonCodes.PackagingAmbiguous);
        }

        if (!freight.IsQuoted)
        {
            // The freight component could not be priced (coverage/tier/currency). A combined amount would
            // be a hidden claim, so it stays null and the freight result is passed through untouched.
            return new ShippingShipmentCost(
                freight,
                packaging,
                null,
                null,
                ShippingShipmentCostReasonCodes.FreightNotQuoted);
        }

        var freightCurrency = freight.CurrencyCode?.Trim().ToUpperInvariant();
        var packagingCurrency = packaging.IsCharged ? packaging.CurrencyCode?.Trim().ToUpperInvariant() : freightCurrency;

        if (packagingCurrency is not null
            && freightCurrency is not null
            && !string.Equals(freightCurrency, packagingCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return new ShippingShipmentCost(
                freight,
                packaging,
                null,
                null,
                ShippingShipmentCostReasonCodes.CurrencyMismatch);
        }

        var currency = freightCurrency ?? packagingCurrency;
        var combined = freight.ChargeAmount + packaging.FeeAmount;
        return new ShippingShipmentCost(
            freight,
            packaging,
            currency,
            combined,
            ShippingShipmentCostReasonCodes.Quoted);
    }
}
