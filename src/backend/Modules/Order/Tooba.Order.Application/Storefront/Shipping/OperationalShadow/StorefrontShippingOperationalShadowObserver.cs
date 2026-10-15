using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Shipping.RateAuthority;
using Tooba.Shipping.Contracts.Operations;

namespace Tooba.Order.Application.Storefront.Shipping.OperationalShadow;

/// <summary>
/// ناظر سایهٔ <b>عملیاتی</b> روی مسیر واقعی projection ارسال فروشگاه: واقعیت‌های مسیر قدیمی را
/// می‌گیرد، مقصد قدیمی را فقط با حل قطعی Geography به هویت پایدار یکتا نگاشت می‌کند و همان ارزیابی
/// عملیاتی کامل را از مرجع Shipping (فقط-خواندنی) می‌پرسد تا تفاوت‌ها <b>فقط مشاهده</b> شوند.
/// </summary>
/// <remarks>
/// <para>
/// <b>دروازه خاموش پیش‌فرض، مسیر سریع.</b> وقتی <c>Enabled</c> یا <c>PrerequisitesConfirmed</c>
/// برقرار نباشد، این نوع <em>هیچ</em> پرس‌وجویی نمی‌زند و فقط یک کد پایدار برمی‌گرداند.
/// </para>
/// <para>
/// <b>اندازهٔ واقعی الزامی است، بدون ساخت وزن.</b> اگر اندازهٔ فیزیکی واقعی مرسوله همراه یک منبع
/// معتبر در ورودی نباشد، نتیجه <see cref="StorefrontShippingOperationalShadowCode.MeasurementUnavailable"/>
/// است و حتی یک پرس‌وجو هم زده نمی‌شود. جمع سبد، تعداد خطوط سبد و تعداد کالا هرگز به وزن تبدیل
/// نمی‌شوند؛ یک مقایسهٔ پله‌ای ساختگی «تطابق» تولید نمی‌کند.
/// </para>
/// <para>
/// <b>حل مقصد بدون حدس.</b> تنها ورودی مقصدی مسیر فروشگاه، استان است. کد کشور از واقعیت موجود در
/// کاتالوگ قدیمی فروشگاه گرفته می‌شود و Geography آن را اعتبارسنجی می‌کند؛ در نبود نگاشت قطعی،
/// نتیجه <c>DestinationNotComparable</c> است و از نام متنی هویت ساخته نمی‌شود.
/// </para>
/// <para>
/// <b>مرز خطا.</b> این نوع هرگز خطای خودش را به مسیر فروشگاه منتشر نمی‌کند: نبود schema/جدول،
/// خطای اتصال یا هر استثنای دیگر به کد مشاهده ترجمه می‌شود. تنها استثنای بازگردانده‌شده لغو صریح
/// (<see cref="OperationCanceledException"/> با توکن لغوشده) است.
/// </para>
/// <para>
/// <b>حریم خصوصی.</b> فقط کد پایدار، تعداد نامزد و مجموعهٔ محدود نتیجه‌ها و مبالغ غیرشخصی ثبت
/// می‌شوند؛ هیچ آدرس، نام شخص، متن خام مشتری یا شناسهٔ مقصد در لاگ نوشته نمی‌شود.
/// </para>
/// </remarks>
public sealed class StorefrontShippingOperationalShadowObserver
{
    /// <summary>
    /// کد کشوری که کاتالوگ قدیمی فروشگاه صریحاً به آن محدود است (ایران). یک <em>کلید</em> ورودی
    /// برای Geography است، نه ساخت هویت.
    /// </summary>
    public const string LegacyCountryCode = "IR";

    /// <summary>کد دلیل پایدار برای «اندازهٔ واقعی مرسوله در دسترس نیست».</summary>
    public const string MeasurementUnavailableReasonCode = "shipping.operational_shadow.measurement_unavailable";

    private readonly IShippingOperationalQuoteLookup? _lookup;
    private readonly IGeographyLookup? _geography;
    private readonly StorefrontShippingOperationalShadowOptions? _options;
    private readonly ILogger<StorefrontShippingOperationalShadowObserver>? _logger;

    /// <summary>ناظر را با درزهای فقط-خواندنی می‌سازد؛ همهٔ درزها اختیاری‌اند تا نصب آن بی‌خطر باشد.</summary>
    /// <param name="lookup">درز عملیاتی فقط-خواندنی Shipping.</param>
    /// <param name="geography">درز فقط-خواندنی Geography برای حل قطعی مقصد.</param>
    /// <param name="options">تنظیمات دروازه؛ پیش‌فرض خاموش.</param>
    /// <param name="logger">لاگر ساخت‌یافته برای کد پایدار مشاهده.</param>
    public StorefrontShippingOperationalShadowObserver(
        IShippingOperationalQuoteLookup? lookup = null,
        IGeographyLookup? geography = null,
        IOptions<StorefrontShippingOperationalShadowOptions>? options = null,
        ILogger<StorefrontShippingOperationalShadowObserver>? logger = null)
    {
        _lookup = lookup;
        _geography = geography;
        _options = options?.Value;
        _logger = logger;
    }

