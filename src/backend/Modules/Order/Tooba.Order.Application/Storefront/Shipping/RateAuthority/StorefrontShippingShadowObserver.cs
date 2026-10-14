using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.Geography.Contracts.Places;

namespace Tooba.Order.Application.Storefront.Shipping.RateAuthority;

/// <summary>
/// ناظر سایهٔ Order روی مسیر واقعی projection ارسال فروشگاه: واقعیت‌های مسیر قدیمی را می‌گیرد،
/// مقصد قدیمی را فقط با حل قطعی Geography به هویت پایدار یکتا نگاشت می‌کند و همان نرخ را از مرجع
/// تجاری Shipping (فقط-خواندنی) می‌پرسد تا تفاوت‌ها <b>فقط مشاهده</b> شوند.
/// </summary>
/// <remarks>
/// <para>
/// <b>دروازه خاموش پیش‌فرض، مسیر سریع.</b> وقتی <c>ShadowEnabled</c> یا <c>PrerequisitesConfirmed</c>
/// برقرار نباشد، این نوع <em>هیچ</em> پرس‌وجویی نمی‌زند و فقط یک کد پایدار برمی‌گرداند؛ بنابراین نصب
/// این کد روی مسیر زنده هیچ کوئری اضافه و هیچ تغییر رفتاری ندارد.
/// </para>
/// <para>
/// <b>حل مقصد بدون حدس.</b> تنها ورودی مقصدی مسیر فروشگاه، استان است. کد کشور از واقعیت موجود در
/// کاتالوگ قدیمی فروشگاه (ایران) گرفته می‌شود و Geography آن را اعتبارسنجی می‌کند؛ اگر نگاشت
/// <c>Resolved</c> نبود، نتیجه <see cref="StorefrontShippingShadowObservationCode.DestinationNotComparable"/>
/// است و هرگز از نام متنی هویت ساخته نمی‌شود.
/// </para>
/// <para>
/// <b>مرز خطا.</b> این نوع هرگز خطای خودش را به مسیر فروشگاه منتشر نمی‌کند: نبود schema/جدول
/// Geography، خطای اتصال یا هر استثنای دیگر به کد مشاهده ترجمه می‌شود. تنها استثنای بازگردانده‌شده
/// لغو صریح (<see cref="OperationCanceledException"/> با توکن لغوشده) است تا معناشناسی لغو حفظ شود.
/// </para>
/// <para>
/// <b>حریم خصوصی و کاردینالیتی.</b> کد مشاهده و نام‌های محدود نتیجهٔ مقایسه ثبت می‌شوند؛ هیچ آدرس،
/// نام شخص، متن خام مشتری یا شناسهٔ مقصد در لاگ نوشته نمی‌شود.
/// </para>
/// </remarks>
public sealed class StorefrontShippingShadowObserver
{
    /// <summary>
    /// کد کشوری که کاتالوگ قدیمی فروشگاه صریحاً به آن محدود است (ایران). این یک <em>کلید</em> ورودی
    /// برای Geography است، نه ساخت هویت: Geography آن را اعتبارسنجی می‌کند و در نبود نگاشت،
    /// نتیجهٔ <c>Unresolved</c> برمی‌گرداند.
    /// </summary>
    public const string LegacyCountryCode = "IR";

    private readonly StorefrontShippingRateShadowEvaluator? _evaluator;
    private readonly IGeographyLookup? _geography;
    private readonly StorefrontShippingRateAuthorityOptions? _options;
    private readonly ILogger<StorefrontShippingShadowObserver>? _logger;

    /// <summary>ناظر سایه را با درزهای فقط-خواندنی می‌سازد؛ همهٔ درزها اختیاری‌اند تا نصب آن بی‌خطر باشد.</summary>
    /// <param name="evaluator">ارزیاب سایهٔ مرجع نرخ Shipping.</param>
    /// <param name="geography">درز فقط-خواندنی Geography برای حل قطعی مقصد.</param>
    /// <param name="options">تنظیمات دروازه؛ پیش‌فرض خاموش.</param>
    /// <param name="logger">لاگر ساخت‌یافته برای کد پایدار مشاهده.</param>
    public StorefrontShippingShadowObserver(
        StorefrontShippingRateShadowEvaluator? evaluator = null,
        IGeographyLookup? geography = null,
        IOptions<StorefrontShippingRateAuthorityOptions>? options = null,
        ILogger<StorefrontShippingShadowObserver>? logger = null)
    {
        _evaluator = evaluator;
        _geography = geography;
        _options = options?.Value;
        _logger = logger;
    }

