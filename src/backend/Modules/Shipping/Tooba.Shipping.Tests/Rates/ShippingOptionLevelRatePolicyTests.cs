using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Rates;
using Tooba.Shipping.Infrastructure.Rates;
using Tooba.Geography.Contracts.Places;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// TB-SHIPPING-MVP-OPTION-PARITY-069 — هویت گزینه در سیاست نرخ: تقدم قطعی، عدم نشت میان‌گزینه‌ای،
/// سازگاری ردیف‌های قدیمی بدون گزینه، و رد ترکیب گزینهٔ ناشناخته از درز کاتالوگ Fulfillment.
/// </summary>
public sealed class ShippingOptionLevelRatePolicyTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid Area = Guid.Parse("1f8050a8-c325-59fa-959f-82d76b5b2bc5");
    private static readonly Guid City = Guid.Parse("68255340-c69f-5d20-bf33-e8e0b24edde9");
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);

    private static ShippingDestinationScope AreaScope => ShippingDestinationScope.ForAdministrativeArea(Country, Area);

    private static ShippingQuoteRequest Request(string service, string? option, Guid? cityId = null) =>
        new(service, new DestinationIdentifiers(Country, Area, cityId), 1_000_000m, "IRR", option);

    private static ShippingCoverageGrant Grant(string service) =>
        ShippingCoverageGrant.Create(service, AreaScope, Now);

    private static ShippingRatePolicy Policy(string service, decimal charge, int lead, string? option) =>
        ShippingRatePolicy.Create(
            service,
            AreaScope,
            charge,
            "IRR",
            1,
            lead,
            null,
            Now,
            ratePolicyId: null,
            optionCode: option);

    [Fact]
    public void Option_specific_policies_price_their_own_option_only()
    {
        var policies = new[]
        {
            Policy("tipax", 180_000m, 3, "standard"),
            Policy("tipax", 260_000m, 1, "express"),
        };
        var grants = new[] { Grant("tipax") };

        var standard = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);
        var express = ShippingRatePolicyEngine.Quote(Request("tipax", "express"), policies, grants);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, standard.Outcome);
        Assert.Equal(180_000m, standard.ChargeAmount);
        Assert.Equal(3, standard.LeadDays);
        Assert.Equal("standard", standard.OptionCode);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, express.Outcome);
        Assert.Equal(260_000m, express.ChargeAmount);
        Assert.Equal(1, express.LeadDays);
        Assert.Equal("express", express.OptionCode);
    }

    [Fact]
    public void Service_level_policy_still_prices_options_that_have_no_policy_of_their_own()
    {
        // The documented fallback: single-option and no-option services keep working unchanged.
        var policies = new[] { Policy("post", 120_000m, 4, null) };
        var grants = new[] { Grant("post") };

        var byOption = ShippingRatePolicyEngine.Quote(Request("post", "standard"), policies, grants);
        var serviceOnly = ShippingRatePolicyEngine.Quote(Request("post", null), policies, grants);

        Assert.Equal(120_000m, byOption.ChargeAmount);
        Assert.Equal(ShippingServiceabilityOutcome.Quoted, serviceOnly.Outcome);
        Assert.Equal(120_000m, serviceOnly.ChargeAmount);
    }

    [Fact]
    public void Option_specific_policy_wins_over_the_service_level_policy()
    {
        var policies = new[]
        {
            Policy("tipax", 180_000m, 3, null),
            Policy("tipax", 260_000m, 1, "express"),
        };
        var grants = new[] { Grant("tipax") };

        var express = ShippingRatePolicyEngine.Quote(Request("tipax", "express"), policies, grants);
        var standard = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);

        Assert.Equal(260_000m, express.ChargeAmount);
        // No leakage: the express row never prices the standard option, which falls back to the service row.
        Assert.Equal(180_000m, standard.ChargeAmount);
    }

    [Fact]
    public void Option_specific_policy_of_one_service_never_prices_another_service()
    {
        var policies = new[]
        {
            Policy("tipax", 260_000m, 1, "express"),
            Policy("post", 120_000m, 4, null),
        };
        var grants = new[] { Grant("tipax"), Grant("post") };

        var post = ShippingRatePolicyEngine.Quote(Request("post", "express"), policies, grants);

        Assert.Equal(120_000m, post.ChargeAmount);
    }

    [Fact]
    public void Deepest_scope_still_wins_inside_the_same_option_tier()
    {
        var policies = new[]
        {
            Policy("tipax", 180_000m, 3, "standard"),
            ShippingRatePolicy.Create(
                "tipax",
                ShippingDestinationScope.ForCity(Country, Area, City),
                90_000m,
                "IRR",
                1,
                2,
                null,
                Now,
                ratePolicyId: null,
                optionCode: "standard"),
        };
        var grants = new[] { Grant("tipax") };

        var city = ShippingRatePolicyEngine.Quote(Request("tipax", "standard", City), policies, grants);
        var areaOnly = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);

        Assert.Equal(90_000m, city.ChargeAmount);
        Assert.Equal(180_000m, areaOnly.ChargeAmount);
    }

    [Fact]
    public void Two_rows_at_the_same_option_and_scope_stay_a_conflict_not_a_coin_flip()
    {
        var policies = new[]
        {
            Policy("tipax", 180_000m, 3, "standard"),
            Policy("tipax", 180_000m, 3, "standard"),
        };
        var grants = new[] { Grant("tipax") };

        var quote = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);

        Assert.Equal(ShippingServiceabilityOutcome.ScopeConflict, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.ScopeAmbiguous, quote.ReasonCode);
    }

    [Fact]
    public void Option_identity_never_widens_coverage()
    {
        // Coverage stays a service+destination decision: an express policy with no grant is NOT covered.
        var policies = new[] { Policy("tipax", 260_000m, 1, "express") };

        var quote = ShippingRatePolicyEngine.Quote(Request("tipax", "express"), policies, []);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, quote.ReasonCode);
    }

    [Fact]
    public void Option_code_is_canonicalized_and_over_long_codes_are_refused()
    {
        var padded = Policy("tipax", 1m, 1, "  express  ");
        Assert.Equal("express", padded.OptionCode);
        Assert.True(padded.IsOptionSpecific);

        var blank = Policy("tipax", 1m, 1, "   ");
        Assert.Null(blank.OptionCode);
        Assert.False(blank.IsOptionSpecific);

        var thrown = Assert.Throws<SemanticException>(() => Policy(
            "tipax",
            1m,
            1,
            new string('x', ShippingRatePolicy.OptionCodeMaxLength + 1)));
        Assert.Equal(ShippingErrorCodes.OptionCodeInvalid, thrown.Message);
    }

    [Fact]
    public async Task Unknown_option_and_unknown_service_are_refused_through_the_catalog_seam()
    {
        var validator = new FulfillmentShippingMethodOptionValidator(new StubCatalog(), new ModuleCallTracer());

        var ok = await validator.ValidateAsync("tipax", "express", default);
        var okServiceLevel = await validator.ValidateAsync("tipax", null, default);
        var unknownOption = await validator.ValidateAsync("tipax", "ghost", default);
        var unknownService = await validator.ValidateAsync("ghost_service", null, default);
        var compositeAsService = await validator.ValidateAsync("tipax:express", null, default);

        Assert.True(ok.IsSuccess);
        Assert.True(okServiceLevel.IsSuccess);
        Assert.Equal(ShippingErrorCodes.ServiceOptionUnknown, unknownOption.FirstError.Code);
        Assert.Equal(ShippingErrorCodes.ServiceCodeUnknown, unknownService.FirstError.Code);
        Assert.Equal(ShippingErrorCodes.ServiceCodeUnknown, compositeAsService.FirstError.Code);
    }

    private sealed class StubCatalog : IShippingCatalogReader
    {
        private static readonly ShippingCatalogServiceSnapshot Tipax = new(
            Guid.NewGuid(),
            "tipax",
            "ThirdParty",
            "truck",
            "brand",
            true,
            1,
            [new ShippingCatalogTranslationSnapshot(Guid.NewGuid(), "تیپاکس", null)],
            [
                new ShippingCatalogOptionSnapshot(Guid.NewGuid(), Guid.NewGuid(), "standard", true, 1, []),
                new ShippingCatalogOptionSnapshot(Guid.NewGuid(), Guid.NewGuid(), "express", true, 2, []),
            ]);

        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>([Tipax]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken)
            => Task.FromResult<ShippingCatalogServiceSnapshot?>(Tipax.ShippingServiceId == serviceId ? Tipax : null);
    }
}
