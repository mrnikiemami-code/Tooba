using Microsoft.Extensions.Options;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Shipping.OwnedRate;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// آزمون‌های متمرکز دروازهٔ نرخ مشتریِ مالک‌شدهٔ Shipping
/// (TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163): خاموش = صفر فراخوانی و رفتار قدیمی، روشن =
/// فقط نرخ گزینهٔ دقیقِ مالک، و هر کمبودی fail-closed با دلیل پایدار (نه صفر، نه رایگان، نه fallback).
/// </summary>
public sealed class StorefrontShippingOwnedRateResolverTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-4222-8222-222222222222");

    // ---------- fakes ----------

    private sealed class FakeGeography : IGeographyLookup
    {
        private readonly DestinationResolution _resolution;

        public FakeGeography(DestinationResolution resolution) => _resolution = resolution;

        public int ResolveCalls { get; private set; }

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
        {
            ResolveCalls++;
            return Task.FromResult(_resolution);
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

    private sealed class RecordingLookup(Func<ShippingQuoteRequest, ShippingQuote> factory) : IShippingRateQuoteLookup
    {
        public int Calls { get; private set; }

        public List<ShippingQuoteRequest> Requests { get; } = [];

        public Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            Requests.Add(request);
            return Task.FromResult(factory(request));
        }
    }

    // ---------- helpers ----------

    private static StorefrontShippingOwnedRateResolver Resolver(
        IShippingRateQuoteLookup? lookup,
        IGeographyLookup? geography,
        bool enabled)
        => new(lookup, geography, Options.Create(new StorefrontShippingOwnedRateOptions { Enabled = enabled }));

    private static FakeGeography ResolvedGeography()
        => new(DestinationResolution.Resolved(new DestinationIdentifiers(CountryId, AreaId)));

    private static StorefrontShippingOwnedRateCandidate Standard() => new("tipax:standard", "tipax", "standard");

    private static StorefrontShippingOwnedRateCandidate Express() => new("tipax:express", "tipax", "express");

    private static ShippingQuote Quoted(
        ShippingQuoteRequest request,
        decimal amount,
        string currency = "IRR")
        => new(
            ShippingServiceabilityOutcome.Quoted,
            request.ServiceCode,
            amount,
            currency,
            1,
            2,
            null,
            ShippingReasonCodes.Quoted,
            request.OptionCode);

    private static ShippingQuote Rejected(ShippingQuoteRequest request, string reasonCode)
        => new(
            ShippingServiceabilityOutcome.NotCovered,
            request.ServiceCode,
            0m,
            null,
            0,
            0,
            null,
            reasonCode,
            request.OptionCode);

    private static Task<StorefrontShippingOwnedRateResolution> ResolveAsync(
        StorefrontShippingOwnedRateResolver resolver,
        params StorefrontShippingOwnedRateCandidate[] candidates)
        => resolver.ResolveAsync(
            candidates.Length == 0 ? [Standard(), Express()] : candidates,
            "tehran",
            "tehran",
            destination: null,
            cartSubtotalExclusive: 500_000m,
            currencyCode: "IRR",
            CancellationToken.None);

    // ---------- tests ----------

    /// <summary>خاموش: هیچ کوئری/حل مقصدی زده نمی‌شود و نتیجه «اجرا نشد» است (پاریتهٔ کامل مسیر قدیمی).</summary>
    [Fact]
    public async Task Gate_off_performs_zero_owner_calls_and_keeps_legacy_path()
    {
        var lookup = new RecordingLookup(_ => throw new InvalidOperationException("must not be called"));
        var geography = ResolvedGeography();
        var resolver = Resolver(lookup, geography, enabled: false);

        var resolution = await ResolveAsync(resolver);

        Assert.False(resolution.Evaluated);
        Assert.Empty(resolution.Quoted);
        Assert.Empty(resolution.Unavailable);
        Assert.Equal(0, lookup.Calls);
        Assert.Equal(0, geography.ResolveCalls);
        Assert.False(resolver.IsEnabled);
    }

    /// <summary>روشن: هر روش فقط نرخ ردیف گزینهٔ دقیق خودش را می‌گیرد؛ گزینهٔ هم‌خانواده ارث نمی‌برد.</summary>
    [Fact]
    public async Task Gate_on_prices_each_exact_option_and_never_inherits_sibling_option()
    {
        var lookup = new RecordingLookup(request => request.OptionCode switch
        {
            "standard" => Quoted(request, 205_000m),
            "express" => Quoted(request, 480_000m),
            _ => Rejected(request, ShippingReasonCodes.OwnedOptionNotPriced),
        });
        var resolver = Resolver(lookup, ResolvedGeography(), enabled: true);

        var resolution = await ResolveAsync(resolver);

        Assert.True(resolution.Evaluated);
        Assert.Equal(205_000m, resolution.Quoted["tipax:standard"].Amount);
        Assert.Equal(480_000m, resolution.Quoted["tipax:express"].Amount);
        Assert.Empty(resolution.Unavailable);
        Assert.Equal(2, lookup.Calls);
        Assert.All(lookup.Requests, request =>
        {
            Assert.Equal(ShippingOptionMatchMode.StrictOwnedOption, request.OptionMatchMode);
            // اندازهٔ فیزیکی هرگز از متن مشتری/سبد ساخته نمی‌شود.
            Assert.Null(request.Parcel);
        });
    }

    /// <summary>ردیف سطح‌خدمت (فقط shared) قیمت نمی‌دهد و دلیل مالک عیناً حفظ می‌شود.</summary>
    [Fact]
    public async Task Legacy_shared_only_row_is_unquotable_with_stable_reason()
    {
        var lookup = new RecordingLookup(request => Rejected(request, ShippingReasonCodes.OwnedOptionNotPriced));
        var resolver = Resolver(lookup, ResolvedGeography(), enabled: true);

        var resolution = await ResolveAsync(resolver, Standard());

        Assert.Empty(resolution.Quoted);
        var unavailable = Assert.Single(resolution.Unavailable);
        Assert.Equal("tipax:standard", unavailable.MethodCode);
        Assert.Equal(ShippingReasonCodes.OwnedOptionNotPriced, unavailable.ReasonCode);
    }

    /// <summary>پوشش Restricted رد می‌کند: روشن‌بودن دروازه هرگز پوشش را «مجاز» نمی‌کند.</summary>
    [Fact]
    public async Task Restricted_coverage_denial_fails_closed()
    {
        var lookup = new RecordingLookup(request => Rejected(request, ShippingReasonCodes.CoverageMethodRestricted));
        var resolver = Resolver(lookup, ResolvedGeography(), enabled: true);

        var resolution = await ResolveAsync(resolver, Standard());

        Assert.Empty(resolution.Quoted);
        Assert.Equal(
            ShippingReasonCodes.CoverageMethodRestricted,
            Assert.Single(resolution.Unavailable).ReasonCode);
    }

    /// <summary>قاعدهٔ شرط‌دار بدون اندازهٔ مورداعتماد: رد صریح، بدون پارسل ساختگی و بدون مبلغ.</summary>
    [Fact]
    public async Task Measurement_required_fails_closed_without_fabricating_parcel()
    {
        var lookup = new RecordingLookup(request => Rejected(request, ShippingReasonCodes.MeasurementRequired));
        var resolver = Resolver(lookup, ResolvedGeography(), enabled: true);

        var resolution = await ResolveAsync(resolver, Standard());

        Assert.Empty(resolution.Quoted);
        Assert.Equal(ShippingReasonCodes.MeasurementRequired, Assert.Single(resolution.Unavailable).ReasonCode);
        Assert.Null(Assert.Single(lookup.Requests).Parcel);
    }

    /// <summary>ارز ناهم‌خوان با سبد پذیرفته نمی‌شود.</summary>
    [Fact]
    public async Task Currency_mismatch_is_rejected()
    {
        var lookup = new RecordingLookup(request => Quoted(request, 205_000m, currency: "USD"));
        var resolver = Resolver(lookup, ResolvedGeography(), enabled: true);

        var resolution = await ResolveAsync(resolver, Standard());

        Assert.Empty(resolution.Quoted);
        Assert.Equal(
            StorefrontShippingOwnedRateReasons.CurrencyMismatch,
            Assert.Single(resolution.Unavailable).ReasonCode);
    }

    /// <summary>مقصد حل‌نشده: هیچ حدسی ساخته نمی‌شود و هیچ کوئری نرخی زده نمی‌شود.</summary>
    [Fact]
    public async Task Unresolved_destination_fails_closed_without_any_quote()
    {
        var lookup = new RecordingLookup(_ => throw new InvalidOperationException("must not be called"));
        var geography = new FakeGeography(DestinationResolution.Unresolved());
        var resolver = Resolver(lookup, geography, enabled: true);

        var resolution = await ResolveAsync(resolver);

        Assert.True(resolution.Evaluated);
        Assert.Empty(resolution.Quoted);
        Assert.Equal(2, resolution.Unavailable.Count);
        Assert.All(
            resolution.Unavailable,
            u => Assert.Equal(StorefrontShippingOwnedRateReasons.DestinationUnresolved, u.ReasonCode));
        Assert.Equal(0, lookup.Calls);
    }

    /// <summary>خطای مرجع/نبود درز هرگز به صفر یا رایگان تبدیل نمی‌شود.</summary>
    [Fact]
    public async Task Owner_failure_is_fail_closed_and_never_free()
    {
        var throwing = Resolver(null, ResolvedGeography(), enabled: true);
        var fromNull = await ResolveAsync(throwing, Standard());
        Assert.Empty(fromNull.Quoted);
        Assert.Equal(
            StorefrontShippingOwnedRateReasons.OwnerUnavailable,
            Assert.Single(fromNull.Unavailable).ReasonCode);

        var raising = new RecordingLookup(_ => throw new InvalidOperationException("relation missing"));
        var fromError = await ResolveAsync(Resolver(raising, ResolvedGeography(), enabled: true), Standard());
        Assert.Empty(fromError.Quoted);
        Assert.Equal(
            StorefrontShippingOwnedRateReasons.QuoteFailed,
            Assert.Single(fromError.Unavailable).ReasonCode);
    }

    /// <summary>هویت گزینهٔ ناقص: تطبیق سخت‌گیرانه معنا ندارد و هیچ کوئری‌ای زده نمی‌شود.</summary>
    [Fact]
    public async Task Missing_service_identity_is_rejected_before_any_quote()
    {
        var lookup = new RecordingLookup(_ => throw new InvalidOperationException("must not be called"));
        var resolver = Resolver(lookup, ResolvedGeography(), enabled: true);

        var resolution = await ResolveAsync(resolver, new StorefrontShippingOwnedRateCandidate("tipax:standard", "", "standard"));

        Assert.Empty(resolution.Quoted);
        Assert.Equal(
            StorefrontShippingOwnedRateReasons.MethodIdentityIncomplete,
            Assert.Single(resolution.Unavailable).ReasonCode);
        Assert.Equal(0, lookup.Calls);
    }

    /// <summary>هر دو روش یک سرویس مستقل قیمت‌گذاری می‌شوند: رد یکی، پذیرش دیگری را خراب نمی‌کند.</summary>
    [Fact]
    public async Task One_rejected_option_does_not_poison_the_quoted_sibling()
    {
        var lookup = new RecordingLookup(request => request.OptionCode == "standard"
            ? Quoted(request, 205_000m)
            : Rejected(request, ShippingReasonCodes.OwnedOptionNotPriced));
        var resolver = Resolver(lookup, ResolvedGeography(), enabled: true);

        var resolution = await ResolveAsync(resolver);

        Assert.Equal(205_000m, resolution.Quoted["tipax:standard"].Amount);
        Assert.DoesNotContain("tipax:express", resolution.Quoted.Keys);
        Assert.Equal("tipax:express", Assert.Single(resolution.Unavailable).MethodCode);
    }
}
