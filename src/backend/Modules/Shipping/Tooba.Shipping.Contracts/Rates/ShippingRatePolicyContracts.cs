using Tooba.Geography.Contracts.Places;

namespace Tooba.Shipping.Contracts.Rates;

/// <summary>
/// سطح دامنهٔ مقصد یک سیاست نرخ یا یک مجوز پوشش. سطح یک واقعیت داده‌ای است، نه یک درخت ثابت:
/// دامنهٔ ریشه «کشور» است و هر سطح پایین‌تر باید والد خود را نیز داشته باشد.
/// </summary>
public enum ShippingDestinationLevel
{
    /// <summary>دامنه در سطح کشور.</summary>
    Country = 0,

    /// <summary>دامنه در سطح ناحیهٔ اداری (استان).</summary>
    AdministrativeArea = 1,

    /// <summary>دامنه در سطح شهر.</summary>
    City = 2,

    /// <summary>دامنه در سطح ناحیهٔ خدمت.</summary>
    ServiceArea = 3,
}

/// <summary>
/// شکست یک سیاست نرخ یا مجوز پوشش روی شناسه‌های یک مقصد. مقصد و پوشش هر دو با شناسهٔ پایدار
/// بیان می‌شوند؛ هیچ برچسب محلی‌شده‌ای در این قرارداد حمل نمی‌شود.
/// </summary>
/// <remarks>
/// یک شیء دامنه‌دار باید همیشه همهٔ والدها را داشته باشد (شهر بدون ناحیهٔ اداری تنها وقتی مجاز
/// است که واقعاً بدون ناحیه باشد)، وگرنه «نیمه‌دامنه»ای ساخته می‌شود که می‌تواند بی‌سروصدا به
/// مقصد اشتباه متصل شود.
/// </remarks>
public sealed record ShippingDestinationScope(
    Guid CountryId,
    Guid? AdministrativeAreaId = null,
    Guid? CityId = null,
    Guid? ServiceAreaId = null)
{
    /// <summary>دامنهٔ سطح کشور می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    public static ShippingDestinationScope ForCountry(Guid countryId) => new(countryId);

    /// <summary>دامنهٔ سطح ناحیهٔ اداری می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری.</param>
    public static ShippingDestinationScope ForAdministrativeArea(Guid countryId, Guid administrativeAreaId)
        => new(countryId, administrativeAreaId);

    /// <summary>دامنهٔ سطح شهر می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری؛ تهی یعنی شهر بدون ناحیه.</param>
    /// <param name="cityId">شناسهٔ شهر.</param>
    public static ShippingDestinationScope ForCity(Guid countryId, Guid? administrativeAreaId, Guid cityId)
        => new(countryId, administrativeAreaId, cityId);

    /// <summary>دامنهٔ سطح ناحیهٔ خدمت می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری؛ تهی یعنی شهر بدون ناحیه.</param>
    /// <param name="cityId">شناسهٔ شهر مالک ناحیهٔ خدمت.</param>
    /// <param name="serviceAreaId">شناسهٔ ناحیهٔ خدمت.</param>
    public static ShippingDestinationScope ForServiceArea(
        Guid countryId,
        Guid? administrativeAreaId,
        Guid cityId,
        Guid serviceAreaId)
        => new(countryId, administrativeAreaId, cityId, serviceAreaId);

    /// <summary>سطح دامنه بر پایهٔ عمیق‌ترین شناسهٔ موجود.</summary>
    public ShippingDestinationLevel Level => ServiceAreaId is not null
        ? ShippingDestinationLevel.ServiceArea
        : CityId is not null
            ? ShippingDestinationLevel.City
            : AdministrativeAreaId is not null
                ? ShippingDestinationLevel.AdministrativeArea
                : ShippingDestinationLevel.Country;
}

/// <summary>وضعیت خدمت‌رسانی برای یک درخواست نرخ.</summary>
public enum ShippingServiceabilityOutcome
{
    /// <summary>یک سیاست فعال با پوشش معتبر پیدا شد؛ نرخ قابل اعلام است.</summary>
    Quoted = 0,

    /// <summary>هیچ مجوز پوششی برای این مقصد وجود ندارد. عدم مجوز یعنی «پوشش‌نشده»، نه «بدون محدودیت».</summary>
    NotCovered = 1,

