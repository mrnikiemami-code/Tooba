using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.RateAuthority;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// آزمون‌های متمرکز ناظر سایه روی مسیر واقعی projection ارسال: دروازهٔ خاموش هیچ پرس‌وجویی نمی‌زند،
/// مقصد مبهم/نامنگاشت حدس زده نمی‌شود، نبود پوشش هرگز «مجاز» نمی‌شود و خطای مرجع جدید مسیر فروشگاه را
/// نمی‌شکند. هیچ‌کدام از این نتایج قیمت، promise یا snapshot را تغییر نمی‌دهد.
/// </summary>
public sealed class StorefrontShippingShadowObserverTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    // ---------- fakes ----------

    private sealed class FakeGeography : IGeographyLookup
    {
        private readonly Func<string?, DestinationResolution> _resolution;

        public FakeGeography(Func<string?, DestinationResolution> resolution) => _resolution = resolution;

        public int ResolveCalls { get; private set; }

        public string? LastCountryCode { get; private set; }

        public string? LastAreaName { get; private set; }

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
        {
            ResolveCalls++;
            LastCountryCode = countryCode;
            LastAreaName = administrativeAreaName;
            return Task.FromResult(_resolution(administrativeAreaName));
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

    private sealed class ThrowingGeography : IGeographyLookup
    {
        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("relation \"geography.countries\" does not exist");

        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();
    }

    private sealed class CancellingGeography : IGeographyLookup
    {
        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
            => throw new OperationCanceledException(cancellationToken);

        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken)
            => throw new OperationCanceledException(cancellationToken);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken)
            => throw new OperationCanceledException(cancellationToken);

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken)
            => throw new OperationCanceledException(cancellationToken);

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken)
            => throw new OperationCanceledException(cancellationToken);

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken)
            => throw new OperationCanceledException(cancellationToken);
    }

    private sealed class RecordingQuoteLookup : IShippingRateQuoteLookup
    {
        private readonly Func<ShippingQuoteRequest, ShippingQuote> _factory;

        public RecordingQuoteLookup(Func<ShippingQuoteRequest, ShippingQuote> factory) => _factory = factory;

        public int Calls { get; private set; }

        public ShippingQuoteRequest? LastRequest { get; private set; }

        public Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(_factory(request));
        }
    }

    private sealed class ThrowingQuoteLookup : IShippingRateQuoteLookup
    {
        public Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("relation \"shipping.rate_policies\" does not exist");
    }

    // ---------- helpers ----------

    private static DestinationResolution ResolvedIranArea()
        => DestinationResolution.Resolved(new DestinationIdentifiers(CountryId, AreaId));

    private static StorefrontShippingShadowObservationInput Input(
        string? province = "tehran",
        decimal subtotal = 500_000m,
        string? currency = "IRR",
        params StorefrontShippingShadowCandidate[] candidates)
        => new(
            province,
            subtotal,
            currency,
            candidates.Length == 0
                ? [new StorefrontShippingShadowCandidate("post", 150_000m)]
                : candidates);

    private static StorefrontShippingRateAuthorityOptions Gate(bool enabled, bool confirmed) => new()
    {
        ShadowEnabled = enabled,
        PrerequisitesConfirmed = confirmed,
    };

    private static StorefrontShippingShadowObserver Observer(
        IGeographyLookup? geography,
        IShippingRateQuoteLookup? lookup,
        StorefrontShippingRateAuthorityOptions options)
        => new(
            lookup is null ? null : new StorefrontShippingRateShadowEvaluator(lookup),
            geography,
            Options.Create(options),
            NullLogger<StorefrontShippingShadowObserver>.Instance);

    private static ShippingQuote Quote(
        ShippingServiceabilityOutcome outcome,
        decimal amount,
        string reason,
        int leadDays = 2)
        => new(outcome, "post", amount, "IRR", 1, leadDays, null, reason);

    // ---------- gate ----------

    [Fact]
    public async Task Shadow_off_by_default_performs_no_geography_and_no_shipping_lookup()
    {
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(geography, lookup, new StorefrontShippingRateAuthorityOptions());

        var result = await observer.ObserveAsync(Input(), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.Disabled, result.Code);
        Assert.Empty(result.Reports);
        Assert.Equal(0, geography.ResolveCalls);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Shadow_enabled_without_confirmed_prerequisites_performs_no_lookup()
    {
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: false));

        var result = await observer.ObserveAsync(Input(), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.PrerequisitesUnconfirmed, result.Code);
        Assert.Equal(0, geography.ResolveCalls);
        Assert.Equal(0, lookup.Calls);
    }

    // ---------- destination resolution ----------

    [Fact]
    public async Task Legacy_province_is_forwarded_verbatim_with_the_legacy_country_key()
    {
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        await observer.ObserveAsync(Input(province: "tehran"), CancellationToken.None);

        Assert.Equal(1, geography.ResolveCalls);
        Assert.Equal(StorefrontShippingShadowObserver.LegacyCountryCode, geography.LastCountryCode);
        Assert.Equal("tehran", geography.LastAreaName);
    }

    [Fact]
    public async Task Ambiguous_legacy_destination_is_not_comparable_and_is_never_guessed()
    {
        var geography = new FakeGeography(_ => DestinationResolution.Ambiguous(3));
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(province: "تهران"), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.DestinationNotComparable, result.Code);
        Assert.Empty(result.Reports);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Unresolved_legacy_label_is_not_comparable_and_is_never_guessed()
    {
        var geography = new FakeGeography(_ => DestinationResolution.Unresolved());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(province: "shiraz-free-text"), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.DestinationNotComparable, result.Code);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Absent_province_is_not_comparable_without_any_guess()
    {
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(province: null), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.DestinationNotComparable, result.Code);
        Assert.Equal(0, geography.ResolveCalls);
        Assert.Equal(0, lookup.Calls);
    }

    // ---------- observation outcomes ----------

    [Fact]
    public async Task Missing_coverage_is_reported_and_never_becomes_allowed_or_free()
    {
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.NotCovered, 0m, ShippingReasonCodes.CoverageMissing));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.Observed, result.Code);
        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingRateShadowOutcome.LegacyAllowedShippingDenied, report.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, report.ShippingReasonCode);
        // مبلغ authoritative همان ۱۵۰٬۰۰۰ مسیر قدیمی است؛ صفر شدن مرجع جدید جای آن را نمی‌گیرد.
        Assert.Equal(150_000m, report.LegacyPriceAmount);
    }

    [Fact]
    public async Task Amount_mismatch_and_currency_mismatch_are_observed_without_touching_legacy_price_or_promise()
    {
        var legacyOptions = new ShippingMethodsOptions
        {
            DefaultSellerPreparationDays = 1,
            DeliveryHorizonDays = 5,
            Rates = [new ShippingMethodRateOptions { Code = "post", BasePrice = 150_000m, LeadDays = 9 }],
        };
        var legacyRate = StorefrontShippingCalculator.ResolveRate("post", legacyOptions);
        var legacyPrice = StorefrontShippingCalculator.QuotePrice(legacyRate, 500_000m);
        var legacyMinimum = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(
            new DateOnly(2026, 10, 14),
            StorefrontShippingCalculator.MaxSellerPreparationDays([Guid.NewGuid()], legacyOptions),
            legacyRate.LeadDays);
        Assert.Equal(150_000m, legacyPrice);

        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 999_999m, ShippingReasonCodes.Quoted, leadDays: 2));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(subtotal: 500_000m), CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingRateShadowOutcome.AmountMismatch, report.Outcome);
        Assert.Equal(999_999m, report.ShippingChargeAmount); // فقط مشاهده‌ای

        // promise و قیمت مسیر قدیمی دقیقاً همان مقدار قبل از مشاهده است.
        Assert.Equal(legacyPrice, report.LegacyPriceAmount);
        Assert.Equal(
            legacyMinimum,
            StorefrontShippingCalculator.ComputeMinimumDeliveryDate(
                new DateOnly(2026, 10, 14),
                StorefrontShippingCalculator.MaxSellerPreparationDays([Guid.NewGuid()], legacyOptions),
                legacyRate.LeadDays));
    }

    [Fact]
    public async Task Free_shipping_threshold_case_stays_an_observation()
    {
        // توجه: مرجع جدید در این آزمون یک test double است؛ هیچ نرخ/مجوز production ساخته نمی‌شود و
        // «تطابق» مشاهده‌شده در آزمون هیچ‌گاه اجازهٔ cutover نمی‌دهد (مجوزهای واقعی پوشش خالی‌اند).
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(
                ShippingServiceabilityOutcome.Quoted,
                0m,
                ShippingReasonCodes.QuotedFreeAboveThreshold));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(
            Input(subtotal: 5_000_000m, candidates: new StorefrontShippingShadowCandidate("post", 0m)),
            CancellationToken.None);

        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingRateShadowOutcome.Match, report.Outcome);
        Assert.Equal(0m, report.LegacyPriceAmount);
    }

    [Fact]
    public async Task Service_code_cart_facts_and_currency_are_forwarded_untouched()
    {
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(geography, lookup, Gate(enabled: true, confirmed: true));

        await observer.ObserveAsync(
            Input(subtotal: 777_000m, currency: "IRT", candidates: new StorefrontShippingShadowCandidate("post:standard", 150_000m)),
            CancellationToken.None);

        Assert.Equal(1, lookup.Calls);
        Assert.Equal("post:standard", lookup.LastRequest!.ServiceCode);
        Assert.Equal(777_000m, lookup.LastRequest.CartSubtotalExclusive);
        Assert.Equal("IRT", lookup.LastRequest.CurrencyCode);
        Assert.Equal(CountryId, lookup.LastRequest.Destination.CountryId);
        Assert.Equal(AreaId, lookup.LastRequest.Destination.AdministrativeAreaId);
    }

    // ---------- failure containment ----------

    [Fact]
    public async Task Throwing_geography_degrades_to_shadow_unavailable_and_never_escapes()
    {
        var lookup = new RecordingQuoteLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var observer = Observer(new ThrowingGeography(), lookup, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.ShadowUnavailable, result.Code);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Missing_shipping_authority_degrades_per_candidate_without_escaping()
    {
        var geography = new FakeGeography(_ => ResolvedIranArea());
        var observer = Observer(geography, new ThrowingQuoteLookup(), Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.Observed, result.Code);
        var report = Assert.Single(result.Reports);
        Assert.Equal(StorefrontShippingRateShadowOutcome.ShippingUnavailable, report.Outcome);
        Assert.Equal(150_000m, report.LegacyPriceAmount);
        Assert.Null(report.ShippingChargeAmount);
    }

    [Fact]
    public async Task Uncomposed_read_seams_are_shadow_unavailable_without_any_query()
    {
        var observer = Observer(geography: null, lookup: null, Gate(enabled: true, confirmed: true));

        var result = await observer.ObserveAsync(Input(), CancellationToken.None);

        Assert.Equal(StorefrontShippingShadowObservationCode.ShadowUnavailable, result.Code);
    }

    [Fact]
    public async Task Cancellation_semantics_are_preserved_and_not_swallowed()
    {
        var observer = Observer(
            new CancellingGeography(),
            new RecordingQuoteLookup(_ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted)),
            Gate(enabled: true, confirmed: true));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => observer.ObserveAsync(Input(), cts.Token));
    }
}

