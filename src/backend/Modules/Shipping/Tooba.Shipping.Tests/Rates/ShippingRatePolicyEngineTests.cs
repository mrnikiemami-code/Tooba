using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// سیاست fail-closed خدمت‌رسانی: نبود پوشش هرگز به «مجاز» تبدیل نمی‌شود، ابهام هرگز حل نمی‌شود و
/// عمیق‌ترین دامنه برنده است.
/// </summary>
public sealed class ShippingRatePolicyEngineTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid OtherCountry = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e331");
    private static readonly Guid Tehran = Guid.Parse("3cefa472-d0ac-5b2e-817b-ae7df7c33382");
    private static readonly Guid Isfahan = Guid.Parse("ed83ce72-057e-5222-b05a-12283c27386d");
    private static readonly Guid TehranCity = Guid.Parse("b1b0a0aa-0000-4000-8000-000000000001");
    private static readonly Guid Karaj = Guid.Parse("b1b0a0aa-0000-4000-8000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static ShippingQuoteRequest Request(
        string serviceCode = "post",
        Guid? countryId = null,
        Guid? areaId = null,
        Guid? cityId = null,
        decimal subtotal = 0m,
        string? currency = null)
        => new(
            serviceCode,
            new DestinationIdentifiers(countryId ?? Country, areaId, cityId),
            subtotal,
            currency);

    private static ShippingRatePolicy Policy(
        string serviceCode,
        ShippingDestinationScope scope,
        decimal charge,
        decimal? freeAbove = null,
        string currency = "IRR")
        => ShippingRatePolicy.Create(serviceCode, scope, charge, currency, 1, 2, freeAbove, Now);

    private static ShippingCoverageGrant Grant(string serviceCode, ShippingDestinationScope scope)
        => ShippingCoverageGrant.Create(serviceCode, scope, Now);

    [Fact]
    public void Missing_coverage_grant_is_denied_not_unrestricted()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m)],
            []);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
        Assert.False(quote.IsQuoted);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, quote.ReasonCode);
        Assert.Null(quote.CurrencyCode);
    }

    [Fact]
    public void Null_coverage_collection_is_also_denied()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m)],
            null);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
    }

    [Fact]
    public void Service_code_without_any_active_policy_is_unknown()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax"),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m)],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.ServiceNotPriced, quote.ReasonCode);
    }

    [Fact]
    public void Inactive_policy_is_not_used()
    {
        var policy = Policy("post", ShippingDestinationScope.ForCountry(Country), 150m);
        policy.Deactivate(Now);

        var quote = ShippingRatePolicyEngine.Quote(
            Request(),
            [policy],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, quote.Outcome);
    }

    [Fact]
    public void Inactive_coverage_grant_does_not_grant_coverage()
    {
        var grant = Grant("post", ShippingDestinationScope.ForCountry(Country));
        grant.Deactivate(Now);

        var quote = ShippingRatePolicyEngine.Quote(
            Request(),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m)],
            [grant]);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
    }

    [Fact]
    public void Deepest_scope_wins_over_country_scope()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(areaId: Tehran),
            [
                Policy("post", ShippingDestinationScope.ForCountry(Country), 150m),
                Policy("post", ShippingDestinationScope.ForAdministrativeArea(Country, Tehran), 90m),
            ],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
        Assert.Equal(90m, quote.ChargeAmount);
    }

    [Fact]
    public void A_policy_for_another_area_does_not_cover_the_request()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(areaId: Tehran),
            [Policy("post", ShippingDestinationScope.ForAdministrativeArea(Country, Isfahan), 90m)],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, quote.Outcome);
    }

    [Fact]
    public void A_policy_for_another_country_does_not_cover_the_request()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(),
            [Policy("post", ShippingDestinationScope.ForCountry(OtherCountry), 90m)],
            [Grant("post", ShippingDestinationScope.ForCountry(OtherCountry))]);

        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, quote.Outcome);
    }

    [Fact]
    public void Equally_specific_competing_policies_are_reported_not_picked()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(cityId: TehranCity),
            [
                Policy("post", ShippingDestinationScope.ForCity(Country, null, TehranCity), 100m),
                Policy("post", ShippingDestinationScope.ForCity(Country, null, TehranCity), 120m),
            ],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.ScopeConflict, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.ScopeAmbiguous, quote.ReasonCode);
    }

    [Fact]
    public void Country_level_coverage_grant_covers_a_city_destination_explicitly()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(cityId: Karaj),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m)],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
        Assert.Equal(150m, quote.ChargeAmount);
    }

    [Fact]
    public void Coverage_grant_must_cover_the_requested_subtree()
    {
        // The grant is narrowed to Isfahan, so a Tehran request stays denied even though a policy exists.
        var quote = ShippingRatePolicyEngine.Quote(
            Request(areaId: Tehran),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m)],
            [Grant("post", ShippingDestinationScope.ForAdministrativeArea(Country, Isfahan))]);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
    }

    [Fact]
    public void Free_above_threshold_zeroes_the_charge_and_says_so()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(subtotal: 5_000m),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m, freeAbove: 1_000m)],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
        Assert.Equal(0m, quote.ChargeAmount);
        Assert.Equal(ShippingReasonCodes.QuotedFreeAboveThreshold, quote.ReasonCode);
        Assert.Equal(1_000m, quote.FreeAboveThreshold);
    }

    [Fact]
    public void Below_the_threshold_the_charge_is_kept()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(subtotal: 999m),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m, freeAbove: 1_000m)],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(150m, quote.ChargeAmount);
        Assert.Equal(ShippingReasonCodes.Quoted, quote.ReasonCode);
    }

    [Fact]
    public void Currency_mismatch_is_reported_instead_of_converted()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(currency: "USD"),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m, currency: "IRR")],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.CurrencyMismatch, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.CurrencyMismatch, quote.ReasonCode);
    }

    [Fact]
    public void Matching_currency_is_accepted_case_insensitively()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(currency: "irr"),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m, currency: "IRR")],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
        Assert.Equal("IRR", quote.CurrencyCode);
    }

    [Fact]
    public void Preparation_and_lead_days_are_returned_as_two_separate_numbers()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request(),
            [ShippingRatePolicy.Create("post", ShippingDestinationScope.ForCountry(Country), 150m, "IRR", 4, 2, null, Now)],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(4, quote.PreparationDays);
        Assert.Equal(2, quote.LeadDays);
    }

    [Fact]
    public void Service_code_matching_ignores_case_and_surrounding_whitespace()
    {
        var quote = ShippingRatePolicyEngine.Quote(
            Request("  POST "),
            [Policy("post", ShippingDestinationScope.ForCountry(Country), 150m)],
            [Grant("post", ShippingDestinationScope.ForCountry(Country))]);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
    }
}