    /// <summary>کد خدمت هیچ سیاست فعالی ندارد.</summary>
    UnknownService = 2,

    /// <summary>بیش از یک سیاست هم‌سطح و ناسازگار با دامنهٔ درخواست سازگار است؛ انتخاب خودکار انجام نمی‌شود.</summary>
    ScopeConflict = 3,

    /// <summary>سیاست نرخ و ارز آن با ارز درخواست سازگار نیست.</summary>
    CurrencyMismatch = 4,

    /// <summary>
    /// همهٔ سیاست‌های سازگار برای این خدمت/گزینه/دامنه شرط‌دارند و درخواست اندازهٔ فیزیکی لازم
    /// (وزن یا تعداد اقلام) را اعلام نکرده است. غیبت اندازه هرگز به «تطبیق پلهٔ محدود» تعبیر نمی‌شود:
    /// یک درخواست بی‌اندازه به‌طور پیش‌فرض قیمت نمی‌گیرد.
    /// </summary>
    MeasurementRequired = 5,

    /// <summary>
    /// اندازهٔ فیزیکی اعلام‌شده با هیچ پلهٔ شرط‌دار سازگاری ندارد (خارج از همهٔ بازه‌های نیم‌باز).
    /// این حالت از «خدمت قیمت‌گذاری‌نشده» جدا است: سیاست وجود دارد، ولی برای این اندازه نیست.
    /// </summary>
    ConditionNotMatched = 6,

    /// <summary>
    /// بیش از یک پلهٔ شرط‌دار در همان سطح هویت/دامنه با اندازهٔ درخواست سازگار است. این ابهام یک
    /// تصمیم تجاری است و به‌جای انتخاب سطر اول صریحاً گزارش می‌شود.
    /// </summary>
    ConditionConflict = 7,
}

/// <summary>کدهای دلیلِ پایدار برای نتیجهٔ خدمت‌رسانی؛ مستقل از زبان و مناسب برای لاگ/تلمتری.</summary>
public static class ShippingReasonCodes
{
    /// <summary>نرخ از یک سیاست فعال و پوشش‌شده محاسبه شد.</summary>
    public const string Quoted = "shipping.quote.quoted";

    /// <summary>نرخ از سیاست فعال محاسبه شد و آستانهٔ ارسال رایگان اعمال شد.</summary>
    public const string QuotedFreeAboveThreshold = "shipping.quote.free_above_threshold";

    /// <summary>برای این کد خدمت و مقصد هیچ مجوز پوششی فعالی وجود ندارد.</summary>
    public const string CoverageMissing = "shipping.coverage.missing";

    /// <summary>کد خدمت هیچ سیاست فعالی ندارد.</summary>
    public const string ServiceNotPriced = "shipping.service.not_priced";

    /// <summary>چند سیاست هم‌سطح با دامنهٔ درخواست سازگارند.</summary>
    public const string ScopeAmbiguous = "shipping.scope.ambiguous";

    /// <summary>ارز درخواست با ارز سیاست یکسان نیست.</summary>
    public const string CurrencyMismatch = "shipping.currency.mismatch";

    /// <summary>سیاست سازگار شرط‌دار است و درخواست اندازهٔ فیزیکی لازم را اعلام نکرده.</summary>
    public const string MeasurementRequired = "shipping.quote.measurement_required";

    /// <summary>اندازهٔ فیزیکی اعلام‌شده با هیچ پلهٔ شرط‌دار سازگار نیست.</summary>
    public const string ConditionNotMatched = "shipping.quote.condition_not_matched";

    /// <summary>بیش از یک پلهٔ شرط‌دار با اندازهٔ درخواست سازگار است؛ ابهام حل نمی‌شود.</summary>
    public const string ConditionAmbiguous = "shipping.quote.condition_ambiguous";
}

/// <summary>
/// واحد وزن متعارف ارسال. یک واحد و یک عدد صحیح غیرمنفی انتخاب شده است تا مقایسهٔ پله‌ها
/// دقیق و قابل‌بازتولید باشد و خطای اعشاری در مرزها ظاهر نشود.
/// </summary>
public enum ShippingWeightUnit
{
    /// <summary>گرم؛ واحد متعارف و پیش‌فرض این نسخه.</summary>
    Gram = 0,
}

