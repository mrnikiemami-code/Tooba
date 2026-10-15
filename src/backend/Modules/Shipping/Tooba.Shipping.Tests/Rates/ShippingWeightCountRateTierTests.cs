using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-073 — پله‌های وزن/تعداد در سیاست نرخ: معنای نیم‌باز، مرزهای دقیق
/// ۱۹۹۹/۲۰۰۰ و ۵/۶، تقدم قطعی، اندازهٔ غایب fail-closed، و دست‌نخورده‌ماندن ردیف‌های بی‌شرط قدیمی.
/// </summary>
public sealed class ShippingWeightCountRateTierTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid Area = Guid.Parse("1f8050a8-c325-59fa-959f-82d76b5b2bc5");
    private static readonly Guid City = Guid.Parse("68255340-c69f-5d20-bf33-e8e0b24edde9");
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 6, 30, 0, TimeSpan.Zero);

    private static ShippingDestinationScope AreaScope => ShippingDestinationScope.ForAdministrativeArea(Country, Area);

    private static ShippingCoverageGrant Grant(string service) => ShippingCoverageGrant.Create(service, AreaScope, Now);

    private static ShippingQuoteRequest Request(
        string service,
        ShippingMeasuredParcel? parcel,
        string? option = null,
        Guid? cityId = null,
        string? currency = "IRR") =>
        new(service, new DestinationIdentifiers(Country, Area, cityId), 1_000_000m, currency, option, parcel);

    private static ShippingRatePolicy Tiered(
        string service,
        decimal charge,
        ShippingRateCondition? condition,
        string? option = null) =>
        ShippingRatePolicy.Create(
            service,
            AreaScope,
            charge,
            "IRR",
            1,
            2,
            null,
            Now,
            ratePolicyId: null,
            optionCode: option,
            condition: condition);

    private static ShippingRateCondition Weight(long min, long? max) =>
        new(ShippingNumericTier.Between(min, max), null);

    private static ShippingRateCondition Count(long min, long? max) =>
        new(null, ShippingNumericTier.Between(min, max));

    // ------------------------------------------------------------------
    // Half-open boundary semantics: the two documented tiers [0,2000) and [2000,5000)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(0, 120_000)]
    [InlineData(1, 120_000)]
    [InlineData(1999, 120_000)]
    [InlineData(2000, 260_000)]
    [InlineData(2001, 260_000)]
    [InlineData(4999, 260_000)]
    public void Weight_tier_boundary_is_exact_at_1999_and_2000(long weightGrams, decimal expectedCharge)
    {
        var policies = new[]
        {
            Tiered("tipax", 120_000m, Weight(0, 2000)),
            Tiered("tipax", 260_000m, Weight(2000, 5000)),
        };
        var grants = new[] { Grant("tipax") };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(weightGrams)),
            policies,
            grants);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
        Assert.Equal(expectedCharge, quote.ChargeAmount);
    }

    [Theory]
    [InlineData(1, 100_000)]
    [InlineData(5, 100_000)]
    [InlineData(6, 180_000)]
    [InlineData(10, 180_000)]
    public void Count_tier_boundary_is_exact_at_5_and_6(int itemCount, decimal expectedCharge)
    {
        var policies = new[]
        {
            Tiered("tipax", 100_000m, Count(1, 6)),
            Tiered("tipax", 180_000m, Count(6, 11)),
        };
        var grants = new[] { Grant("tipax") };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithItemCount(itemCount)),
            policies,
            grants);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
        Assert.Equal(expectedCharge, quote.ChargeAmount);
    }

    [Fact]
    public void The_matched_condition_is_reported_so_the_winning_tier_is_auditable()
    {
        var policies = new[] { Tiered("tipax", 260_000m, Weight(2000, 5000)) };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(2500)),
            policies,
            [Grant("tipax")]);

        Assert.NotNull(quote.MatchedCondition);
        Assert.NotNull(quote.MatchedCondition!.Weight);
        Assert.Equal(2000, quote.MatchedCondition.Weight!.Min);
        Assert.Equal(5000, quote.MatchedCondition.Weight!.Max);
        Assert.Null(quote.MatchedCondition.Count);
    }

    // ------------------------------------------------------------------
    // Missing measurement must never match a restricted tier
    // ------------------------------------------------------------------

    [Fact]
    public void A_conditioned_policy_never_matches_a_request_that_omitted_the_measurement()
    {
        var policies = new[] { Tiered("tipax", 260_000m, Weight(0, 5000)) };

        var quote = ShippingRatePolicyEngine.Quote(Request("tipax", parcel: null), policies, [Grant("tipax")]);

        Assert.Equal(ShippingServiceabilityOutcome.MeasurementRequired, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.MeasurementRequired, quote.ReasonCode);
        Assert.Equal(0m, quote.ChargeAmount);
    }

    [Fact]
    public void A_weight_only_request_does_not_satisfy_a_count_condition()
    {
        var policies = new[] { Tiered("tipax", 100_000m, Count(1, 6)) };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(1500)),
            policies,
            [Grant("tipax")]);

        Assert.Equal(ShippingServiceabilityOutcome.MeasurementRequired, quote.Outcome);
    }

    [Fact]
    public void A_measurement_outside_every_tier_is_reported_distinctly_from_a_missing_one()
    {
        var policies = new[] { Tiered("tipax", 120_000m, Weight(0, 2000)) };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(9000)),
            policies,
            [Grant("tipax")]);

        Assert.Equal(ShippingServiceabilityOutcome.ConditionNotMatched, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.ConditionNotMatched, quote.ReasonCode);
    }

    [Fact]
    public void An_unconditioned_base_row_still_covers_measurements_no_tier_declares()
    {
        // The documented fallback: a conditioned sibling is an explicit exception, the unconditioned row
        // stays the general rule. This is exactly how every pre-073 row keeps working.
        var policies = new[]
        {
            Tiered("tipax", 90_000m, condition: null),
            Tiered("tipax", 260_000m, Weight(0, 2000)),
        };

        var insideTier = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(1500)),
            policies,
            [Grant("tipax")]);
        var outsideTier = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(9000)),
            policies,
            [Grant("tipax")]);
        var noMeasurement = ShippingRatePolicyEngine.Quote(
            Request("tipax", parcel: null),
            policies,
            [Grant("tipax")]);

        Assert.Equal(260_000m, insideTier.ChargeAmount);
        Assert.Equal(90_000m, outsideTier.ChargeAmount);
        Assert.Equal(90_000m, noMeasurement.ChargeAmount);
        Assert.Null(outsideTier.MatchedCondition);
    }

    // ------------------------------------------------------------------
    // Deterministic precedence: option, then destination depth, then tier
    // ------------------------------------------------------------------

    [Fact]
    public void A_weight_tier_never_overrides_option_identity_or_destination_depth()
    {
        var policies = new[]
        {
            // Option-specific unconditioned express row.
            Tiered("tipax", 300_000m, condition: null, option: "express"),
            // Option-specific standard tiers.
            Tiered("tipax", 120_000m, Weight(0, 2000), option: "standard"),
            Tiered("tipax", 260_000m, Weight(2000, 5000), option: "standard"),
            // A city-scoped standard tier is deeper than the area-scoped ones above.
            ShippingRatePolicy.Create(
                "tipax",
                ShippingDestinationScope.ForCity(Country, Area, City),
                70_000m,
                "IRR",
                1,
                2,
                null,
                Now,
                ratePolicyId: null,
                optionCode: "standard",
                condition: Weight(0, 5000)),
        };
        var grants = new[] { Grant("tipax") };

        var express = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(1500), "express"),
            policies,
            grants);
        var areaStandard = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(2500), "standard"),
            policies,
            grants);
        var cityStandard = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(2500), "standard", City),
            policies,
            grants);

        // Option identity first: express keeps its own price and never picks up a standard weight tier.
        Assert.Equal(300_000m, express.ChargeAmount);
        Assert.Null(express.MatchedCondition);

        // Inside standard, the area-scoped tier wins by weight.
        Assert.Equal(260_000m, areaStandard.ChargeAmount);

        // Destination depth beats the tier: the city row prices even though it is conditioned.
        Assert.Equal(70_000m, cityStandard.ChargeAmount);
    }

    [Fact]
    public void Two_tiers_matching_the_same_parcel_stay_a_conflict_not_a_row_order_pick()
    {
        var policies = new[]
        {
            Tiered("tipax", 120_000m, Weight(0, 3000)),
            Tiered("tipax", 260_000m, Weight(2000, 5000)),
        };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(2500)),
            policies,
            [Grant("tipax")]);

        Assert.Equal(ShippingServiceabilityOutcome.ConditionConflict, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.ConditionAmbiguous, quote.ReasonCode);
    }

    [Fact]
    public void Weight_and_count_conditions_are_an_intersection_not_a_union()
    {
        var policies = new[]
        {
            // Only the row whose BOTH axes contain the parcel may win.
            Tiered("tipax", 120_000m, new ShippingRateCondition(
                ShippingNumericTier.Between(0, 2000),
                ShippingNumericTier.Between(1, 6))),
            Tiered("tipax", 260_000m, new ShippingRateCondition(
                ShippingNumericTier.Between(2000, 5000),
                ShippingNumericTier.Between(6, 11))),
        };
        var grants = new[] { Grant("tipax") };

        var lightSmall = ShippingRatePolicyEngine.Quote(
            Request("tipax", new ShippingMeasuredParcel(1500, 3)),
            policies,
            grants);
        var heavyMany = ShippingRatePolicyEngine.Quote(
            Request("tipax", new ShippingMeasuredParcel(3000, 8)),
            policies,
            grants);
        // Light but many items: the weight axis matches the first row, the count axis does not, and the
        // second row fails on weight too. Neither row matches, so the request is explicitly not priced.
        var crossed = ShippingRatePolicyEngine.Quote(
            Request("tipax", new ShippingMeasuredParcel(1500, 8)),
            policies,
            grants);

        Assert.Equal(120_000m, lightSmall.ChargeAmount);
        Assert.Equal(260_000m, heavyMany.ChargeAmount);
        Assert.Equal(ShippingServiceabilityOutcome.ConditionNotMatched, crossed.Outcome);
    }

    [Fact]
    public void Coverage_stays_a_service_and_destination_decision_for_tiered_rows_too()
    {
        var policies = new[] { Tiered("tipax", 120_000m, Weight(0, 2000)) };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(1500)),
            policies,
            []);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
    }

    [Fact]
    public void Currency_equality_and_the_free_threshold_still_apply_to_a_tiered_row()
    {
        var policies = new[]
        {
            ShippingRatePolicy.Create(
                "tipax",
                AreaScope,
                260_000m,
                "IRR",
                1,
                2,
                900_000m,
                Now,
                ratePolicyId: null,
                optionCode: null,
                condition: Weight(0, 2000)),
        };
        var grants = new[] { Grant("tipax") };

        var free = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(1500)),
            policies,
            grants);
        var mismatch = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(1500), currency: "USD"),
            policies,
            grants);

        Assert.Equal(0m, free.ChargeAmount);
        Assert.Equal(ShippingReasonCodes.QuotedFreeAboveThreshold, free.ReasonCode);
        Assert.Equal(ShippingServiceabilityOutcome.CurrencyMismatch, mismatch.Outcome);
    }

    // ------------------------------------------------------------------
    // Domain shape guards: reversed, zero-width, negative and oversized tiers
    // ------------------------------------------------------------------

    [Fact]
    public void Reversed_zero_width_and_negative_tiers_are_refused_with_stable_codes()
    {
        var reversed = Assert.Throws<SemanticException>(() => Tiered("tipax", 1m, Weight(2000, 2000)));
        Assert.Equal(ShippingErrorCodes.WeightTierInvalid, reversed.Message);

        var inverted = Assert.Throws<SemanticException>(() => Tiered("tipax", 1m, Weight(3000, 2000)));
        Assert.Equal(ShippingErrorCodes.WeightTierInvalid, inverted.Message);

        var negative = Assert.Throws<SemanticException>(() => Tiered("tipax", 1m, Weight(-1, 2000)));
        Assert.Equal(ShippingErrorCodes.WeightTierInvalid, negative.Message);

        var oversized = Assert.Throws<SemanticException>(() => Tiered(
            "tipax",
            1m,
            Weight(0, ShippingRatePolicy.WeightGramsMaxValue + 1)));
        Assert.Equal(ShippingErrorCodes.WeightTierOutOfRange, oversized.Message);

        var countReversed = Assert.Throws<SemanticException>(() => Tiered("tipax", 1m, Count(5, 5)));
        Assert.Equal(ShippingErrorCodes.CountTierInvalid, countReversed.Message);
    }

    [Fact]
    public void An_open_upper_bound_is_supported_and_contains_every_larger_measurement()
    {
        var policy = Tiered("tipax", 400_000m, Weight(5000, null));

        Assert.True(policy.IsWeightConditioned);
        Assert.False(policy.MatchesCondition(ShippingMeasuredParcel.WithWeight(4999)));
        Assert.True(policy.MatchesCondition(ShippingMeasuredParcel.WithWeight(5000)));
        Assert.True(policy.MatchesCondition(ShippingMeasuredParcel.WithWeight(10_000_000)));

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", ShippingMeasuredParcel.WithWeight(50_000)),
            [policy],
            [Grant("tipax")]);
        Assert.Equal(400_000m, quote.ChargeAmount);
    }

    [Fact]
    public void An_empty_condition_object_collapses_to_unconditioned()
    {
        var policy = Tiered("tipax", 1m, new ShippingRateCondition());

        Assert.False(policy.IsWeightConditioned);
        Assert.False(policy.IsCountConditioned);
        Assert.True(policy.IsUnconditioned);
        Assert.True(policy.MatchesCondition(parcel: null));
    }

    [Fact]
    public void Zero_weight_and_zero_count_are_real_measurements_inside_a_zero_based_tier()
    {
        var weight = Tiered("tipax", 10m, Weight(0, 2000));
        var count = Tiered("tipax", 20m, Count(0, 6));

        Assert.True(weight.MatchesCondition(ShippingMeasuredParcel.WithWeight(0)));
        Assert.True(count.MatchesCondition(ShippingMeasuredParcel.WithItemCount(0)));
        // A count tier that starts at 1 deliberately excludes the zero-item parcel: zero items is not a
        // shipment, so it must not be priced by a positive-count rule.
        Assert.False(Tiered("tipax", 20m, Count(1, 6)).MatchesCondition(ShippingMeasuredParcel.WithItemCount(0)));
    }

    [Fact]
    public void Tier_overlap_detection_is_symmetric_and_respects_adjacency()
    {
        var low = Tiered("tipax", 1m, Weight(0, 2000));
        var high = Tiered("tipax", 1m, Weight(2000, 5000));
        var crossed = Tiered("tipax", 1m, Weight(1500, 3000));

        Assert.False(low.TiersOverlapWith(Weight(2000, 5000)));
        Assert.False(high.TiersOverlapWith(Weight(0, 2000)));
        Assert.True(low.TiersOverlapWith(Weight(1500, 3000)));
        Assert.True(crossed.TiersOverlapWith(Weight(0, 2000)));

        // A conditioned row next to an unconditioned base row is the documented fallback, not a conflict.
        Assert.False(low.TiersOverlapWith(candidate: null));
        Assert.False(low.TiersOverlapWith(new ShippingRateCondition()));
    }

    [Fact]
    public void Tier_overlap_on_one_axis_only_is_not_an_overlap_when_the_other_axis_is_disjoint()
    {
        var smallShipment = Tiered("tipax", 1m, new ShippingRateCondition(
            ShippingNumericTier.Between(0, 5000),
            ShippingNumericTier.Between(1, 6)));
        var bulkShipment = Tiered("tipax", 1m, new ShippingRateCondition(
            ShippingNumericTier.Between(0, 5000),
            ShippingNumericTier.Between(6, 11)));

        // The weight axes are identical, but the count axes are adjacent (not overlapping), so the pair
        // is a legal two-dimensional partition rather than an ambiguous overlap.
        Assert.False(smallShipment.TiersOverlapWith(bulkShipment.Condition));
    }

    [Fact]
    public void Legacy_unconditioned_rows_keep_their_exact_previous_behavior()
    {
        // A pre-073 row: no condition at all. Nothing about its result may change, including the
        // free-shipping threshold that already applied to it before this task.
        var legacy = ShippingRatePolicy.Create(
            "post",
            AreaScope,
            120_000m,
            "IRR",
            1,
            4,
            500_000m,
            Now,
            ratePolicyId: null,
            optionCode: null);

        Assert.True(legacy.IsUnconditioned);
        Assert.Null(legacy.Condition.Weight);
        Assert.Null(legacy.Condition.Count);

        var withParcel = ShippingRatePolicyEngine.Quote(
            Request("post", ShippingMeasuredParcel.WithWeight(999_999)),
            [legacy],
            [Grant("post")]);
        var withoutParcel = ShippingRatePolicyEngine.Quote(Request("post", parcel: null), [legacy], [Grant("post")]);

        // Request() carries a 1_000_000 subtotal, which is above the 500_000 threshold, so the legacy
        // free-shipping rule still zeroes the charge exactly as before - with or without a measurement.
        Assert.Equal(0m, withParcel.ChargeAmount);
        Assert.Equal(0m, withoutParcel.ChargeAmount);
        Assert.Equal(ShippingReasonCodes.QuotedFreeAboveThreshold, withoutParcel.ReasonCode);
        Assert.Null(withoutParcel.MatchedCondition);

        // Below the threshold the legacy fixed amount is unchanged too.
        var belowThreshold = ShippingRatePolicyEngine.Quote(
            new ShippingQuoteRequest(
                "post",
                new DestinationIdentifiers(Country, Area),
                400_000m,
                "IRR"),
            [legacy],
            [Grant("post")]);
        Assert.Equal(120_000m, belowThreshold.ChargeAmount);
        Assert.Equal(ShippingReasonCodes.Quoted, belowThreshold.ReasonCode);
    }
}
