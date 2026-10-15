using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Domain.Aggregates;

/// <summary>
/// سیاست تجاری نرخ ارسال: Shipping مالک «چقدر» و «با چه پوششی» است، نه هویت خدمت.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ownership.</b> این aggregate فقط سیاست تجاری را نگه می‌دارد. ردیف‌های کاتالوگ خدمت/گزینه
/// (هویت و نمایش) دارایی Fulfillment می‌مانند و اینجا تکرار نمی‌شوند: <see cref="ServiceCode"/>
/// تنها یک <em>کلید سازگاری</em> است، دقیقاً همان کدهایی که مصرف‌کننده‌های فعلی می‌شناسند.
/// </para>
/// <para>
/// <b>دو مفهوم زمانی جدا.</b> <see cref="PreparationDays"/> (آماده‌سازی فروشنده) و
/// <see cref="LeadDays"/> (حمل) عمداً دو فیلد جدا هستند؛ جمع‌کردن آن‌ها در یک عدد همان ابهامی
/// است که promise مشتری را غیرقابل‌حسابرسی می‌کند.
/// </para>
/// </remarks>
public sealed class ShippingRatePolicy
{
    /// <summary>حداکثر طول کد خدمت.</summary>
    public const int ServiceCodeMaxLength = 64;

    /// <summary>حداکثر طول کد گزینهٔ روش ارسال.</summary>
    public const int OptionCodeMaxLength = 64;

    /// <summary>حداکثر مبلغ مجاز هزینه (برای جلوگیری از خطای داده‌ای).</summary>
    public const decimal ChargeAmountMaxValue = 1_000_000_000m;

    /// <summary>حداکثر تعداد روز آماده‌سازی/حمل.</summary>
    public const int DaysMaxValue = 365;

    /// <summary>
    /// حداکثر وزن قابل تنظیم در یک پله (گرم). سقف عمدی است تا یک مقدار بی‌معنا (خطای ورودی یا
    /// سرریز) به یک قاعدهٔ تجاری تبدیل نشود.
    /// </summary>
    public const long WeightGramsMaxValue = 1_000_000_000L;

    /// <summary>حداکثر تعداد اقلام قابل تنظیم در یک پله.</summary>
    public const int ItemCountMaxValue = 1_000_000;

    private ShippingRatePolicy()
    {
    }

    /// <summary>شناسهٔ پایدار سیاست نرخ.</summary>
    public Guid RatePolicyId { get; init; }

    /// <summary>کد خدمت ارسال (کلید سازگاری؛ بدون تکرار ردیف کاتالوگ).</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>
    /// گزینهٔ روش ارسال در صورت وجود؛ تهی یعنی سیاست در سطح خود خدمت است.
    /// </summary>
    /// <remarks>
    /// این یک <em>هویت</em> است، نه بخشی از کد خدمت: سیاست گزینه‌دار فقط همان گزینه را قیمت
    /// می‌دهد و هیچ‌گاه روی گزینهٔ دیگری اثر نمی‌گذارد. سیاست بدون گزینه یک قاعدهٔ سطح‌خدمت است
    /// که تنها زمانی به کار می‌آید که برای همان گزینه سیاست اختصاصی‌تری وجود نداشته باشد.
    /// </remarks>
    public string? OptionCode { get; private set; }

    /// <summary>کشور مالک دامنه.</summary>
    public Guid CountryId { get; private set; }

    /// <summary>ناحیهٔ اداری دامنه؛ تهی یعنی دامنه در سطح کشور.</summary>
    public Guid? AdministrativeAreaId { get; private set; }

    /// <summary>شهر دامنه؛ تهی یعنی دامنه در سطح ناحیه/کشور.</summary>
    public Guid? CityId { get; private set; }

    /// <summary>ناحیهٔ خدمت دامنه؛ تهی یعنی دامنه در سطح شهر یا بالاتر.</summary>
    public Guid? ServiceAreaId { get; private set; }

    /// <summary>مبلغ ثابت هزینهٔ ارسال؛ هرگز منفی نیست.</summary>
    public decimal ChargeAmount { get; private set; }

    /// <summary>کد ارز سه‌حرفی.</summary>
    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>روزهای آماده‌سازی فروشنده.</summary>
    public int PreparationDays { get; private set; }

    /// <summary>روزهای حمل این خدمت.</summary>
    public int LeadDays { get; private set; }

    /// <summary>آستانهٔ ارسال رایگان؛ تهی یعنی بدون آستانه.</summary>
    public decimal? FreeAboveThreshold { get; private set; }