    /// <summary>
    /// یک مشاهدهٔ سایه انجام می‌دهد. خروجی فقط برای مشاهده/تلمتری است و هیچ مسیری به قیمت،
    /// پیش‌نویس یا snapshot سفارش ندارد.
    /// </summary>
    /// <param name="input">واقعیت‌های مسیر واقعی فروشگاه.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<StorefrontShippingShadowObservationResult> ObserveAsync(
        StorefrontShippingShadowObservationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var options = _options;
        var gate = StorefrontShippingRateAuthorityPolicy.Evaluate(options);
        if (gate == StorefrontShippingRateAuthorityGate.Disabled)
        {
            // مسیر خاموش: بدون هیچ پرس‌وجو، بدون لاگ و بدون هزینه.
            return new StorefrontShippingShadowObservationResult(
                StorefrontShippingShadowObservationCode.Disabled,
                []);
        }

        if (gate != StorefrontShippingRateAuthorityGate.Allowed)
        {
            return Record(StorefrontShippingShadowObservationCode.PrerequisitesUnconfirmed, [], input);
        }

        if (_evaluator is null || _geography is null)
        {
            return Record(StorefrontShippingShadowObservationCode.ShadowUnavailable, [], input);
        }

        if (string.IsNullOrWhiteSpace(input.ProvinceName))
        {
            // شکاف منبع: مسیر فروشگاه در این حالت هیچ مقصدی انتخاب نکرده است و Geography فقط می‌تواند
            // هویت سطح کشور بدهد؛ مقایسهٔ نرخ بدون مقصد، مشاهدهٔ معتبری نیست و حدس زده نمی‌شود.
            return Record(
                StorefrontShippingShadowObservationCode.DestinationNotComparable,
                [],
                input);
        }

        DestinationIdentifiers destination;
        try
        {
            var resolution = await _geography.ResolveLegacyAsync(
                LegacyCountryCode,
                input.ProvinceName,
                cityName: null,
                serviceAreaName: null,
                cancellationToken);

            if (resolution.Outcome != DestinationResolutionOutcome.Resolved
                || resolution.Identifiers is null)
            {
                // مبهم یا نامنگاشت: مقایسه بی‌معنا است و نباید حدس زده شود.
                return Record(
                    StorefrontShippingShadowObservationCode.DestinationNotComparable,
                    [],
                    input);
            }

            destination = resolution.Identifiers;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // نبود جدول/schema یا خطای Geography هرگز نباید مسیر فروشگاه را بشکند.
            return Record(StorefrontShippingShadowObservationCode.ShadowUnavailable, [], input);
        }

        var reports = new List<StorefrontShippingRateShadowReport>(input.Candidates.Count);
        foreach (var candidate in input.Candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.ServiceCode))
            {
                continue;
            }

            reports.Add(await _evaluator.EvaluateAsync(
                new StorefrontShippingRateShadowInput(
                    candidate.ServiceCode.Trim(),
                    candidate.LegacyPriceAmount,

                    // هر نامزد از projection فیلترشدهٔ همان مقصد آمده است، پس مسیر قدیمی آن را
                    // برای این مقصد مجاز دانسته؛ این یک واقعیت منبع است، نه استنتاج.
                    LegacyDestinationAllowed: true,
                    destination,
                    input.CartSubtotalExclusive,
                    input.CurrencyCode),
                options,
                cancellationToken));
        }

        return Record(StorefrontShippingShadowObservationCode.Observed, reports, input);
    }

    private StorefrontShippingShadowObservationResult Record(
        StorefrontShippingShadowObservationCode code,
        IReadOnlyList<StorefrontShippingRateShadowReport> reports,
        StorefrontShippingShadowObservationInput input)
    {
        if (_logger is not null)
        {
            // فقط کد پایدار، تعداد نامزد و مجموعهٔ محدود نتیجه‌ها؛ بدون دادهٔ خصوصی.
            var outcomes = reports
                .Select(r => r.Outcome.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            _logger.LogInformation(
                "Storefront shipping rate shadow observation {ShadowObservationCode} candidates={CandidateCount} outcomes={ShadowOutcomes}",
                code.ToString(),
                input.Candidates.Count,
                outcomes.Length == 0 ? "none" : string.Join(',', outcomes));
        }

        return new StorefrontShippingShadowObservationResult(code, reports);
    }
}