    /// <summary>
    /// یک مشاهدهٔ عملیاتی انجام می‌دهد. خروجی فقط برای مشاهده/تلمتری است و هیچ مسیری به قیمت،
    /// وعدهٔ تحویل، پیش‌نویس یا snapshot سفارش ندارد.
    /// </summary>
    /// <param name="input">واقعیت‌های مسیر واقعی فروشگاه.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<StorefrontShippingOperationalShadowResult> ObserveAsync(
        StorefrontShippingOperationalShadowInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var options = _options;
        var gate = StorefrontShippingRateAuthorityPolicy.Evaluate(
            options?.Enabled ?? false,
            options?.PrerequisitesConfirmed ?? false);

        if (gate == StorefrontShippingRateAuthorityGate.Disabled)
        {
            // مسیر خاموش: بدون هیچ پرس‌وجو، بدون لاگ و بدون هزینه.
            return new StorefrontShippingOperationalShadowResult(
                StorefrontShippingOperationalShadowCode.Disabled,
                []);
        }

        if (gate != StorefrontShippingRateAuthorityGate.Allowed)
        {
            return Record(StorefrontShippingOperationalShadowCode.PrerequisitesUnconfirmed, [], input);
        }

        if (_lookup is null || _geography is null)
        {
            return Record(StorefrontShippingOperationalShadowCode.ShadowUnavailable, [], input);
        }

        if (string.IsNullOrWhiteSpace(input.ProvinceName) && string.IsNullOrWhiteSpace(input.CityName)
            && input.Destination is null)
        {
            // شکاف منبع: مسیر فروشگاه در این حالت هیچ مقصدی انتخاب نکرده است و Geography فقط می‌تواند
            // هویت سطح کشور بدهد؛ مقایسهٔ نرخ بدون مقصد، مشاهدهٔ معتبری نیست و حدس زده نمی‌شود.
            return Record(StorefrontShippingOperationalShadowCode.DestinationNotComparable, [], input);
        }

        // اندازهٔ فیزیکی واقعی و منبع معتبر، پیش‌شرط مقایسهٔ پله‌ای است. یک اندازهٔ بی‌منبع هرگز
        // به‌عنوان وزن تجاری پذیرفته نمی‌شود و نبود اندازه هم با وزن ساختگی جبران نمی‌شود: مقدار تهی
        // به مرجع می‌رود و یک قاعدهٔ شرط‌دار fail-closed قیمت نمی‌دهد.
        Tooba.Shipping.Contracts.Rates.ShippingMeasuredParcel? parcel;
        if (input.Parcel is { IsEmpty: false } measured)
        {
            if (string.IsNullOrWhiteSpace(input.ParcelProvenance))
            {
                return Record(StorefrontShippingOperationalShadowCode.MeasurementUnavailable, [], input);
            }

            parcel = measured;
        }
        else
        {
            parcel = null;
        }

        DestinationIdentifiers destination;
        try
        {
            var resolved = await ResolveDestinationAsync(input, cancellationToken);
            if (resolved is null)
            {
                return Record(StorefrontShippingOperationalShadowCode.DestinationNotComparable, [], input);
            }

            destination = resolved;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // نبود جدول/schema یا خطای Geography هرگز نباید مسیر فروشگاه را بشکند.
            return Record(StorefrontShippingOperationalShadowCode.ShadowUnavailable, [], input);
        }