    /// <summary>بازهٔ وزن گرم این سیاست؛ تهی یعنی روی وزن بی‌شرط است.</summary>
    public long? MinWeightGrams { get; private set; }

    /// <summary>کران بالای غیرشامل بازهٔ وزن؛ تهی یعنی بی‌کران بالا یا بی‌شرط بودن روی وزن.</summary>
    public long? MaxWeightGrams { get; private set; }

    /// <summary>بازهٔ تعداد اقلام این سیاست؛ تهی یعنی روی تعداد بی‌شرط است.</summary>
    public int? MinItemCount { get; private set; }

    /// <summary>کران بالای غیرشامل بازهٔ تعداد؛ تهی یعنی بی‌کران بالا یا بی‌شرط بودن روی تعداد.</summary>
    public int? MaxItemCount { get; private set; }

    /// <summary>
    /// آیا این سیاست روی محور وزن شرط دارد. تشخیص با «کران پایین مقدار دارد» انجام می‌شود:
    /// یک پلهٔ وزنی همیشه کران پایین مشخص دارد، پس نبود آن یعنی بی‌شرط بودن روی وزن.
    /// </summary>
    public bool IsWeightConditioned => MinWeightGrams is not null;

    /// <summary>آیا این سیاست روی محور تعداد اقلام شرط دارد.</summary>
    public bool IsCountConditioned => MinItemCount is not null;

    /// <summary>آیا این سیاست هیچ شرط اندازه‌ای ندارد (یک قاعدهٔ بی‌شرط قدیمی).</summary>
    public bool IsUnconditioned => !IsWeightConditioned && !IsCountConditioned;

    /// <summary>شرط عددی این سیاست به‌صورت یک مقدار واحد.</summary>
    public ShippingRateCondition Condition =>
        new(
            MinWeightGrams is { } minWeight
                ? ShippingNumericTier.Between(minWeight, MaxWeightGrams)
                : null,
            MinItemCount is { } minCount
                ? ShippingNumericTier.Between(minCount, MaxItemCount)
                : null);

    /// <summary>آیا سیاست فعال است؛ رکورد هرگز فیزیکی حذف نمی‌شود.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>دامنهٔ مقصد این سیاست به‌صورت یک مقدار واحد.</summary>
    public ShippingDestinationScope Scope =>
        new(CountryId, AdministrativeAreaId, CityId, ServiceAreaId);

    /// <summary>آیا این سیاست یک گزینهٔ مشخص را قیمت می‌دهد (در مقابل قاعدهٔ سطح‌خدمت).</summary>
    public bool IsOptionSpecific => OptionCode is not null;

