using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.RateAuthority;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// آزمون‌های متمرکز درز سایهٔ مرجع نرخ ارسال: مرجع قدیمی دست‌نخورده می‌ماند، عدم پوشش بی‌سروصدا
/// به «مجاز» تبدیل نمی‌شود و نبود داده/جدول ارسال مسیر زنده را نمی‌شکند.
/// </summary>
public sealed class StorefrontShippingRateShadowTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid ProvinceId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    private static DestinationIdentifiers Destination() => new(CountryId, ProvinceId, CityId);

    private sealed class RecordingLookup : IShippingRateQuoteLookup
    {
        private readonly Func<ShippingQuoteRequest, ShippingQuote> _factory;

        public RecordingLookup(Func<ShippingQuoteRequest, ShippingQuote> factory) => _factory = factory;

        public int Calls { get; private set; }

        public ShippingQuoteRequest? LastRequest { get; private set; }

        public Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(_factory(request));
        }
    }

    private sealed class ThrowingLookup : IShippingRateQuoteLookup
    {
        public Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("relation \"shipping.rate_policies\" does not exist");
    }

    private static ShippingQuote Quote(
        ShippingServiceabilityOutcome outcome,
        decimal amount,
        string reason) => new(outcome, "post", amount, "IRR", 1, 2, null, reason);

    private static StorefrontShippingRateShadowInput Input(
        decimal legacyPrice = 150_000m,
        bool legacyAllowed = true,
        DestinationIdentifiers? destination = null) => new(
            "post",
            legacyPrice,
            legacyAllowed,
            destination ?? Destination(),
            500_000m,
            "IRR");

    private static StorefrontShippingRateAuthorityOptions Enabled(bool prerequisitesConfirmed) => new()
    {
        ShadowEnabled = true,
        PrerequisitesConfirmed = prerequisitesConfirmed,
    };

    private static ShippingMethodsOptions LegacyOptions() => new()
    {
        DefaultSellerPreparationDays = 1,
        DeliveryHorizonDays = 5,
        Rates =
        [
            new ShippingMethodRateOptions
            {
                Code = "post",
                BasePrice = 150_000m,
                LeadDays = 2,
            },
        ],
    };

    [Fact]
    public async Task Gate_Is_Disabled_By_Default_And_Legacy_Price_Is_Not_Touched()
    {
        var lookup = new RecordingLookup(_ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var evaluator = new StorefrontShippingRateShadowEvaluator(lookup);

        var report = await evaluator.EvaluateAsync(Input(), new StorefrontShippingRateAuthorityOptions(), default);

        Assert.Equal(StorefrontShippingRateAuthorityGate.Disabled, report.Gate);
        Assert.Equal(StorefrontShippingRateShadowOutcome.NotEvaluated, report.Outcome);
        Assert.Equal(150_000m, report.LegacyPriceAmount);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Enabled_Shadow_Without_Confirmed_Prerequisites_Still_Does_Not_Query_Shipping()
    {
        var lookup = new RecordingLookup(_ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var evaluator = new StorefrontShippingRateShadowEvaluator(lookup);

        var report = await evaluator.EvaluateAsync(Input(), Enabled(prerequisitesConfirmed: false), default);

        Assert.Equal(StorefrontShippingRateAuthorityGate.PrerequisitesUnconfirmed, report.Gate);
        Assert.Equal(StorefrontShippingRateShadowOutcome.NotEvaluated, report.Outcome);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Uncovered_Destination_Is_Reported_Not_Treated_As_Unrestricted_Or_Allowed()
    {
        var lookup = new RecordingLookup(
            _ => Quote(ShippingServiceabilityOutcome.NotCovered, 0m, ShippingReasonCodes.CoverageMissing));
        var evaluator = new StorefrontShippingRateShadowEvaluator(lookup);

        var report = await evaluator.EvaluateAsync(Input(), Enabled(prerequisitesConfirmed: true), default);

        Assert.Equal(StorefrontShippingRateAuthorityGate.Allowed, report.Gate);
        Assert.Equal(StorefrontShippingRateShadowOutcome.LegacyAllowedShippingDenied, report.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, report.ShippingReasonCode);
        // قیمت authoritative همان مقدار مسیر قدیمی است؛ «صفر» مرجع جدید جای آن را نمی‌گیرد.
        Assert.Equal(150_000m, report.LegacyPriceAmount);
    }

    [Fact]
    public async Task Missing_Destination_Identifiers_Are_Not_Comparable_And_Never_Guessed_From_Text()
    {
        var lookup = new RecordingLookup(_ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var evaluator = new StorefrontShippingRateShadowEvaluator(lookup);
        var input = new StorefrontShippingRateShadowInput(
            "post",
            150_000m,
            true,
            Destination: null,
            500_000m,
            "IRR");

        var report = await evaluator.EvaluateAsync(input, Enabled(prerequisitesConfirmed: true), default);

        Assert.Equal(StorefrontShippingRateShadowOutcome.NotComparable, report.Outcome);
        Assert.Equal(0, lookup.Calls);
    }

    [Fact]
    public async Task Shipping_Failure_Or_Absent_Tables_Degrade_To_Unavailable_And_Never_Throw()
    {
        var evaluator = new StorefrontShippingRateShadowEvaluator(new ThrowingLookup());

        var report = await evaluator.EvaluateAsync(Input(), Enabled(prerequisitesConfirmed: true), default);

        Assert.Equal(StorefrontShippingRateShadowOutcome.ShippingUnavailable, report.Outcome);
        Assert.Equal(150_000m, report.LegacyPriceAmount);
        Assert.Null(report.ShippingChargeAmount);
    }

    [Fact]
    public async Task Unconfigured_Lookup_Is_Unavailable_Without_Any_Query()
    {
        var evaluator = new StorefrontShippingRateShadowEvaluator(lookup: null);

        var report = await evaluator.EvaluateAsync(Input(), Enabled(prerequisitesConfirmed: true), default);

        Assert.Equal(StorefrontShippingRateShadowOutcome.NotComparable, report.Outcome);
    }

    [Fact]
    public async Task Shipping_Quote_Is_Observation_Only_And_Committed_Amount_Stays_Legacy()
    {
        // مسیر قدیمی: post با قیمت ۱۵۰٬۰۰۰ برای سبد ۵۰۰٬۰۰۰ (معافیت فعال نیست).
        var options = LegacyOptions();
        var legacyRate = StorefrontShippingCalculator.ResolveRate("post", options);
        var legacyPrice = StorefrontShippingCalculator.QuotePrice(legacyRate, 500_000m);
        Assert.Equal(150_000m, legacyPrice);

        // مرجع جدید عددی کاملاً متفاوت اعلام می‌کند؛ این عدد هرگز نباید به مشتری برسد.
        var lookup = new RecordingLookup(
            _ => Quote(ShippingServiceabilityOutcome.Quoted, 999_999m, ShippingReasonCodes.Quoted));
        var evaluator = new StorefrontShippingRateShadowEvaluator(lookup);

        var report = await evaluator.EvaluateAsync(
            Input(legacyPrice: legacyPrice),
            Enabled(prerequisitesConfirmed: true),
            default);

        Assert.Equal(StorefrontShippingRateShadowOutcome.AmountMismatch, report.Outcome);
        Assert.Equal(999_999m, report.ShippingChargeAmount); // فقط مشاهده‌ای

        // مقدار authoritative و promise حداقل تحویل هر دو از مسیر قدیمی می‌آیند و تغییر نکرده‌اند.
        var reResolvedPrice = StorefrontShippingCalculator.QuotePrice(
            StorefrontShippingCalculator.ResolveRate("post", options),
            500_000m);
        Assert.Equal(legacyPrice, reResolvedPrice);
        Assert.Equal(legacyPrice, report.LegacyPriceAmount);

        var minimum = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(
            new DateOnly(2026, 10, 14),
            StorefrontShippingCalculator.MaxSellerPreparationDays([Guid.NewGuid()], options),
            legacyRate.LeadDays);
        Assert.Equal(new DateOnly(2026, 10, 17), minimum);
        Assert.NotEqual(legacyPrice, report.ShippingChargeAmount);
    }

    [Fact]
    public async Task Service_Code_Is_Forwarded_Untouched_To_The_Shipping_Authority()
    {
        var lookup = new RecordingLookup(_ => Quote(ShippingServiceabilityOutcome.Quoted, 150_000m, ShippingReasonCodes.Quoted));
        var evaluator = new StorefrontShippingRateShadowEvaluator(lookup);

        await evaluator.EvaluateAsync(Input(), Enabled(prerequisitesConfirmed: true), default);

        Assert.Equal(1, lookup.Calls);
        Assert.Equal("post", lookup.LastRequest!.ServiceCode);
        Assert.Equal(500_000m, lookup.LastRequest.CartSubtotalExclusive);
        Assert.Equal("IRR", lookup.LastRequest.CurrencyCode);
    }
}