/// <summary>
/// بازهٔ نیم‌باز <c>[Min, Max)</c> برای یک شرط عددی پله‌ای: کران پایین شامل و کران بالا غیرشامل
/// است. <see cref="Max"/> تهی یعنی «کران بالا باز» (بی‌نهایت).
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا نیم‌باز.</b> با <c>[0,2000)</c> و <c>[2000,5000)</c> هیچ عددی در دو پله هم‌زمان نمی‌افتد و
/// هیچ عددی هم در مرز گم نمی‌شود؛ ۲۰۰۰ دقیقاً یک بار (در پلهٔ دوم) قیمت می‌گیرد. بازهٔ بسته
/// باعث می‌شد ۲۰۰۰ در هر دو پله بیفتد و «کدام پله برنده است» به ترتیب سطر وابسته شود.
/// </para>
/// <para>
/// <b>کدام انتها باز است.</b> فقط <see cref="Max"/> می‌تواند باز باشد؛ یک پلهٔ «از ۲۰۰۰ به بالا».
/// <see cref="Min"/> همیشه مشخص است، چون بازهٔ «تا ۲۰۰۰» را با <c>Min = 0</c> بیان می‌کنیم.
/// دو پلهٔ بی‌کران پایین/بالا روی یک هویت، یک ابهام واقعی است و در سطح داده رد می‌شود.
/// </para>
/// </remarks>
public sealed record ShippingNumericTier
{
    /// <summary>کران پایین؛ شامل و همیشه مشخص.</summary>
    public long Min { get; }

    /// <summary>کران بالا؛ غیرشامل. تهی یعنی بی‌کران بالا.</summary>
    public long? Max { get; }

    private ShippingNumericTier(long min, long? max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>بازهٔ <c>[min, max)</c> می‌سازد؛ <paramref name="max"/> تهی یعنی بی‌کران بالا.</summary>
    /// <param name="min">کران پایین شامل؛ نباید منفی باشد.</param>
    /// <param name="max">کران بالا غیرشامل؛ در صورت وجود باید اکیداً بزرگ‌تر از کران پایین باشد.</param>
    /// <returns>بازهٔ معتبر.</returns>
    public static ShippingNumericTier Between(long min, long? max) => new(min, max);

    /// <summary>آیا مقدار داده‌شده داخل این بازه است (پایین شامل، بالا غیرشامل).</summary>
    /// <param name="value">مقدار اندازه‌گیری‌شده.</param>
    public bool Contains(long value) => value >= Min && (Max is not { } max || value < max);

    /// <summary>
    /// آیا این بازه با بازهٔ دیگری هم‌پوشانی دارد. دو بازهٔ چسبیده (<c>[0,2000)</c> و
    /// <c>[2000,5000)</c>) هم‌پوشانی ندارند، دقیقاً به‌خاطر معنای نیم‌باز.
    /// </summary>
    /// <param name="other">بازهٔ دیگر.</param>
    public bool Overlaps(ShippingNumericTier other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var thisEnd = Max ?? long.MaxValue;
        var otherEnd = other.Max ?? long.MaxValue;
        return Min < otherEnd && other.Min < thisEnd;
    }

    /// <summary>آیا این بازه بی‌کران بالا است.</summary>
    public bool IsOpenUpper => Max is null;
}

/// <summary>
/// درخواست نرخ ارسال. مقصد با شناسه‌های پایدار Geography بیان می‌شود و مبلغ سبد جدا از هزینهٔ
/// ارسال است تا آستانهٔ ارسال رایگان بدون بازتفسیر قابل اعمال باشد.
/// </summary>
/// <param name="ServiceCode">کد خدمت ارسال؛ همان کدهای موجود Fulfillment، دست‌نخورده و فقط به‌عنوان کلید.</param>
/// <param name="Destination">شناسه‌های پایدار مقصد.</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال.</param>
/// <param name="CurrencyCode">ارز درخواست؛ تهی یعنی بررسی ارز انجام نشود.</param>
/// <param name="OptionCode">
/// گزینهٔ روش ارسال در صورت وجود؛ تهی یعنی فراخوان فقط در سطح خدمت می‌پرسد.
/// این مقدار <em>هرگز</em> به کد خدمت تبدیل نمی‌شود: «tipax:express» خدمت tipax و گزینهٔ express است.
/// </param>
/// <param name="Parcel">
/// اندازهٔ فیزیکی اندازه‌گیری‌شدهٔ مرسوله؛ تهی یعنی فراخوان هیچ اندازه‌ای اعلام نکرده است. Shipping
/// این مقدار را هرگز از جمع سبد یا تعداد خطوط سبد حدس نمی‌زند: یک قاعدهٔ شرط‌دار با اندازهٔ غایب
/// fail-closed قیمت نمی‌دهد.
/// </param>
public sealed record ShippingQuoteRequest(
    string ServiceCode,
    DestinationIdentifiers Destination,
    decimal CartSubtotalExclusive,
    string? CurrencyCode = null,
    string? OptionCode = null,
    ShippingMeasuredParcel? Parcel = null);

/// <summary>
/// اندازهٔ فیزیکی اندازه‌گیری‌شدهٔ مرسوله، همان‌طور که فراخوان اعلام می‌کند.
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا صریح و اختیاری.</b> قیمت‌گذاری پله‌ای فقط با اندازهٔ واقعی معنا دارد. Shipping هیچ‌گاه
/// اندازه را از جمع سبد، تعداد خطوط سبد یا هر کمیت دیگر <em>حدس نمی‌زند</em>: اگر فراخوان اندازه
/// نداشته باشد، مقدار تهی می‌ماند و یک قاعدهٔ شرط‌دار fail-closed قیمت نمی‌دهد.
/// </para>
/// <para>
/// <b>وزن.</b> یک عدد صحیح غیرمنفی بر حسب <see cref="ShippingWeightUnit.Gram"/>؛ عدد صحیح عمداً
/// انتخاب شده تا مرز پله‌ها دقیق باشد و خطای اعشاری/گردکردن وارد تصمیم تجاری نشود.
/// </para>
/// <para>
/// <b>تعداد اقلام.</b> تعداد اقلام مرسوله (تعداد واحدهای فیزیکی که بسته‌بندی می‌شوند)، نه تعداد
/// خطوط سبد و نه تعداد کل Quantity اقلام سبد. یک عدد صحیح مثبت است.
/// </para>
/// </remarks>
/// <param name="WeightGrams">وزن اندازه‌گیری‌شده بر حسب گرم؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="ItemCount">تعداد اقلام مرسوله؛ تهی یعنی اعلام‌نشده.</param>
public sealed record ShippingMeasuredParcel(long? WeightGrams = null, int? ItemCount = null)
{
    /// <summary>آیا هیچ اندازه‌ای اعلام نشده است.</summary>
    public bool IsEmpty => WeightGrams is null && ItemCount is null;

