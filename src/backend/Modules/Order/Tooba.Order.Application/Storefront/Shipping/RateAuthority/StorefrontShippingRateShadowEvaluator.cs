using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.RateAuthority;

/// <summary>
/// ارزیاب سایه: قیمت مسیر قدیمی را دست‌نخورده نگه می‌دارد و در صورت اجازهٔ دروازه، نتیجهٔ مرجع
/// تجاری Shipping را فقط برای مشاهده و مقایسه می‌پرسد.
/// </summary>
/// <remarks>
/// <para>
/// این نوع <b>هیچ‌گاه</b> خطا را به مسیر پرداخت منتشر نمی‌کند: نبود schema/جدول/دادهٔ ارسال،
/// خطای اتصال یا هر استثنای دیگر به نتیجهٔ <see cref="StorefrontShippingRateShadowOutcome.ShippingUnavailable"/>
/// ترجمه می‌شود تا مسیر قدیمی فعال بماند. این تنها «شکست امن» قابل قبول است.
/// </para>
/// <para>
/// این ارزیاب در این Task به ریشهٔ ترکیب (DI) یا سرویس فروشگاهی موجود وصل نمی‌شود؛ برای اتصال زنده
/// به تأیید جداگانه نیاز دارد (نگاه کنید به گزارش SHIPPING-CHECKOUT-BINDING-049).
/// </para>
/// </remarks>
public sealed class StorefrontShippingRateShadowEvaluator
{
    private readonly IShippingRateQuoteLookup? _lookup;

    /// <summary>ارزیاب را با درز فقط-خواندنی Shipping می‌سازد.</summary>
    /// <param name="lookup">درز نرخ Shipping؛ تهی یعنی مرجع جدید در دسترس نیست.</param>
    public StorefrontShippingRateShadowEvaluator(IShippingRateQuoteLookup? lookup)
    {
        _lookup = lookup;
    }

    /// <summary>یک ارزیابی سایه انجام می‌دهد و همیشه یک گزارش برمی‌گرداند.</summary>
    /// <param name="input">واقعیت‌های مسیر قدیمی.</param>
    /// <param name="options">تنظیمات دروازه.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<StorefrontShippingRateShadowReport> EvaluateAsync(
        StorefrontShippingRateShadowInput input,
        StorefrontShippingRateAuthorityOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var gate = StorefrontShippingRateAuthorityPolicy.Evaluate(options);
        if (gate != StorefrontShippingRateAuthorityGate.Allowed)
        {
            return StorefrontShippingRateShadowComparer.NotEvaluated(
                input,
                gate,
                StorefrontShippingRateShadowOutcome.NotEvaluated);
        }

        // مقایسه بدون هویت پایدار مقصد بی‌معنا است؛ حدس‌زدن از نام متنی ممنوع است.
        if (_lookup is null || input.Destination is null)
        {
            return StorefrontShippingRateShadowComparer.NotEvaluated(
                input,
                gate,
                StorefrontShippingRateShadowOutcome.NotComparable);
        }

        ShippingQuote quote;
        try
        {
            quote = await _lookup.QuoteAsync(
                new ShippingQuoteRequest(
                    input.EffectiveServiceCode,
                    input.Destination,
                    input.CartSubtotalExclusive,
                    input.CurrencyCode,
                    input.OptionCode),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // نبود جدول/داده یا هر خطای مرجع جدید هرگز نباید مسیر قدیمی را بشکند.
            return StorefrontShippingRateShadowComparer.NotEvaluated(
                input,
                gate,
                StorefrontShippingRateShadowOutcome.ShippingUnavailable);
        }

        if (quote is null)
        {
            return StorefrontShippingRateShadowComparer.NotEvaluated(
                input,
                gate,
                StorefrontShippingRateShadowOutcome.ShippingUnavailable);
        }

        return StorefrontShippingRateShadowComparer.Compare(input, quote, gate);
    }
}