/// <summary>
/// گاردهای متنی ترکیب: دروازه پیش‌فرض خاموش است، مسیر واقعی projection فقط مشاهده می‌کند و
/// ریشهٔ ترکیب Order همان درزها را یک‌بار ثبت می‌کند.
/// </summary>
public sealed class StorefrontShippingShadowCompositionGuardTests
{
    [Fact]
    public void Gate_option_defaults_stay_off_and_the_authority_is_still_disabled()
    {
        var options = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority",
            "StorefrontShippingRateAuthorityOptions.cs"));

        Assert.Contains("public bool ShadowEnabled { get; set; }", options, StringComparison.Ordinal);
        Assert.Contains("public bool PrerequisitesConfirmed { get; set; }", options, StringComparison.Ordinal);
        Assert.DoesNotContain("ShadowEnabled { get; set; } = true", options, StringComparison.Ordinal);
        Assert.DoesNotContain("PrerequisitesConfirmed { get; set; } = true", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Real_projection_query_observes_the_shadow_and_returns_the_untouched_result()
    {
        var handler = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/Queries",
            "ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs"));

        Assert.Contains("shadow.ObserveAsync(", handler, StringComparison.Ordinal);
        Assert.Contains("if (result.IsSuccess && result.Value is { } projection)", handler, StringComparison.Ordinal);
        Assert.Contains("return result;", handler, StringComparison.Ordinal);
        // هیچ مسیری از گزارش سایه به انتخاب/مبلغ/تاریخ نمی‌رود.
        Assert.DoesNotContain("SelectedShippingAmount =", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ShippingAmount", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_module_registers_the_shadow_seams_once_from_existing_composition()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs"));

        Assert.Single(Regex.Matches(module, @"new Application\.Storefront\.Shipping\.RateAuthority\.StorefrontShippingShadowObserver\("));
        Assert.Contains("StorefrontShippingRateShadowEvaluator", module, StringComparison.Ordinal);
        Assert.Contains("StorefrontShippingRateAuthorityOptions.SectionName", module, StringComparison.Ordinal);
        Assert.Contains("Tooba.Shipping.Contracts.Rates.IShippingRateQuoteLookup", module, StringComparison.Ordinal);
        Assert.Contains("Tooba.Geography.Contracts.Places.IGeographyLookup", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Shadow_report_has_no_path_to_price_draft_or_snapshot_authority()
    {
        var rateAuthority = Path.Combine(
            RepoRoot(),
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority");

        foreach (var file in new[]
                 {
                     "StorefrontShippingRateShadowEvaluator.cs",
                     "StorefrontShippingRateShadowComparer.cs",
                     "StorefrontShippingRateShadowContracts.cs",
                     "StorefrontShippingShadowObserver.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(rateAuthority, file));
            Assert.DoesNotContain("CartShippingDraft", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CheckoutSnapshot", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ShippingAmount", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontShippingMethodView", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SelectedShippingAmount", text, StringComparison.Ordinal);
        }
    }

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
}
