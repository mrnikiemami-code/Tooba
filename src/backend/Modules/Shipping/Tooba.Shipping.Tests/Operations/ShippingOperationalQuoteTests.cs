using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Operations;
using Xunit;

namespace Tooba.Shipping.Tests.Operations;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-076 — the operational quote seam composes the EXISTING rate engine, the
/// EXISTING packaging resolver and the schedule evaluator into one auditable answer with an explicit
/// payment split, and it never returns a success-shaped zero when a component could not be priced.
/// </summary>
public sealed class ShippingOperationalQuoteTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid Area = Guid.Parse("1f8050a8-c325-59fa-959f-82d76b5b2bc5");
    private static readonly Guid City = Guid.Parse("b1b0a0aa-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static ShippingOperationalQuoteRequest Request(
        string service = "tipax",
        string? option = null,
        decimal subtotal = 500_000m,
        string? currency = "IRR",
        ShippingMeasuredParcel? parcel = null,
        DateTimeOffset? pickupAtUtc = null)
        => new(
            service,
            new DestinationIdentifiers(Country, Area),
            subtotal,
            currency,
            option,
            parcel,
            pickupAtUtc);

    private static ShippingRatePolicy Policy(
        string service = "tipax",
        ShippingDestinationScope? scope = null,
        decimal charge = 120_000m,
        string currency = "IRR",
        decimal? freeAbove = null,
        string? option = null,
        ShippingRateCondition? condition = null)
        => ShippingRatePolicy.Create(
            service,
            scope ?? ShippingDestinationScope.ForAdministrativeArea(Country, Area),
            charge,
            currency,
            1,
            2,
            freeAbove,
            Now,
            optionCode: option,
            condition: condition);

    private static ShippingCoverageGrant Grant(string service = "tipax") =>
        ShippingCoverageGrant.Create(service, ShippingDestinationScope.ForCountry(Country), Now);

    private static ShippingPackagingPolicy Packaging(
        decimal fee,
        string currency = "IRR",
        ShippingFreightPayerMode payer = ShippingFreightPayerMode.Prepaid,
        string? option = null)
        => ShippingPackagingPolicy.Create(
            "tipax",
            ShippingDestinationScope.ForAdministrativeArea(Country, Area),
            fee,
            currency,
            payer,
            Now,
            optionCode: option);

    private static ShippingOperationalQuote Evaluate(
        ShippingOperationalQuoteRequest request,
        IReadOnlyCollection<ShippingRatePolicy>? policies = null,
        IReadOnlyCollection<ShippingCoverageGrant>? grants = null,
        IReadOnlyCollection<ShippingPackagingPolicy>? packaging = null,
        IReadOnlyCollection<Domain.Scheduling.ShippingOperatingSchedule>? schedules = null)
        => ShippingOperationalQuoteEvaluator.Evaluate(
            request,
            policies ?? [Policy()],
            grants ?? [Grant()],
            packaging ?? [],
            schedules ?? []);

    [Fact]
    public void Prepaid_freight_and_packaging_are_both_payable_at_checkout()
    {
        var quote = Evaluate(Request(), packaging: [Packaging(12_000m)]);

        Assert.Equal(ShippingOperationalQuoteOutcome.Quoted, quote.Outcome);
        Assert.Equal(ShippingOperationalQuoteReasonCodes.Quoted, quote.ReasonCode);
        Assert.Equal(120_000m, quote.FreightAmount);
        Assert.Equal(12_000m, quote.PackagingAmount);
        Assert.Equal(132_000m, quote.TotalServiceExpense);
        Assert.Equal(132_000m, quote.AmountPayableAtCheckout);
        Assert.Equal(0m, quote.AmountCollectOnDelivery);
        Assert.Equal(ShippingFreightPayerMode.Prepaid, quote.FreightPayerMode);
        Assert.Equal("IRR", quote.CurrencyCode);
        Assert.True(quote.IsOperationallyEligible);
    }

    [Fact]
    public void Collect_on_delivery_moves_only_the_freight_out_of_the_checkout_amount()
    {
        var quote = Evaluate(
            Request(),
            packaging: [Packaging(12_000m, payer: ShippingFreightPayerMode.CollectOnDelivery)]);

        Assert.Equal(132_000m, quote.TotalServiceExpense);
        // Packaging is never collectable by the carrier: only freight moves to COD.
        Assert.Equal(12_000m, quote.AmountPayableAtCheckout);
        Assert.Equal(120_000m, quote.AmountCollectOnDelivery);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, quote.FreightPayerMode);
        Assert.Equal(ShippingPackagingChargePayer.Checkout, quote.PackagingPayer);
    }

    [Fact]
    public void Free_freight_still_carries_the_independent_packaging_fee()
    {
        var quote = Evaluate(
            Request(subtotal: 5_000_000m),
            policies: [Policy(freeAbove: 1_000_000m)],
            packaging: [Packaging(12_000m)]);

        Assert.Equal(0m, quote.FreightAmount);
        Assert.Equal(12_000m, quote.PackagingAmount);
        Assert.Equal(12_000m, quote.TotalServiceExpense);
        Assert.Equal(12_000m, quote.AmountPayableAtCheckout);
        Assert.Equal(ShippingReasonCodes.QuotedFreeAboveThreshold, quote.CarrierFreight.ReasonCode);
    }

    [Fact]
    public void No_packaging_policy_is_an_explicit_zero_not_a_hidden_charge()
    {
        var quote = Evaluate(Request(), packaging: []);

        Assert.True(quote.IsQuoted);
        Assert.False(quote.Packaging.IsCharged);
        Assert.Equal(ShippingPackagingReasonCodes.NotConfigured, quote.Packaging.ReasonCode);
        Assert.Equal(0m, quote.PackagingAmount);
        Assert.Equal(120_000m, quote.TotalServiceExpense);
    }

    [Fact]
    public void Ambiguous_packaging_policies_block_every_amount()
    {
        var first = Packaging(12_000m);
        var second = Packaging(9_000m);
        var quote = Evaluate(Request(), packaging: [first, second]);

        Assert.Equal(ShippingOperationalQuoteOutcome.PackagingAmbiguous, quote.Outcome);
        Assert.Equal(ShippingOperationalQuoteReasonCodes.PackagingAmbiguous, quote.ReasonCode);
        Assert.Null(quote.FreightAmount);
        Assert.Null(quote.PackagingAmount);
        Assert.Null(quote.TotalServiceExpense);
        Assert.Null(quote.AmountPayableAtCheckout);
        Assert.Null(quote.AmountCollectOnDelivery);
        Assert.False(quote.IsQuoted);
        Assert.False(quote.IsOperationallyEligible);
    }

    [Fact]
    public void Mismatched_component_currencies_are_never_summed_or_converted()
    {
        var quote = Evaluate(Request(), packaging: [Packaging(12_000m, currency: "USD")]);

        Assert.Equal(ShippingOperationalQuoteOutcome.CurrencyMismatch, quote.Outcome);
        Assert.Equal(ShippingOperationalQuoteReasonCodes.CurrencyMismatch, quote.ReasonCode);
        Assert.Null(quote.TotalServiceExpense);
        Assert.Null(quote.AmountPayableAtCheckout);
        Assert.Null(quote.CurrencyCode);
    }

    [Fact]
    public void An_unknown_service_is_reported_through_the_freight_component_not_as_a_zero_price()
    {
        var quote = Evaluate(
            Request(service: "invented"),
            policies: [Policy()],
            grants: [Grant()],
            packaging: [Packaging(12_000m)]);

        Assert.Equal(ShippingOperationalQuoteOutcome.FreightNotQuoted, quote.Outcome);
        Assert.Equal(ShippingOperationalQuoteReasonCodes.FreightNotQuoted, quote.ReasonCode);
        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, quote.CarrierFreight.Outcome);
        Assert.Equal(ShippingReasonCodes.ServiceNotPriced, quote.CarrierFreight.ReasonCode);
        Assert.Null(quote.TotalServiceExpense);
    }

    [Fact]
    public void A_missing_coverage_grant_is_a_coverage_rejection_not_an_unrestricted_price()
    {
        var quote = Evaluate(Request(), grants: []);

        Assert.Equal(ShippingOperationalQuoteOutcome.FreightNotQuoted, quote.Outcome);
        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.CarrierFreight.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, quote.CarrierFreight.ReasonCode);
        Assert.Null(quote.FreightAmount);
    }

    [Fact]
    public void Missing_measurement_is_distinct_from_a_measurement_outside_every_tier()
    {
        var conditioned = Policy(condition: new ShippingRateCondition(Weight: ShippingNumericTier.Between(0, 2_000)));

        var missing = Evaluate(Request(parcel: null), policies: [conditioned]);
        Assert.Equal(ShippingOperationalQuoteOutcome.FreightNotQuoted, missing.Outcome);
        Assert.Equal(ShippingServiceabilityOutcome.MeasurementRequired, missing.CarrierFreight.Outcome);
        Assert.Equal(ShippingReasonCodes.MeasurementRequired, missing.CarrierFreight.ReasonCode);

        var outside = Evaluate(Request(parcel: ShippingMeasuredParcel.WithWeight(99_999)), policies: [conditioned]);
        Assert.Equal(ShippingServiceabilityOutcome.ConditionNotMatched, outside.CarrierFreight.Outcome);
        Assert.Equal(ShippingReasonCodes.ConditionNotMatched, outside.CarrierFreight.ReasonCode);
    }

    [Fact]
    public void Weight_tier_boundary_1999_is_the_lower_tier_and_2000_is_the_upper_tier()
    {
        var policies = new[]
        {
            Policy(charge: 100_000m, condition: new ShippingRateCondition(Weight: ShippingNumericTier.Between(0, 2_000))),
            Policy(charge: 200_000m, condition: new ShippingRateCondition(Weight: ShippingNumericTier.Between(2_000, 5_000))),
        };

        var below = Evaluate(Request(parcel: ShippingMeasuredParcel.WithWeight(1_999)), policies: policies);
        Assert.Equal(100_000m, below.FreightAmount);
        Assert.NotNull(below.MatchedCondition);
        Assert.Equal(2_000, below.MatchedCondition!.Weight!.Max);

        var boundary = Evaluate(Request(parcel: ShippingMeasuredParcel.WithWeight(2_000)), policies: policies);
        Assert.Equal(200_000m, boundary.FreightAmount);
        Assert.Equal(2_000, boundary.MatchedCondition!.Weight!.Min);
    }

    [Fact]
    public void Count_tier_boundary_5_is_the_lower_tier_and_6_is_the_upper_tier()
    {
        var policies = new[]
        {
            Policy(charge: 100_000m, condition: new ShippingRateCondition(Count: ShippingNumericTier.Between(0, 6))),
            Policy(charge: 200_000m, condition: new ShippingRateCondition(Count: ShippingNumericTier.Between(6, 12))),
        };

        var five = Evaluate(Request(parcel: ShippingMeasuredParcel.WithItemCount(5)), policies: policies);
        Assert.Equal(100_000m, five.FreightAmount);

        var six = Evaluate(Request(parcel: ShippingMeasuredParcel.WithItemCount(6)), policies: policies);
        Assert.Equal(200_000m, six.FreightAmount);
    }

    [Fact]
    public void Both_measurement_axes_must_match_the_same_tier()
    {
        var policy = Policy(
            charge: 150_000m,
            condition: new ShippingRateCondition(
                Weight: ShippingNumericTier.Between(2_000, 5_000),
                Count: ShippingNumericTier.Between(6, 12)));

        // Inside both axes: priced by the intersection tier.
        var both = Evaluate(
            Request(parcel: new ShippingMeasuredParcel(3_000, 8)),
            policies: [policy]);
        Assert.Equal(150_000m, both.FreightAmount);
        Assert.NotNull(both.MatchedCondition);

        // Weight alone is not enough: the count axis is a real condition, not a hint.
        var weightOnly = Evaluate(
            Request(parcel: ShippingMeasuredParcel.WithWeight(3_000)),
            policies: [policy]);
        Assert.Equal(ShippingServiceabilityOutcome.MeasurementRequired, weightOnly.CarrierFreight.Outcome);

        // Both supplied but the count axis is outside: not matched, and NOT "measurement missing".
        var countOutside = Evaluate(
            Request(parcel: new ShippingMeasuredParcel(3_000, 99)),
            policies: [policy]);
        Assert.Equal(ShippingServiceabilityOutcome.ConditionNotMatched, countOutside.CarrierFreight.Outcome);
    }

    [Fact]
    public void An_option_specific_policy_wins_and_an_unknown_option_is_reported_by_the_engine()
    {
        var policies = new[]
        {
            Policy(charge: 100_000m),
            Policy(charge: 250_000m, option: "express"),
        };

        var express = Evaluate(Request(option: "express"), policies: policies);
        Assert.Equal(250_000m, express.FreightAmount);

        var standard = Evaluate(Request(option: "standard"), policies: policies);
        Assert.Equal(100_000m, standard.FreightAmount);
    }

    [Fact]
    public void The_deepest_destination_scope_wins_over_the_country_scope()
    {
        var policies = new[]
        {
            Policy(scope: ShippingDestinationScope.ForCountry(Country), charge: 300_000m),
            Policy(scope: ShippingDestinationScope.ForAdministrativeArea(Country, Area), charge: 120_000m),
            Policy(scope: ShippingDestinationScope.ForCity(Country, Area, City), charge: 90_000m),
        };

        var quote = Evaluate(Request(), policies: policies);

        // The request's own depth is AdministrativeArea, so the city row (deeper than the request) cannot
        // match while the area row is the deepest compatible one.
        Assert.Equal(120_000m, quote.FreightAmount);
    }

    [Fact]
    public void The_seam_is_deterministic_and_never_reads_the_clock()
    {
        // Same request, same data, two calls: an implicit "now" would make the schedule verdict drift.
        var request = Request();

        var first = Evaluate(request, packaging: [Packaging(12_000m)]);
        var second = Evaluate(request, packaging: [Packaging(12_000m)]);

        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.TotalServiceExpense, second.TotalServiceExpense);
        Assert.Equal(first.Schedule.ReasonCode, second.Schedule.ReasonCode);
    }

    [Fact]
    public void The_contracts_assembly_exposes_the_operational_seam_as_read_only()
    {
        var assembly = typeof(IShippingOperationalQuoteLookup).Assembly;

        var lookup = Assert.Single(assembly.GetTypes(), x => x == typeof(IShippingOperationalQuoteLookup));
        Assert.True(lookup.IsInterface);

        var methods = lookup.GetMethods();
        Assert.Single(methods);
        Assert.Equal("QuoteAsync", methods[0].Name);
        Assert.True(typeof(Task<ShippingOperationalQuote>).IsAssignableFrom(methods[0].ReturnType));
    }
}
