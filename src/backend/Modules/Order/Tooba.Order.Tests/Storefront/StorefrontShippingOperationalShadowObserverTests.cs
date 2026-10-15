using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Shipping.OperationalShadow;
using Tooba.Order.Application.Storefront.Shipping.RateAuthority;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// آزمون‌های متمرکز ناظر سایهٔ <b>عملیاتی</b> روی مسیر واقعی projection ارسال: دروازهٔ مستقل و
/// پیش‌فرض‌خاموش هیچ پرس‌وجویی نمی‌زند، نبود اندازهٔ واقعی به «مقایسهٔ ساختگی» تبدیل نمی‌شود،
/// بسته‌بندی جداگانه داخل مبلغ مشتری جمع نمی‌شود و ظرفیت هرگز «اجراشده» ادعا نمی‌شود.
/// </summary>
public sealed class StorefrontShippingOperationalShadowObserverTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    // ---------- fakes ----------

    private sealed class FakeGeography : IGeographyLookup
    {
        private readonly Func<DestinationResolution> _resolution;

        public FakeGeography(Func<DestinationResolution>? resolution = null)
            => _resolution = resolution ?? (() => DestinationResolution.Resolved(
                new DestinationIdentifiers(CountryId, AreaId)));

        public int ResolveCalls { get; private set; }

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
        {
            ResolveCalls++;
            return Task.FromResult(_resolution());
        }

        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CountrySnapshot>>([]);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AdministrativeAreaSnapshot>>([]);

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CitySnapshot>>([]);

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ServiceAreaSnapshot>>([]);

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken)
            => Task.FromResult<DestinationIdentifiers?>(identifiers);
    }

    private sealed class RecordingOperationalLookup : IShippingOperationalQuoteLookup
    {
        private readonly Func<ShippingOperationalQuoteRequest, ShippingOperationalQuote> _factory;

        public RecordingOperationalLookup(Func<ShippingOperationalQuoteRequest, ShippingOperationalQuote> factory)
            => _factory = factory;

        public int Calls { get; private set; }

        public ShippingOperationalQuoteRequest? LastRequest { get; private set; }

        public Task<ShippingOperationalQuote> QuoteAsync(
            ShippingOperationalQuoteRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(_factory(request));
        }
    }

    private sealed class ThrowingOperationalLookup : IShippingOperationalQuoteLookup
    {
        public Task<ShippingOperationalQuote> QuoteAsync(
            ShippingOperationalQuoteRequest request,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("relation \"shipping.rate_policies\" does not exist");
    }

    // ---------- helpers ----------

    private static StorefrontShippingOperationalShadowOptions Gate(bool enabled, bool confirmed) => new()
    {
        Enabled = enabled,
        PrerequisitesConfirmed = confirmed,
    };

    private static StorefrontShippingOperationalShadowObserver Observer(
        IGeographyLookup? geography,
        IShippingOperationalQuoteLookup? lookup,
        StorefrontShippingOperationalShadowOptions options)
        => new(
            lookup,
            geography,
            Options.Create(options),
            NullLogger<StorefrontShippingOperationalShadowObserver>.Instance);

    private static StorefrontShippingOperationalShadowInput Input(
        string? province = "alborz",
        ShippingMeasuredParcel? parcel = null,
        string? provenance = null,
        params StorefrontShippingOperationalShadowCandidate[] candidates)
        => new(
            province,
            500_000m,
            "IRR",
            candidates.Length == 0
                ? [new StorefrontShippingOperationalShadowCandidate("tipax", "standard", 180_000m, 3)]
                : candidates)
        {
            Parcel = parcel,
            ParcelProvenance = provenance,
        };

    private static ShippingMeasuredParcel Parcel(long? grams = 1999, int? count = 2) => new(grams, count);

    private static ShippingQuote CarrierQuote(
        ShippingServiceabilityOutcome outcome,
        decimal amount,
        string reason,
        ShippingConditionMatch? matched = null)
        => new(outcome, "tipax", amount, "IRR", 1, 3, null, reason, "standard", matched);

    private static ShippingPackagingFeeComponent Packaging(
        bool charged,
        decimal fee,
        ShippingFreightPayerMode payerMode = ShippingFreightPayerMode.Prepaid)
        => new(
            charged,
            fee,
            charged ? "IRR" : null,
            payerMode,
            ShippingPackagingChargePayer.Checkout,
            charged ? Guid.NewGuid() : null,
            charged ? ShippingPackagingReasonCodes.Charged : ShippingPackagingReasonCodes.NotConfigured);

    private static ShippingScheduleEvaluation Schedule(
        ShippingScheduleAvailability availability = ShippingScheduleAvailability.Available,
        int? capacity = null)
        => new(
            availability,
            availability switch
            {
                ShippingScheduleAvailability.Available => ShippingScheduleEvaluationReasonCodes.Available,
                ShippingScheduleAvailability.Unavailable => ShippingScheduleEvaluationReasonCodes.OutsideWindows,
                ShippingScheduleAvailability.NotEvaluated => ShippingScheduleEvaluationReasonCodes.NotEvaluated,
                _ => ShippingScheduleEvaluationReasonCodes.NotConfigured,
            },
            TimeZoneId: availability is ShippingScheduleAvailability.Available
                or ShippingScheduleAvailability.Unavailable
                ? "Asia/Tehran"
                : null,
            ConfiguredCapacityTotal: capacity,
            CapacityEnforcement: ShippingCapacityEnforcement.NotEnforced);

    private static ShippingOperationalQuote Quoted(
        decimal freight,
        ShippingPackagingFeeComponent packaging,
        ShippingScheduleEvaluation? schedule = null,
        ShippingConditionMatch? matched = null)
        => new(
            ShippingOperationalQuoteOutcome.Quoted,
            "tipax",
            "standard",
            "IRR",
            freight,
            packaging.FeeAmount,
            freight + packaging.FeeAmount,
            packaging.FreightPayerMode == ShippingFreightPayerMode.CollectOnDelivery
                ? packaging.FeeAmount
                : freight + packaging.FeeAmount,
            packaging.FreightPayerMode == ShippingFreightPayerMode.CollectOnDelivery ? freight : 0m,
            packaging.FreightPayerMode,
            ShippingPackagingChargePayer.Checkout,
            CarrierQuote(ShippingServiceabilityOutcome.Quoted, freight, ShippingReasonCodes.Quoted, matched),
            packaging,
            schedule ?? Schedule(),
            ShippingOperationalQuoteReasonCodes.Quoted,
            matched);

    private static ShippingOperationalQuote NotQuoted(
        ShippingOperationalQuoteOutcome outcome,
        string reasonCode,
        ShippingReasonCodesKind carrierKind = ShippingReasonCodesKind.CoverageMissing,
        ShippingPackagingFeeComponent? packaging = null)
    {
        var carrierReason = carrierKind switch
        {
            ShippingReasonCodesKind.MeasurementRequired => ShippingReasonCodes.MeasurementRequired,
            ShippingReasonCodesKind.ConditionNotMatched => ShippingReasonCodes.ConditionNotMatched,
            ShippingReasonCodesKind.CurrencyMismatch => ShippingReasonCodes.CurrencyMismatch,
            _ => ShippingReasonCodes.CoverageMissing,
        };
        var carrierOutcome = carrierKind == ShippingReasonCodesKind.MeasurementRequired
            ? ShippingServiceabilityOutcome.MeasurementRequired
            : ShippingServiceabilityOutcome.NotCovered;
        var pack = packaging ?? Packaging(false, 0m);

        return new ShippingOperationalQuote(
            outcome,
            "tipax",
            "standard",
            null,
            null,
            null,
            null,
            null,
            null,
            pack.FreightPayerMode,
            ShippingPackagingChargePayer.Checkout,
            CarrierQuote(carrierOutcome, 0m, carrierReason),
            pack,
            Schedule(ShippingScheduleAvailability.NotConfigured),
            reasonCode);
    }

    private enum ShippingReasonCodesKind
    {
        CoverageMissing,
        MeasurementRequired,
        ConditionNotMatched,
        CurrencyMismatch,
    }

    // ---------- gate ----------

    [Fact]
    public async Task Operational_shadow_is_off_by_default_and_performs_no_query()
    {
        var geography = new FakeGeography();
        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(geography, lookup, new StorefrontShippingOperationalShadowOptions());

        var result = await observer.ObserveAsync(Input(parcel: Parcel(), provenance: "packing"), CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.Disabled, result.Code);
        Assert.Empty(result.Reports);
        Assert.Equal(0, geography.ResolveCalls);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Operational_shadow_enabled_without_confirmed_prerequisites_performs_no_lookup()
    {
        var geography = new FakeGeography();
        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: false));

        var result = await observer.ObserveAsync(Input(parcel: Parcel(), provenance: "packing"), CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.PrerequisitesUnconfirmed, result.Code);
        Assert.Equal(0, geography.ResolveCalls);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Simple_rate_shadow_gate_and_operational_gate_are_independent()
    {
        // روشن‌کردن سایهٔ ساده نباید سایهٔ عملیاتی را فعال کند و برعکس.
        var simpleGate = StorefrontShippingRateAuthorityPolicy.Evaluate(
            new StorefrontShippingRateAuthorityOptions { ShadowEnabled = true, PrerequisitesConfirmed = true });
        Assert.Equal(StorefrontShippingRateAuthorityGate.Allowed, simpleGate);

        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(new FakeGeography(), lookup, new StorefrontShippingOperationalShadowOptions());

        var result = await observer.ObserveAsync(Input(parcel: Parcel(), provenance: "packing"), CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.Disabled, result.Code);
        Assert.Equal(0, lookup.Calls);
    }

    // ---------- measurement provenance ----------

    [Fact]
    public async Task Missing_measurement_is_reported_and_no_tiered_comparison_is_fabricated()
    {
        // مرجع با اندازهٔ تهی یک قاعدهٔ شرط‌دار را fail-closed رد می‌کند؛ این «شکاف اندازه» است،
        // نه یک اختلاف نرخ ساختگی، و هیچ وزنی از سبد ساخته نشد.
        var lookup = new RecordingOperationalLookup(_ => NotQuoted(
            ShippingOperationalQuoteOutcome.FreightNotQuoted,
            ShippingOperationalQuoteReasonCodes.FreightNotQuoted,
            ShippingReasonCodesKind.MeasurementRequired));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(parcel: null), CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.MeasurementUnavailable, result.Code);
        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.MeasurementUnavailable, report.Outcome);
        Assert.Null(report.LegacyFreightAmount);
        // اندازهٔ تهی به مرجع رفت؛ هیچ عددی از سبد ساخته نشد.
        Assert.Null(lookup.LastRequest!.Parcel);
    }

    [Fact]
    public async Task Measurement_without_a_trusted_provenance_is_refused()
    {
        // یک اندازه بدون منبع معتبر هرگز به‌عنوان وزن تجاری پذیرفته نمی‌شود و به مرجع نمی‌رسد.
        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(parcel: Parcel(), provenance: "  "), CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.MeasurementUnavailable, result.Code);
        Assert.Empty(result.Reports);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Real_measurement_reaches_the_authority_verbatim()
    {
        // اندازه‌ای که یک فراخوان معتبر (مثلاً یک harness متمرکز) می‌دهد دست‌نخورده به مرجع می‌رسد؛
        // این لایه هیچ عددی را خودش نمی‌سازد و هیچ گردکردنی انجام نمی‌دهد.
        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        await observer.ObserveAsync(
            Input(parcel: Parcel(grams: 1999, count: 2), provenance: "packing:aggregate"),
            CancellationToken.None);

        Assert.Equal(1, lookup.Calls);
        Assert.Equal(1999, lookup.LastRequest!.Parcel!.WeightGrams);
        Assert.Equal(2, lookup.LastRequest.Parcel.ItemCount);
        Assert.Equal(500_000m, lookup.LastRequest.CartSubtotalExclusive);
        Assert.Equal("IRR", lookup.LastRequest.CurrencyCode);
        Assert.Equal("standard", lookup.LastRequest.OptionCode);
    }

    [Fact]
    public async Task Tier_boundary_is_priced_by_the_authority_and_never_re_derived_here()
    {
        // 1999 گرم و 2000 گرم دو پاسخ متفاوت می‌گیرند؛ تصمیم مرز کاملاً در اختیار موتور نرخ است و
        // این لایه هیچ آستانه‌ای را دوباره پیاده نمی‌کند.
        var boundary = new ShippingConditionMatch(
            ShippingNumericTier.Between(2000, 5000),
            null);
        var lookup = new RecordingOperationalLookup(request =>
            request.Parcel!.WeightGrams >= 2000
                ? Quoted(240_000m, Packaging(false, 0m), matched: boundary)
                : Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var below = await observer.ObserveAsync(
            Input(parcel: Parcel(grams: 1999, count: 2), provenance: "packing", candidates:
                new StorefrontShippingOperationalShadowCandidate("tipax", "standard", 180_000m, 3)),
            CancellationToken.None);
        var above = await observer.ObserveAsync(
            Input(parcel: Parcel(grams: 2000, count: 2), provenance: "packing", candidates:
                new StorefrontShippingOperationalShadowCandidate("tipax", "standard", 240_000m, 3)),
            CancellationToken.None);

        Assert.Equal(
            StorefrontShippingOperationalShadowOutcome.Match,
            Assert.Single(below.Reports).Outcome);
        Assert.Equal(
            StorefrontShippingOperationalShadowOutcome.Match,
            Assert.Single(above.Reports).Outcome);
        Assert.Equal(180_000m, Assert.Single(below.Reports).LegacyFreightAmount);
        Assert.Equal(240_000m, Assert.Single(above.Reports).LegacyFreightAmount);
    }

    // ---------- destination ----------

    [Fact]
    public async Task Absent_destination_is_not_comparable_and_never_guessed()
    {
        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(province: null, parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.DestinationNotComparable, result.Code);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Unresolved_legacy_label_is_not_comparable()
    {
        var geography = new FakeGeography(() => DestinationResolution.Unresolved());
        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(province: "shiraz-free-text", parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.DestinationNotComparable, result.Code);
        Assert.Equal(0, lookup.Calls);
    }

    // ---------- comparison outcomes ----------

    [Fact]
    public async Task Freight_parity_with_no_packaging_fee_is_a_match()
    {
        var lookup = new RecordingOperationalLookup(_ => Quoted(180_000m, Packaging(false, 0m)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.Match, report.Outcome);
        Assert.Equal(180_000m, report.LegacyFreightAmount);
        Assert.Equal(0m, report.PackagingAmount);
        Assert.Equal("Prepaid", report.FreightPayerMode);
    }

    [Fact]
    public async Task Charged_packaging_is_reported_separately_and_never_folded_into_the_customer_amount()
    {
        var lookup = new RecordingOperationalLookup(
            _ => Quoted(180_000m, Packaging(charged: true, fee: 25_000m)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.PackagingNotReflected, report.Outcome);
        // مبلغ قدیمی دست‌نخورده است و هزینهٔ بسته‌بندی فقط گزارش می‌شود.
        Assert.Equal(180_000m, report.LegacyPriceAmount);
        Assert.Equal(25_000m, report.PackagingAmount);
        Assert.Equal(205_000m, report.TotalServiceExpense);
        Assert.Equal(ShippingPackagingReasonCodes.Charged, report.PackagingReasonCode);
    }

    [Fact]
    public async Task Collect_on_delivery_split_is_observed_as_a_fact_without_any_payment_effect()
    {
        var lookup = new RecordingOperationalLookup(
            _ => Quoted(
                180_000m,
                Packaging(charged: true, fee: 25_000m, payerMode: ShippingFreightPayerMode.CollectOnDelivery)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal("CollectOnDelivery", report.FreightPayerMode);
        Assert.Equal("Checkout", report.PackagingPayer);
        Assert.Equal(25_000m, report.AmountPayableAtCheckout);
        Assert.Equal(180_000m, report.AmountCollectOnDelivery);
    }

    [Fact]
    public async Task Freight_amount_mismatch_is_reported_and_the_legacy_amount_stays_authoritative()
    {
        var lookup = new RecordingOperationalLookup(_ => Quoted(999_999m, Packaging(false, 0m)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.AmountMismatch, report.Outcome);
        Assert.Equal(180_000m, report.LegacyPriceAmount);
        Assert.Equal(999_999m, report.LegacyFreightAmount);
    }

    [Fact]
    public async Task Missing_coverage_is_reported_and_never_becomes_allowed_or_free()
    {
        var lookup = new RecordingOperationalLookup(
            _ => NotQuoted(
                ShippingOperationalQuoteOutcome.FreightNotQuoted,
                ShippingOperationalQuoteReasonCodes.FreightNotQuoted));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.LegacyAllowedShippingDenied, report.Outcome);
        Assert.Null(report.LegacyFreightAmount);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, report.CarrierReasonCode);
        Assert.Equal(ShippingOperationalQuoteReasonCodes.FreightNotQuoted, report.ReasonCode);
    }

    [Fact]
    public async Task Authority_side_measurement_requirement_is_reported_as_a_measurement_gap()
    {
        // مرجع جدید خودش می‌گوید اندازه لازم است: این «عدم تطابق پله‌ای ساختگی» نیست، یک شکاف
        // اندازه است و باید صریح گزارش شود.
        var lookup = new RecordingOperationalLookup(
            _ => NotQuoted(
                ShippingOperationalQuoteOutcome.FreightNotQuoted,
                ShippingOperationalQuoteReasonCodes.FreightNotQuoted,
                ShippingReasonCodesKind.MeasurementRequired));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(grams: 1999, count: null), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.MeasurementUnavailable, report.Outcome);
    }

    [Fact]
    public async Task Ambiguous_packaging_is_reported_without_any_amount()
    {
        var lookup = new RecordingOperationalLookup(
            _ => NotQuoted(
                ShippingOperationalQuoteOutcome.PackagingAmbiguous,
                ShippingOperationalQuoteReasonCodes.PackagingAmbiguous));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.LegacyAllowedShippingDenied, report.Outcome);
        Assert.Equal(ShippingOperationalQuoteReasonCodes.PackagingAmbiguous, report.ReasonCode);
        Assert.Null(report.PackagingAmount);
    }

    // ---------- schedule and capacity honesty ----------

    [Fact]
    public async Task Carrier_window_is_observed_as_carrier_availability_not_a_customer_promise()
    {
        var lookup = new RecordingOperationalLookup(
            _ => Quoted(180_000m, Packaging(false, 0m), Schedule(ShippingScheduleAvailability.Unavailable)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        // خارج از پنجرهٔ حامل: کرایه هنوز قابل مقایسه است، اما هیچ «وعدهٔ تحویل» یا «عدم امکان»
        // برای مشتری ساخته نمی‌شود؛ فقط یک واقعیت عملیاتی گزارش می‌شود.
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.Match, report.Outcome);
        Assert.Equal("Unavailable", report.ScheduleAvailability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.OutsideWindows, report.ScheduleReasonCode);
    }

    [Fact]
    public async Task Configured_capacity_is_never_presented_as_enforced_or_remaining()
    {
        var lookup = new RecordingOperationalLookup(
            _ => Quoted(180_000m, Packaging(false, 0m), Schedule(capacity: 25)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(25, report.ConfiguredCapacityTotal);
        Assert.Equal("NotEnforced", report.CapacityEnforcement);
    }

    [Fact]
    public async Task Not_evaluated_schedule_is_disclosed_and_never_treated_as_available()
    {
        var lookup = new RecordingOperationalLookup(
            _ => Quoted(180_000m, Packaging(false, 0m), Schedule(ShippingScheduleAvailability.NotEvaluated)));
        var observer = Observer(new FakeGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal("NotEvaluated", report.ScheduleAvailability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.NotEvaluated, report.ScheduleReasonCode);
    }

    // ---------- failure containment ----------

    [Fact]
    public async Task Throwing_authority_degrades_per_candidate_and_never_escapes()
    {
        var observer = Observer(new FakeGeography(), new ThrowingOperationalLookup(), Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.Observed, result.Code);
        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingOperationalShadowOutcome.ShippingUnavailable, report.Outcome);
        Assert.Equal(180_000m, report.LegacyPriceAmount);
        Assert.Null(report.LegacyFreightAmount);
    }

    [Fact]
    public async Task Uncomposed_read_seams_are_shadow_unavailable_without_any_query()
    {
        var observer = Observer(geography: null, lookup: null, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(parcel: Parcel(), provenance: "packing"),
            CancellationToken.None);

        Assert.Equal(StorefrontShippingOperationalShadowCode.ShadowUnavailable, result.Code);
    }
}

/// <summary>
/// گاردهای متنی ترکیب: دروازهٔ عملیاتی مستقل و پیش‌فرض‌خاموش است، مسیر واقعی projection فقط مشاهده
/// می‌کند، ریشهٔ ترکیب Order همان درزها را یک‌بار ثبت می‌کند و هیچ مسیری از گزارش به قیمت/وعده/پیش‌نویس
/// وجود ندارد.
/// </summary>
public sealed class StorefrontShippingOperationalShadowCompositionGuardTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string Read(params string[] parts)
        => File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));

    [Fact]
    public void Operational_gate_defaults_stay_off_and_are_separate_from_the_simple_rate_gate()
    {
        var options = Read(
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow",
            "StorefrontShippingOperationalShadowOptions.cs");

        Assert.Contains("Tooba:ShippingOperationalShadow", options, StringComparison.Ordinal);
        Assert.Contains("public bool Enabled { get; set; }", options, StringComparison.Ordinal);
        Assert.Contains("public bool PrerequisitesConfirmed { get; set; }", options, StringComparison.Ordinal);
        Assert.DoesNotContain("Enabled { get; set; } = true", options, StringComparison.Ordinal);
        Assert.DoesNotContain("PrerequisitesConfirmed { get; set; } = true", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Real_projection_query_observes_both_shadows_and_returns_the_untouched_result()
    {
        var handler = Read(
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/Queries",
            "ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs");

        Assert.Contains("shadow.ObserveAsync(", handler, StringComparison.Ordinal);
        Assert.Contains("operationalShadow.ObserveAsync(", handler, StringComparison.Ordinal);
        Assert.Contains("return result;", handler, StringComparison.Ordinal);
        // هیچ مسیری از گزارش سایه به انتخاب/مبلغ/تاریخ نمی‌رود.
        Assert.DoesNotContain("SelectedShippingAmount =", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ShippingAmount =", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("MinimumDeliveryDate =", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_module_registers_the_operational_shadow_seams_once()
    {
        var module = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs");

        Assert.Single(Regex.Matches(
            module,
            @"new Application\.Storefront\.Shipping\.OperationalShadow\.StorefrontShippingOperationalShadowObserver\("));
        Assert.Contains(
            "Tooba.Shipping.Contracts.Operations.IShippingOperationalQuoteLookup",
            module,
            StringComparison.Ordinal);
        Assert.Contains(
            "StorefrontShippingOperationalShadowOptions.SectionName",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_shadow_report_has_no_path_to_price_draft_or_snapshot_authority()
    {
        var dir = Path.Combine(
            RepoRoot(),
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow");

        foreach (var file in new[]
                 {
                     "StorefrontShippingOperationalShadowComparer.cs",
                     "StorefrontShippingOperationalShadowContracts.cs",
                     "StorefrontShippingOperationalShadowObserver.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(dir, file));
            Assert.DoesNotContain("CartShippingDraft", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CheckoutSnapshot", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SelectedShippingAmount", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontShippingMethodView", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Operational_shadow_never_manufactures_a_weight_from_cart_facts()
    {
        var dir = Path.Combine(
            RepoRoot(),
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow");

        foreach (var file in Directory.GetFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("ItemCount =", text, StringComparison.Ordinal);
            Assert.DoesNotContain("WeightGrams =", text, StringComparison.Ordinal);
        }
    }
}