        var reports = new List<StorefrontShippingOperationalShadowReport>(input.Candidates.Count);
        foreach (var candidate in input.Candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.ServiceCode))
            {
                continue;
            }

            reports.Add(await EvaluateAsync(candidate, destination, input, parcel, gate, cancellationToken));
        }

        // اگر همهٔ نامزدها فقط به‌خاطر نبود اندازهٔ واقعی قابل قیمت‌گذاری نبودند، کد مشاهده هم
        // «اندازه در دسترس نیست» است تا این شکاف در سطح مشاهده صریح باشد، نه پنهان در نتیجه‌ها.
        var code = reports.Count > 0
            && reports.All(r => r.Outcome == StorefrontShippingOperationalShadowOutcome.MeasurementUnavailable)
                ? StorefrontShippingOperationalShadowCode.MeasurementUnavailable
                : StorefrontShippingOperationalShadowCode.Observed;

        return Record(code, reports, input);
    }

    /// <summary>
    /// یک نامزد را ارزیابی می‌کند و هر خطای مرجع جدید را به یک نتیجهٔ مشاهده‌ای ترجمه می‌کند.
    /// </summary>
    private async Task<StorefrontShippingOperationalShadowReport> EvaluateAsync(
        StorefrontShippingOperationalShadowCandidate candidate,
        DestinationIdentifiers destination,
        StorefrontShippingOperationalShadowInput input,
        Tooba.Shipping.Contracts.Rates.ShippingMeasuredParcel? parcel,
        StorefrontShippingRateAuthorityGate gate,
        CancellationToken cancellationToken)
    {
        var serviceCode = candidate.ServiceCode.Trim();
        var optionCode = string.IsNullOrWhiteSpace(candidate.OptionCode) ? null : candidate.OptionCode.Trim();

        try
        {
            var quote = await _lookup!.QuoteAsync(
                new ShippingOperationalQuoteRequest(
                    serviceCode,
                    destination,
                    input.CartSubtotalExclusive,
                    input.CurrencyCode,
                    optionCode,
                    parcel,
                    input.RequestedPickupAtUtc),
                cancellationToken);

            if (quote is null)
            {
                return StorefrontShippingOperationalShadowComparer.NotEvaluated(
                    candidate,
                    gate,
                    StorefrontShippingOperationalShadowOutcome.ShippingUnavailable);
            }

            return StorefrontShippingOperationalShadowComparer.Compare(candidate, quote, gate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // نبود جدول/داده یا هر خطای مرجع جدید هرگز نباید مسیر قدیمی را بشکند.
            return StorefrontShippingOperationalShadowComparer.NotEvaluated(
                candidate,
                gate,
                StorefrontShippingOperationalShadowOutcome.ShippingUnavailable);
        }
    }

    /// <summary>
    /// هویت پایدار مقصد را با بالاترین دقت اثبات‌شده می‌سازد، یا <c>null</c> وقتی هیچ راه قطعی
    /// وجود ندارد.
    /// </summary>
    private async Task<DestinationIdentifiers?> ResolveDestinationAsync(
        StorefrontShippingOperationalShadowInput input,
        CancellationToken cancellationToken)
    {
        var geography = _geography!;

        if (input.Destination is { } explicitIdentifiers)
        {
            if (explicitIdentifiers.CountryId == Guid.Empty)
            {
                return null;
            }

            return await geography.GetByIdsAsync(explicitIdentifiers, cancellationToken);
        }

        var resolution = await geography.ResolveLegacyAsync(
            LegacyCountryCode,
            input.ProvinceName,
            input.CityName,
            serviceAreaName: null,
            cancellationToken);

        return resolution.Outcome == DestinationResolutionOutcome.Resolved
            ? resolution.Identifiers
            : null;
    }

    private StorefrontShippingOperationalShadowResult Record(
        StorefrontShippingOperationalShadowCode code,
        IReadOnlyList<StorefrontShippingOperationalShadowReport> reports,
        StorefrontShippingOperationalShadowInput input)
    {
        if (_logger is not null)
        {
            var outcomes = reports
                .Select(r => r.Outcome.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            _logger.LogInformation(
                "Storefront shipping operational shadow observation {ShadowObservationCode} candidates={CandidateCount} outcomes={ShadowOutcomes}",
                code.ToString(),
                input.Candidates.Count,
                outcomes.Length == 0 ? "none" : string.Join(',', outcomes));

            foreach (var report in reports)
            {
                _logger.LogInformation(
                    "Storefront shipping operational shadow candidate service={ServiceCode} option={OptionCode} legacy={LegacyAmount} freight={FreightAmount} packaging={PackagingAmount} payer={FreightPayerMode} schedule={ScheduleAvailability} capacity={CapacityEnforcement} outcome={ShadowOutcome} reason={CarrierReasonCode}",
                    report.ServiceCode,
                    report.OptionCode ?? "none",
                    report.LegacyPriceAmount,
                    Invariant(report.LegacyFreightAmount),
                    Invariant(report.PackagingAmount),
                    report.FreightPayerMode ?? "none",
                    report.ScheduleAvailability ?? "none",
                    report.CapacityEnforcement ?? "none",
                    report.Outcome.ToString(),
                    report.CarrierReasonCode ?? "none");
            }
        }

        return new StorefrontShippingOperationalShadowResult(code, reports);
    }

    private static string Invariant(decimal? amount) =>
        amount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none";
}
