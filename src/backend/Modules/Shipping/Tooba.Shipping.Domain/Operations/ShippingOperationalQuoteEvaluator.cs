using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Packaging;
using Tooba.Shipping.Domain.Rates;
using Tooba.Shipping.Domain.Scheduling;

namespace Tooba.Shipping.Domain.Operations;

/// <summary>
/// ارزیاب قطعی و فقط-محاسباتی «قیمت عملیاتی» Shipping: موتور نرخ موجود، resolver بسته‌بندی موجود
/// و ارزیاب بازهٔ حامل را کنار هم می‌گذارد و تفکیک پرداخت را صریح می‌کند.
/// </summary>
/// <remarks>
/// <para>
/// <b>هیچ قاعده‌ای دوباره پیاده نمی‌شود.</b> این لایه فقط ترکیب می‌کند: تطبیق خدمت/گزینه/دامنه/پله
/// در <see cref="ShippingRatePolicyEngine"/> و <see cref="ShippingPackagingFeeResolver"/>
/// می‌ماند. یک موتور نرخ دوم ساخته نمی‌شود.
/// </para>
/// <para>
/// <b>هیچ‌گاه صفرِ موفق‌نما.</b> اگر حمل قیمت‌گذاری نشود، بسته‌بندی مبهم باشد، یا ارز دو مؤلفه
/// ناسازگار باشد، همهٔ مبالغ <c>null</c> می‌مانند. یک مؤلفهٔ اعلام‌نشده هرگز با صفر جمع نمی‌شود.
/// </para>
/// <para>
/// <b>هیچ خواندن ضمنی از زمان.</b> لحظهٔ درخواستی صریحاً داده می‌شود و هیچ ساعت سیستمی خوانده
/// نمی‌شود، تا نتیجهٔ ارزیاب قابل بازتولید و آزمون‌پذیر باشد.
/// </para>
/// <para>
/// <b>تفکیک پرداخت یک واقعیت نرخ است.</b> این ارزیاب هیچ تراکنش پرداختی، هیچ رزرو و هیچ نوشتنی
/// نمی‌سازد. Order/Payment مالک انحصاری پول باقی می‌مانند و امروز هیچ مصرف‌کنندهٔ زنده‌ای این
/// مبالغ را روی سفارش نمی‌نویسد.
/// </para>
/// </remarks>
public static class ShippingOperationalQuoteEvaluator
{
    /// <summary>یک ارزیابی عملیاتی قطعی می‌سازد.</summary>
    /// <param name="request">درخواست ارزیابی.</param>
    /// <param name="policies">سیاست‌های نرخ همان خدمت/کشور؛ ممکن است خالی باشد.</param>
    /// <param name="coverageGrants">مجوزهای پوشش همان خدمت/کشور؛ ممکن است خالی باشد.</param>
    /// <param name="packagingPolicies">سیاست‌های بسته‌بندی همان خدمت/کشور؛ ممکن است خالی باشد.</param>
    /// <param name="schedules">برنامه‌های عملیاتی همان خدمت؛ ممکن است خالی باشد.</param>
    /// <returns>نتیجهٔ ارزیابی؛ هرگز null.</returns>
    public static ShippingOperationalQuote Evaluate(
        ShippingOperationalQuoteRequest request,
        IReadOnlyCollection<ShippingRatePolicy>? policies,
        IReadOnlyCollection<ShippingCoverageGrant>? coverageGrants,
        IReadOnlyCollection<ShippingPackagingPolicy>? packagingPolicies,
        IReadOnlyCollection<ShippingOperatingSchedule>? schedules)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var optionCode = request.OptionCode?.Trim();

        // The two existing engines are called with the SAME inputs the legacy seams receive: this
        // composite adds ordering and payment split, not a second pricing rule.
        var freight = ShippingRatePolicyEngine.Quote(
            new ShippingQuoteRequest(
                serviceCode,
                request.Destination,
                request.CartSubtotalExclusive,
                request.CurrencyCode,
                optionCode,
                request.Parcel),
            policies,
            coverageGrants);

        var packaging = ShippingPackagingFeeResolver.Resolve(
            new ShippingPackagingFeeRequest(serviceCode, request.Destination, optionCode),
            packagingPolicies);

        var schedule = ShippingScheduleAvailabilityEvaluator.Evaluate(
            schedules,
            serviceCode,
            optionCode,
            request.RequestedPickupAtUtc);

        if (packaging.ReasonCode == ShippingPackagingReasonCodes.Ambiguous)
        {
            return NotQuoted(
                serviceCode,
                optionCode,
                freight,
                packaging,
                schedule,
                ShippingOperationalQuoteOutcome.PackagingAmbiguous,
                ShippingOperationalQuoteReasonCodes.PackagingAmbiguous);
        }

        if (!freight.IsQuoted)
        {
            // The freight component could not be priced. A total would be a hidden claim, so every amount
            // stays null and the untouched freight result carries the exact reason code.
            return NotQuoted(
                serviceCode,
                optionCode,
                freight,
                packaging,
                schedule,
                ShippingOperationalQuoteOutcome.FreightNotQuoted,
                ShippingOperationalQuoteReasonCodes.FreightNotQuoted);
        }

        var freightCurrency = NormalizeCurrency(freight.CurrencyCode);
        var packagingCurrency = packaging.IsCharged ? NormalizeCurrency(packaging.CurrencyCode) : freightCurrency;

        if (freightCurrency is not null
            && packagingCurrency is not null
            && !string.Equals(freightCurrency, packagingCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return NotQuoted(
                serviceCode,
                optionCode,
                freight,
                packaging,
                schedule,
                ShippingOperationalQuoteOutcome.CurrencyMismatch,
                ShippingOperationalQuoteReasonCodes.CurrencyMismatch);
        }

        var currency = freightCurrency ?? packagingCurrency;
        var freightAmount = freight.ChargeAmount;
        var packagingAmount = packaging.FeeAmount;
        var total = freightAmount + packagingAmount;

        // Payment split: packaging is always a checkout charge (the carrier has no packaging contract),
        // so only the freight component moves to collect-on-delivery.
        var collectOnDelivery = packaging.FreightPayerMode == ShippingFreightPayerMode.CollectOnDelivery
            ? freightAmount
            : 0m;
        var payableAtCheckout = total - collectOnDelivery;

        return new ShippingOperationalQuote(
            ShippingOperationalQuoteOutcome.Quoted,
            serviceCode,
            optionCode,
            currency,
            freightAmount,
            packagingAmount,
            total,
            payableAtCheckout,
            collectOnDelivery,
            packaging.FreightPayerMode,
            packaging.PackagingPayer,
            freight,
            packaging,
            schedule,
            ShippingOperationalQuoteReasonCodes.Quoted,
            freight.MatchedCondition);
    }

    private static ShippingOperationalQuote NotQuoted(
        string serviceCode,
        string? optionCode,
        ShippingQuote freight,
        ShippingPackagingFeeComponent packaging,
        ShippingScheduleEvaluation schedule,
        ShippingOperationalQuoteOutcome outcome,
        string reasonCode)
        => new(
            outcome,
            serviceCode,
            optionCode,
            null,
            null,
            null,
            null,
            null,
            null,
            packaging.FreightPayerMode,
            packaging.PackagingPayer,
            freight,
            packaging,
            schedule,
            reasonCode);

    private static string? NormalizeCurrency(string? currencyCode)
    {
        var trimmed = currencyCode?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