    /// <summary>پارسل با وزن اعلام‌شده.</summary>
    /// <param name="weightGrams">وزن گرم.</param>
    public static ShippingMeasuredParcel WithWeight(long weightGrams) => new(weightGrams, null);

    /// <summary>پارسل با تعداد اقلام اعلام‌شده.</summary>
    /// <param name="itemCount">تعداد اقلام.</param>
    public static ShippingMeasuredParcel WithItemCount(int itemCount) => new(null, itemCount);
}

/// <summary>
/// شرط عددی که یک سیاست نرخ برای تطبیق لازم دارد: بازهٔ وزن و بازهٔ تعداد اقلام، هر دو اختیاری.
/// </summary>
/// <remarks>
/// مقدار تهی در یک محور یعنی «این سیاست روی آن محور شرطی ندارد»؛ مقدار داشتن یعنی «فقط داخل این
/// بازهٔ نیم‌باز قیمت می‌دهد». سیاستی که هر دو محور تهی باشد یک قاعدهٔ بی‌شرط قدیمی است و رفتارش
/// دقیقاً مانند قبل باقی می‌ماند.
/// </remarks>
/// <param name="Weight">بازهٔ وزن گرم؛ تهی یعنی بی‌شرط روی وزن.</param>
/// <param name="Count">بازهٔ تعداد اقلام؛ تهی یعنی بی‌شرط روی تعداد.</param>
public sealed record ShippingRateCondition(
    ShippingNumericTier? Weight = null,
    ShippingNumericTier? Count = null)
{
    /// <summary>شرط بی‌شرط؛ یعنی سیاست روی هیچ محور اندازه‌ای محدود نیست.</summary>
    public static ShippingRateCondition Unconditioned { get; } = new();

    /// <summary>آیا این سیاست اصلاً شرط اندازه دارد.</summary>
    public bool IsConditioned => Weight is not null || Count is not null;

    /// <summary>آیا این سیاست فقط روی وزن شرط دارد.</summary>
    public bool IsWeightConditioned => Weight is not null;

    /// <summary>آیا این سیاست فقط روی تعداد اقلام شرط دارد.</summary>
    public bool IsCountConditioned => Count is not null;
}

/// <summary>
/// شرط واقعاً تطبیق‌یافتهٔ یک نرخ؛ برای شفافیت، شواهد و ویرایشگر آینده.
/// </summary>
/// <param name="Weight">بازهٔ وزن برنده؛ تهی یعنی این نرخ روی وزن شرطی نداشت.</param>
/// <param name="Count">بازهٔ تعداد برنده؛ تهی یعنی این نرخ روی تعداد شرطی نداشت.</param>
public sealed record ShippingConditionMatch(
    ShippingNumericTier? Weight,
    ShippingNumericTier? Count);

/// <summary>
/// نتیجهٔ قطعی و محدود خدمت‌رسانی همراه با جزئیات آمادهٔ promise مشتری. نتیجه هرگز «سرویس‌پذیر
/// به‌طور پیش‌فرض» نیست: نبود مجوز پوشش صریحاً <see cref="ShippingServiceabilityOutcome.NotCovered"/> است.
/// </summary>
/// <param name="Outcome">وضعیت خدمت‌رسانی.</param>
/// <param name="ServiceCode">کد خدمت درخواست‌شده.</param>
/// <param name="ChargeAmount">هزینهٔ ارسال؛ تنها در حالت <see cref="ShippingServiceabilityOutcome.Quoted"/> معنا دارد.</param>
/// <param name="CurrencyCode">ارز هزینه؛ تنها در حالت نرخ‌دار معنا دارد.</param>
/// <param name="PreparationDays">روزهای آماده‌سازی فروشنده؛ از روزهای حمل جدا است.</param>
/// <param name="LeadDays">روزهای حمل این خدمت.</param>
/// <param name="FreeAboveThreshold">آستانهٔ ارسال رایگان؛ در صورت وجود، برای شفافیت بازگردانده می‌شود.</param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
/// <param name="OptionCode">گزینهٔ درخواست‌شده؛ فقط برای ردیابی، بدون هیچ معنای قیمتی.</param>
/// <param name="MatchedCondition">
/// شرط اندازه‌ای که این نرخ با آن تطبیق یافت؛ تهی یعنی نرخ برنده یک قاعدهٔ بی‌شرط بود یا نرخی
/// اعلام نشد. این مقدار فقط توصیفی است و هیچ محاسبهٔ هزینه‌ای بر آن بنا نمی‌شود.
/// </param>
public sealed record ShippingQuote(
    ShippingServiceabilityOutcome Outcome,
    string ServiceCode,
    decimal ChargeAmount,
    string? CurrencyCode,
    int PreparationDays,
    int LeadDays,
    decimal? FreeAboveThreshold,
    string ReasonCode,
    string? OptionCode = null,
    ShippingConditionMatch? MatchedCondition = null)
{
    /// <summary>آیا نرخ قابل اعلام به مشتری است.</summary>
    public bool IsQuoted => Outcome == ShippingServiceabilityOutcome.Quoted;
}

/// <summary>
/// تنها درز خواندنِ بین‌ماژولی برای نرخ و پوشش ارسال. فقط-خواندنی است: هیچ درز نوشتنی برای
/// ماژول‌های دیگر باز نمی‌شود و Shipping هرگز «حکم خدمت‌رسانی» را از ماژول دیگری نمی‌گیرد.
/// </summary>
/// <remarks>
/// این درز عمداً سیاست را <em>بازنمی‌گرداند</em>؛ فقط نتیجهٔ قطعی نرخ را می‌دهد تا مصرف‌کننده
/// (Order) نتواند قواعد تجاری را بازپیاده‌سازی کند.
/// </remarks>
public interface IShippingRateQuoteLookup
{
    /// <summary>نرخ و خدمت‌رسانی یک درخواست را به‌صورت قطعی و fail-closed محاسبه می‌کند.</summary>
    /// <param name="request">درخواست نرخ.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>نتیجهٔ نرخ؛ هرگز null.</returns>
    Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken);
}
