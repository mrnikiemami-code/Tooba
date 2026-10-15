using Tooba.Order.Application.Storefront.Shipping.RateAuthority;
using Tooba.Shipping.Contracts.Operations;

namespace Tooba.Order.Application.Storefront.Shipping.OperationalShadow;

/// <summary>
/// مقایسه‌کنندهٔ خالص سایهٔ عملیاتی: واقعیت‌های مسیر قدیمی را با نتیجهٔ مرجع عملیاتی Shipping
/// مقایسه می‌کند و هرگز چیزی برنمی‌گرداند که بتواند قیمت، وعدهٔ تحویل یا مجازبودن را تغییر دهد.
/// </summary>
/// <remarks>
/// <para>
/// <b>مبنای مقایسه مبلغ مؤثر است، نه جمع کور.</b> مسیر قدیمی امروز فقط «کرایه» را به مشتری
/// می‌دهد. پس ابتدا کرایهٔ مرجع جدید با مبلغ قدیمی مقایسه می‌شود و سپس — فقط اگر کرایه یکسان
/// بود — هزینهٔ بسته‌بندی جداگانه گزارش می‌شود. جمع‌کردن خودکار بسته‌بندی داخل مبلغ مشتری
/// ممنوع است، چون یک تغییر قیمت واقعی بدون تأیید انسانی می‌سازد.
/// </para>
/// <para>
/// <b>صفر یک مقدار واقعی است.</b> مبالغ تهی «اعلام‌نشده» می‌مانند و هرگز با صفر عوض نمی‌شوند.
/// </para>
/// </remarks>
public static class StorefrontShippingOperationalShadowComparer
{
    /// <summary>یک ارزیابی عملیاتی را با واقعیت‌های مسیر قدیمی مقایسه می‌کند.</summary>
    /// <param name="candidate">نامزد مسیر قدیمی (خدمت/گزینه/مبلغ/مهلت).</param>
    /// <param name="quote">نتیجهٔ قطعی مرجع عملیاتی؛ هرگز null.</param>
    /// <param name="gate">وضعیت دروازه؛ برای ثبت در گزارش.</param>
    public static StorefrontShippingOperationalShadowReport Compare(
        StorefrontShippingOperationalShadowCandidate candidate,
        ShippingOperationalQuote quote,
        StorefrontShippingRateAuthorityGate gate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(quote);

        var outcome = ResolveOutcome(candidate, quote);

        return new StorefrontShippingOperationalShadowReport(
            candidate.ServiceCode,
            candidate.OptionCode,
            candidate.LegacyPriceAmount,
            candidate.LegacyLeadDays,
            gate,
            outcome,
            quote.FreightAmount,
            quote.PackagingAmount,
            quote.TotalServiceExpense,
            quote.AmountPayableAtCheckout,
            quote.AmountCollectOnDelivery,
            quote.FreightPayerMode.ToString(),
            quote.PackagingPayer.ToString(),
            quote.Packaging.ReasonCode,
            quote.Schedule.Availability.ToString(),
            quote.Schedule.ReasonCode,
            quote.Schedule.CapacityEnforcement.ToString(),
            quote.Schedule.ConfiguredCapacityTotal,
            quote.ReasonCode,
            quote.CarrierFreight.ReasonCode);
    }

    /// <summary>گزارش «مقایسه‌ای انجام نشد» را می‌سازد (دروازه بسته، مقصد نامقایسه یا خطای مرجع).</summary>
    /// <param name="candidate">نامزد مسیر قدیمی.</param>
    /// <param name="gate">وضعیت دروازه.</param>
    /// <param name="outcome">نتیجهٔ بدون مقایسه.</param>
    /// <param name="reasonCode">کد دلیل پایدار؛ تهی یعنی دلیلی اعلام نمی‌شود.</param>
    public static StorefrontShippingOperationalShadowReport NotEvaluated(
        StorefrontShippingOperationalShadowCandidate candidate,
        StorefrontShippingRateAuthorityGate gate,
        StorefrontShippingOperationalShadowOutcome outcome,
        string? reasonCode = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return new StorefrontShippingOperationalShadowReport(
            candidate.ServiceCode,
            candidate.OptionCode,
            candidate.LegacyPriceAmount,
            candidate.LegacyLeadDays,
            gate,
            outcome,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            reasonCode);
    }

    private static StorefrontShippingOperationalShadowOutcome ResolveOutcome(
        StorefrontShippingOperationalShadowCandidate candidate,
        ShippingOperationalQuote quote)
    {
        if (!quote.IsQuoted)
        {
            // The freight component could not be priced. If the only missing fact was the parcel size,
            // that is a measurement gap and is reported as such rather than as a coverage/denial fact.
            if (string.Equals(
                    quote.CarrierFreight.ReasonCode,
                    Tooba.Shipping.Contracts.Rates.ShippingReasonCodes.MeasurementRequired,
                    StringComparison.Ordinal))
            {
                return StorefrontShippingOperationalShadowOutcome.MeasurementUnavailable;
            }

            return StorefrontShippingOperationalShadowOutcome.LegacyAllowedShippingDenied;
        }

        if (quote.FreightAmount is not { } freight || freight != candidate.LegacyPriceAmount)
        {
            // فرق مبلغ وقتی مرجع می‌گوید «اندازه لازم است» یک شکاف اندازه است، نه یک اختلاف نرخ:
            // قاعدهٔ شرط‌دار fail-closed قیمت نمی‌دهد و این باید صریح گزارش شود.
            if (string.Equals(
                    quote.CarrierFreight.ReasonCode,
                    Tooba.Shipping.Contracts.Rates.ShippingReasonCodes.MeasurementRequired,
                    StringComparison.Ordinal))
            {
                return StorefrontShippingOperationalShadowOutcome.MeasurementUnavailable;
            }

            return StorefrontShippingOperationalShadowOutcome.AmountMismatch;
        }

        // Freight parity holds. A separately charged packaging component is a REAL commercial
        // difference that must not be silently folded into the customer's checkout amount.
        if (quote.PackagingAmount is { } packaging && packaging > 0m)
        {
            return StorefrontShippingOperationalShadowOutcome.PackagingNotReflected;
        }

        return StorefrontShippingOperationalShadowOutcome.Match;
    }
}
