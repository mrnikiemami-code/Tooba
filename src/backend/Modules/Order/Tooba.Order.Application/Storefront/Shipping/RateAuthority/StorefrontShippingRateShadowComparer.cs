using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.RateAuthority;

/// <summary>
/// مقایسه‌کنندهٔ خالص سایه: واقعیت‌های مسیر قدیمی را با نتیجهٔ مرجع جدید مقایسه می‌کند و هرگز
/// چیزی برنمی‌گرداند که بتواند قیمت یا مجازبودن مسیر قدیمی را تغییر دهد.
/// </summary>
/// <remarks>
/// قاعدهٔ «تفاوت پوشش» عمداً یک نتیجهٔ جداگانه است، نه یک خطا و نه یک اصلاح خودکار: امروز
/// مجوزهای پوشش خالی‌اند و موتور جدید fail-closed است، پس انتظار داریم مسیر قدیمی مجاز بداند و
/// مرجع جدید پوشش‌نشده بگوید. این تفاوت باید <em>گزارش</em> شود تا کسی آگاهانه مجوز seed کند؛
/// تبدیل خودکار آن به «مجاز» دقیقاً همان کاری است که این درز ممنوع می‌کند.
/// </remarks>
public static class StorefrontShippingRateShadowComparer
{
    /// <summary>دو مشاهده را مقایسه می‌کند.</summary>
    /// <param name="input">واقعیت‌های مسیر قدیمی.</param>
    /// <param name="shipping">نتیجهٔ مرجع جدید.</param>
    /// <param name="gate">وضعیت دروازه؛ برای ثبت در گزارش.</param>
    public static StorefrontShippingRateShadowReport Compare(
        StorefrontShippingRateShadowInput input,
        ShippingQuote shipping,
        StorefrontShippingRateAuthorityGate gate)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(shipping);

        var shippingAllowed = shipping.Outcome == ShippingServiceabilityOutcome.Quoted;
        var outcome = (input.LegacyDestinationAllowed, shippingAllowed) switch
        {
            (true, false) => StorefrontShippingRateShadowOutcome.LegacyAllowedShippingDenied,
            (false, true) => StorefrontShippingRateShadowOutcome.ShippingAllowedLegacyDenied,
            (false, false) => StorefrontShippingRateShadowOutcome.Match,
            _ => input.LegacyPriceAmount == shipping.ChargeAmount
                ? StorefrontShippingRateShadowOutcome.Match
                : StorefrontShippingRateShadowOutcome.AmountMismatch,
        };

        return new StorefrontShippingRateShadowReport(
            input.MethodCode,
            gate,
            outcome,
            input.LegacyPriceAmount,
            shipping.ChargeAmount,
            shipping.Outcome.ToString(),
            shipping.ReasonCode,
            input.OptionCode,
            input.EffectiveServiceCode);
    }

    /// <summary>گزارش‌های بدون مقایسه را می‌سازد (دروازه بسته یا عدم امکان مقایسه یا خطای مرجع).</summary>
    /// <param name="input">واقعیت‌های مسیر قدیمی.</param>
    /// <param name="gate">وضعیت دروازه.</param>
    /// <param name="outcome">نتیجهٔ بدون مقایسه.</param>
    public static StorefrontShippingRateShadowReport NotEvaluated(
        StorefrontShippingRateShadowInput input,
        StorefrontShippingRateAuthorityGate gate,
        StorefrontShippingRateShadowOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new StorefrontShippingRateShadowReport(
            input.MethodCode,
            gate,
            outcome,
            input.LegacyPriceAmount,
            null,
            null,
            null,
            input.OptionCode,
            input.EffectiveServiceCode);
    }
}