    /// <summary>
    /// آیا این سیاست برای گزینهٔ درخواست‌شده معتبر است. سیاست سطح‌خدمت (بدون گزینه) برای هر
    /// درخواست معتبر است؛ سیاست گزینه‌دار فقط برای گزینهٔ خودش.
    /// </summary>
    /// <param name="optionCode">گزینهٔ درخواست؛ تهی یعنی درخواست فقط در سطح خدمت است.</param>
    public bool MatchesOption(string? optionCode)
        => OptionCode is null
           || string.Equals(OptionCode, optionCode?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>سیاست نرخ جدید می‌سازد.</summary>
    /// <param name="serviceCode">کد خدمت ارسال.</param>
    /// <param name="scope">دامنهٔ مقصد سیاست.</param>
    /// <param name="chargeAmount">مبلغ هزینه؛ نباید منفی باشد.</param>
    /// <param name="currencyCode">کد ارز سه‌حرفی.</param>
    /// <param name="preparationDays">روزهای آماده‌سازی.</param>
    /// <param name="leadDays">روزهای حمل.</param>
    /// <param name="freeAboveThreshold">آستانهٔ ارسال رایگان اختیاری.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="ratePolicyId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    /// <param name="optionCode">گزینهٔ روش ارسال اختیاری؛ تهی یعنی سیاست در سطح خدمت است.</param>
    /// <param name="condition">
    /// شرط اندازه (وزن/تعداد) این سیاست؛ تهی یعنی قاعدهٔ بی‌شرط، دقیقاً مانند ردیف‌های قدیمی.
    /// </param>
    public static ShippingRatePolicy Create(
        string serviceCode,
        ShippingDestinationScope scope,
        decimal chargeAmount,
        string currencyCode,
        int preparationDays,
        int leadDays,
        decimal? freeAboveThreshold,
        DateTimeOffset now,
        Guid? ratePolicyId = null,
        string? optionCode = null,
        ShippingRateCondition? condition = null)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var normalizedCondition = NormalizeCondition(condition);
        var policy = new ShippingRatePolicy
        {
            RatePolicyId = ratePolicyId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            ServiceCode = NormalizeServiceCode(serviceCode),
            OptionCode = NormalizeOptionCode(optionCode),
            CountryId = RequireCountry(scope.CountryId),
            AdministrativeAreaId = Normalize(scope.AdministrativeAreaId),
            CityId = Normalize(scope.CityId),
            ServiceAreaId = Normalize(scope.ServiceAreaId),
            ChargeAmount = NormalizeCharge(chargeAmount),
            CurrencyCode = NormalizeCurrency(currencyCode),
            PreparationDays = NormalizeDays(preparationDays, ShippingErrorCodes.PreparationDaysNegative),
            LeadDays = NormalizeDays(leadDays, ShippingErrorCodes.LeadDaysNegative),
            FreeAboveThreshold = NormalizeThreshold(freeAboveThreshold),
            MinWeightGrams = normalizedCondition.Weight?.Min,
            MaxWeightGrams = normalizedCondition.Weight?.Max,
            MinItemCount = normalizedCondition.Count is { } count ? checked((int)count.Min) : null,
            MaxItemCount = normalizedCondition.Count is { } count2 && count2.Max is { } max
                ? checked((int)max)
                : null,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        policy.EnsureScopeIsCoherent();
        return policy;
    }

    /// <summary>
    /// شرط اندازهٔ این سیاست را جایگزین می‌کند. هویت (خدمت/گزینه/دامنه) دست‌نخورده می‌ماند: تغییر
    /// شرط، همان قاعدهٔ تجاری را در یک پلهٔ دیگر بازتعریف می‌کند، نه یک قاعدهٔ جدید می‌سازد.
    /// </summary>
    /// <param name="condition">شرط جدید؛ تهی یعنی بازگشت به قاعدهٔ بی‌شرط.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeCondition(ShippingRateCondition? condition, DateTimeOffset now)
    {
        var normalized = NormalizeCondition(condition);
        MinWeightGrams = normalized.Weight?.Min;
        MaxWeightGrams = normalized.Weight?.Max;
        MinItemCount = normalized.Count is { } count ? checked((int)count.Min) : null;
        MaxItemCount = normalized.Count is { } count2 && count2.Max is { } max ? checked((int)max) : null;
        UpdatedAt = now;
    }

    /// <summary>
    /// آیا اندازهٔ اندازه‌گیری‌شدهٔ درخواست داخل شرط این سیاست می‌افتد.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fail-closed روی اندازهٔ غایب.</b> اگر این سیاست روی محوری شرط دارد و درخواست آن محور را
    /// اعلام نکرده است، نتیجه <c>false</c> است: نبود اندازه هرگز به «داخل بازه» تعبیر نمی‌شود،
    /// وگرنه یک پلهٔ محدود بی‌سروصدا روی همهٔ مرسوله‌ها اثر می‌گذاشت.
    /// </para>
    /// <para>
    /// <b>بی‌شرط یعنی همه‌چیز.</b> سیاستی که روی محوری شرط ندارد، روی آن محور با هر مقداری
    /// (حتی اعلام‌نشده) سازگار است؛ بنابراین ردیف‌های قدیمی بدون شرط رفتارشان تغییر نمی‌کند.
    /// </para>
    /// </remarks>
    /// <param name="parcel">اندازهٔ اعلام‌شده؛ تهی یعنی هیچ اندازه‌ای اعلام نشده.</param>
    public bool MatchesCondition(ShippingMeasuredParcel? parcel)
    {
        if (IsUnconditioned)
        {
            return true;
        }

        if (MinWeightGrams is { } minWeight)
        {
            if (parcel?.WeightGrams is not { } weight)
            {
                return false;
            }

            var weightTier = ShippingNumericTier.Between(minWeight, MaxWeightGrams);
            if (!weightTier.Contains(weight))
            {
                return false;
            }
        }

        if (MinItemCount is { } minCount)
        {
            if (parcel?.ItemCount is not { } count)
            {
                return false;
            }

            var countTier = ShippingNumericTier.Between(minCount, MaxItemCount);
            if (!countTier.Contains(count))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>مبلغ هزینه را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    /// <param name="chargeAmount">مبلغ جدید.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeCharge(decimal chargeAmount, DateTimeOffset now)
    {
        ChargeAmount = NormalizeCharge(chargeAmount);
        UpdatedAt = now;
    }

    /// <summary>آستانهٔ ارسال رایگان را تعیین یا حذف می‌کند.</summary>
    /// <param name="freeAboveThreshold">آستانهٔ جدید؛ تهی یعنی بدون آستانه.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void SetFreeAboveThreshold(decimal? freeAboveThreshold, DateTimeOffset now)
    {
        FreeAboveThreshold = NormalizeThreshold(freeAboveThreshold);
        UpdatedAt = now;
    }

    /// <summary>روزهای آماده‌سازی و حمل را جداگانه به‌روز می‌کند.</summary>
    /// <param name="preparationDays">روزهای آماده‌سازی.</param>
    /// <param name="leadDays">روزهای حمل.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeLeadTimes(int preparationDays, int leadDays, DateTimeOffset now)
    {
        PreparationDays = NormalizeDays(preparationDays, ShippingErrorCodes.PreparationDaysNegative);
        LeadDays = NormalizeDays(leadDays, ShippingErrorCodes.LeadDaysNegative);
        UpdatedAt = now;
    }

    /// <summary>سیاست را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>سیاست را غیرفعال می‌کند؛ شناسهٔ پایدار برای ارجاع تاریخی حفظ می‌شود.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>
    /// آیا این سیاست روی محوری شرط دارد که درخواست آن را اعلام نکرده است. تفکیک «اندازه لازم
    /// اعلام نشده» از «اندازهٔ اعلام‌شده در هیچ پله‌ای نیست» فقط با این پرسش ممکن است، و این دو
    /// وضعیت برای مصرف‌کننده دو واقعیت متفاوت‌اند.
    /// </summary>
    /// <param name="parcel">اندازهٔ اعلام‌شده؛ تهی یعنی هیچ اندازه‌ای اعلام نشده.</param>
    public bool HasUnsuppliedAxis(ShippingMeasuredParcel? parcel)
        => (IsWeightConditioned && parcel?.WeightGrams is null)
           || (IsCountConditioned && parcel?.ItemCount is null);

    /// <summary>
    /// آیا شرط اندازهٔ داده‌شده با شرط این سیاست هم‌پوشانی پله‌ای دارد. این پرسش روی همان هویت
    /// (خدمت/گزینه/دامنه) پرسیده می‌شود، چون فقط ردیف‌های هم‌هویت با هم رقابت می‌کنند.
    /// </summary>
    /// <remarks>
    /// یک طرف بی‌شرط یعنی «هم‌پوشانی» شمرده نمی‌شود: بی‌شرط همهٔ اندازه‌ها را می‌پوشاند و در موتور
    /// به‌عنوان قاعدهٔ پایهٔ مستند پذیرفته شده است، پس نوشتن نرخ پایه در کنار پله‌ها مسدود نمی‌شود.
    /// فقط دو پلهٔ شرط‌دار که بازه‌هایشان روی یک محور تلاقی دارد یک ابهام واقعی‌اند.
    /// </remarks>
    /// <param name="candidate">شرط پیشنهادی برای نوشتن.</param>
    public bool TiersOverlapWith(ShippingRateCondition? candidate)
    {
        var other = NormalizeCondition(candidate);
        if (IsUnconditioned || !other.IsConditioned)
        {
            return false;
        }

        var weightOverlap = MinWeightGrams is null
            || other.Weight is null
            || Condition.Weight!.Overlaps(other.Weight);
        var countOverlap = MinItemCount is null
            || other.Count is null
            || Condition.Count!.Overlaps(other.Count);

        return weightOverlap && countOverlap;
    }

    /// <summary>
    /// آیا این سیاست دامنهٔ مقصد داده‌شده را پوشش می‌دهد. هر سطحی که در سیاست مقدار دارد باید با
    /// مقصد برابر باشد؛ مقصد می‌تواند عمیق‌تر از سیاست باشد (سیاست یک زیردرخت را پوشش می‌دهد).
    /// </summary>
    /// <param name="destination">دامنهٔ مقصد درخواست.</param>
    public bool Covers(ShippingDestinationScope destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (CountryId != destination.CountryId)
        {
            return false;
        }

        if (AdministrativeAreaId is { } area && area != destination.AdministrativeAreaId)
        {
            return false;
        }

        if (CityId is { } city && city != destination.CityId)
        {
            return false;
        }

        return ServiceAreaId is not { } serviceArea || serviceArea == destination.ServiceAreaId;
    }

    private void EnsureScopeIsCoherent()
    {
        if (ServiceAreaId is not null && CityId is null)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.CoverageScopeInvalid));
        }

        if (CityId is null && AdministrativeAreaId is not null && ServiceAreaId is not null)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.CoverageScopeInvalid));
        }
    }

    private static string NormalizeServiceCode(string? serviceCode)
    {
        var trimmed = serviceCode?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        if (trimmed.Length > ServiceCodeMaxLength)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeInvalid));
        }

        return trimmed;
    }

    /// <summary>
    /// کد گزینه را به شکل متعارف می‌برد: فاصله‌های اضافی حذف و مقدار تهی به <c>null</c> نگاشت
    /// می‌شود تا «بدون گزینه» یک حالت واحد داشته باشد و ردیف‌های هم‌معنا تکراری نشوند.
    /// </summary>
    private static string? NormalizeOptionCode(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > OptionCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.OptionCodeInvalid))
            : trimmed;
    }

    private static Guid RequireCountry(Guid countryId)
        => countryId == Guid.Empty
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.DestinationCountryIdRequired))
            : countryId;

    private static Guid? Normalize(Guid? id) => id is { } value && value != Guid.Empty ? value : null;

    private static decimal NormalizeCharge(decimal chargeAmount)
    {
        if (chargeAmount < 0m)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ChargeAmountNegative));
        }

        return chargeAmount > ChargeAmountMaxValue
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ChargeAmountOutOfRange))
            : chargeAmount;
    }

    private static decimal? NormalizeThreshold(decimal? freeAboveThreshold)
    {
        if (freeAboveThreshold is not { } threshold)
        {
            return null;
        }

        return threshold < 0m
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.FreeAboveThresholdNegative))
            : threshold;
    }

    /// <summary>
    /// شرط اندازه را اعتبارسنجی و به شکل متعارف می‌برد. قواعد عمداً سخت‌گیرانه‌اند:
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>کران پایین منفی مجاز نیست؛ وزن و تعداد کمیت فیزیکی‌اند و منفی معنا ندارند.</item>
    /// <item>کران بالا در صورت وجود باید اکیداً بزرگ‌تر از کران پایین باشد؛ بازهٔ خالی/معکوس
    /// یک پلهٔ بی‌معنا است که هرگز نباید ذخیره شود.</item>
    /// <item>هر دو کران باید داخل سقف متعارف باشند تا خطای ورودی به یک قاعدهٔ تجاری تبدیل نشود.</item>
    /// <item>یک شرط کاملاً خالی (<c>new ShippingRateCondition()</c>) به <c>null</c> نگاشت می‌شود تا
    /// «بی‌شرط» تنها یک نمایش داشته باشد و ردیف‌های هم‌معنا تکراری ساخته نشوند.</item>
    /// </list>
    /// </remarks>
    private static ShippingRateCondition NormalizeCondition(ShippingRateCondition? condition)
    {
        if (condition is null || !condition.IsConditioned)
        {
            return ShippingRateCondition.Unconditioned;
        }

        var weight = NormalizeTier(
            condition.Weight,
            ShippingErrorCodes.WeightTierInvalid,
            ShippingErrorCodes.WeightTierOutOfRange,
            WeightGramsMaxValue);
        var count = NormalizeTier(
            condition.Count,
            ShippingErrorCodes.CountTierInvalid,
            ShippingErrorCodes.CountTierOutOfRange,
            ItemCountMaxValue);

        return new ShippingRateCondition(weight, count);
    }

    private static ShippingNumericTier? NormalizeTier(
        ShippingNumericTier? tier,
        string invalidCode,
        string outOfRangeCode,
        long maxValue)
    {
        if (tier is null)
        {
            return null;
        }

        if (tier.Min < 0 || tier.Min > maxValue)
        {
            throw new SemanticException(new SemanticError(
                tier.Min < 0 ? invalidCode : outOfRangeCode));
        }

        if (tier.Max is not { } upper)
        {
            return ShippingNumericTier.Between(tier.Min, null);
        }

        if (upper <= tier.Min)
        {
            throw new SemanticException(new SemanticError(invalidCode));
        }

        return upper > maxValue
            ? throw new SemanticException(new SemanticError(outOfRangeCode))
            : ShippingNumericTier.Between(tier.Min, upper);
    }

    private static int NormalizeDays(int days, string errorCode)
        => days is < 0 or > DaysMaxValue
            ? throw new SemanticException(new SemanticError(errorCode))
            : days;

    private static string NormalizeCurrency(string? currencyCode)
    {
        var trimmed = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmed.Length != 3 || !trimmed.All(char.IsAsciiLetter))
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.CurrencyCodeInvalid));
        }

        return trimmed;
    }
}
