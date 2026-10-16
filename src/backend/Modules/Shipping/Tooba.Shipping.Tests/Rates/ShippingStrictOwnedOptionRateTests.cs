using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Rates;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// TB-SHIPPING-OWNER-STRICT-OPTION-RATE-160 — مالکیت سخت‌گیرانهٔ گزینه در موتور نرخ مالک‌شده.
/// </summary>
/// <remarks>
/// این آزمون‌ها مرز «هیچ ردیف ارائه‌دهنده‌ای (سطح‌خدمت) و هیچ ردیف هم‌خانوادهٔ گزینهٔ دیگر هرگز
/// قیمت این گزینه را نمی‌شود» را روی موتور/آداپتور واقعی اثبات می‌کنند و هم‌زمان ثابت می‌کنند حالت
/// پیش‌فرض قدیمی دست‌نخورده مانده است.
/// </remarks>
public sealed class ShippingStrictOwnedOptionRateTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid Area = Guid.Parse("1f8050a8-c325-59fa-959f-82d76b5b2bc5");
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 21, 0, 0, TimeSpan.Zero);

    private static ShippingDestinationScope AreaScope => ShippingDestinationScope.ForAdministrativeArea(Country, Area);

    private static ShippingDestinationScope CountryScope => ShippingDestinationScope.ForCountry(Country);

    private static ShippingQuoteRequest Request(
        string service,
        string? option,
        ShippingOptionMatchMode mode = ShippingOptionMatchMode.StrictOwnedOption,
        ShippingMeasuredParcel? parcel = null) =>
        new(service, new DestinationIdentifiers(Country, Area), 1_000_000m, "IRR", option, parcel)
        {
            OptionMatchMode = mode,
        };

    private static ShippingRatePolicy Policy(
        string service,
        decimal charge,
        int lead,
        string? option,
        ShippingRateCondition? condition = null,
        ShippingDestinationScope? scope = null) =>
        ShippingRatePolicy.Create(
            service,
            scope ?? AreaScope,
            charge,
            "IRR",
            1,
            lead,
            null,
            Now,
            ratePolicyId: null,
            optionCode: option,
            condition: condition);

    private static ShippingCoverageGrant Grant(string service) =>
        ShippingCoverageGrant.Create(service, AreaScope, Now);

    // (a) A provider-level row alone must never price an option-specific method in strict mode.
    [Fact]
    public void Strict_mode_refuses_provider_level_row_as_a_method_price()
    {
        var policies = new[] { Policy("tipax", 185_000m, 3, null) };
        var grants = new[] { Grant("tipax") };

        var strict = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);

        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, strict.Outcome);
        Assert.False(strict.IsQuoted);
        Assert.Equal(ShippingReasonCodes.OwnedOptionNotPriced, strict.ReasonCode);
        Assert.Equal(0m, strict.ChargeAmount);
        Assert.Null(strict.CurrencyCode);
    }

    // (b) A sibling option row must never price the requested option in strict mode.
    [Fact]
    public void Strict_mode_never_lets_a_sibling_option_price_this_option()
    {
        var policies = new[] { Policy("tipax", 260_000m, 1, "express") };
        var grants = new[] { Grant("tipax") };

        var strict = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);

        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, strict.Outcome);
        Assert.False(strict.IsQuoted);
        Assert.Equal(ShippingReasonCodes.OwnedOptionNotPriced, strict.ReasonCode);
    }

    // (c) An owned row prices exactly its own option and nothing else.
    [Fact]
    public void Strict_mode_prices_the_owned_option_and_never_the_sibling()
    {
        var policies = new[]
        {
            Policy("tipax", 205_000m, 3, "standard"),
            Policy("tipax", 260_000m, 1, "express"),
        };
        var grants = new[] { Grant("tipax") };

        var standard = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);
        var express = ShippingRatePolicyEngine.Quote(Request("tipax", "express"), policies, grants);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, standard.Outcome);
        Assert.Equal(205_000m, standard.ChargeAmount);
        Assert.Equal(ShippingReasonCodes.Quoted, standard.ReasonCode);
        Assert.Equal("standard", standard.OptionCode);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, express.Outcome);
        Assert.Equal(260_000m, express.ChargeAmount);
    }

    // (d) Mixed shared + owned: strict and legacy both choose the owned row, but they differ on the
    // option that has no owned row — strict refuses, legacy falls back (documented, unchanged).
    [Fact]
    public void Mixed_shared_and_owned_rows_keep_owned_precedence_and_diverge_only_on_legacy_fallback()
    {
        var policies = new[]
        {
            Policy("tipax", 185_000m, 3, null),
            Policy("tipax", 205_000m, 3, "standard"),
        };
        var grants = new[] { Grant("tipax") };

        var strictStandard = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);
        var legacyStandard = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", ShippingOptionMatchMode.LegacyFallback),
            policies,
            grants);

        var strictExpress = ShippingRatePolicyEngine.Quote(Request("tipax", "express"), policies, grants);
        var legacyExpress = ShippingRatePolicyEngine.Quote(
            Request("tipax", "express", ShippingOptionMatchMode.LegacyFallback),
            policies,
            grants);

        Assert.Equal(205_000m, strictStandard.ChargeAmount);
        Assert.Equal(205_000m, legacyStandard.ChargeAmount);

        Assert.False(strictExpress.IsQuoted);
        Assert.Equal(ShippingReasonCodes.OwnedOptionNotPriced, strictExpress.ReasonCode);

        // The legacy outcome is untouched: this is the exact TB-159 defect, preserved on purpose for
        // callers that did not opt in.
        Assert.Equal(185_000m, legacyExpress.ChargeAmount);
    }

    // (e) Strict without an owned coverage grant fails closed instead of pricing.
    [Fact]
    public void Strict_mode_without_coverage_fails_closed()
    {
        var policies = new[] { Policy("tipax", 205_000m, 3, "standard") };

        var strict = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, []);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, strict.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, strict.ReasonCode);
        Assert.False(strict.IsQuoted);
    }

    // (f) A conditioned strict-owned row without measurements fails closed, and still does with them.
    [Fact]
    public void Strict_mode_conditioned_row_without_measurement_fails_closed_and_quotes_with_it()
    {
        var policies = new[]
        {
            Policy("tipax", 205_000m, 3, "standard", ShippingRateCondition.Unconditioned with
            {
                Weight = ShippingNumericTier.Between(0, 5_000),
            }),
        };
        var grants = new[] { Grant("tipax") };

        var withoutMeasurement = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), policies, grants);
        var withMeasurement = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", parcel: ShippingMeasuredParcel.WithWeight(2_000)),
            policies,
            grants);

        Assert.Equal(ShippingServiceabilityOutcome.MeasurementRequired, withoutMeasurement.Outcome);
        Assert.Equal(ShippingReasonCodes.MeasurementRequired, withoutMeasurement.ReasonCode);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, withMeasurement.Outcome);
        Assert.Equal(205_000m, withMeasurement.ChargeAmount);
    }

    // (g) Optionless methods and legacy requests stay byte-compatible.
    [Fact]
    public void Optionless_methods_and_legacy_requests_are_unchanged()
    {
        var serviceLevel = new[] { Policy("post", 120_000m, 4, null) };
        var grants = new[] { Grant("post") };

        var optionless = ShippingRatePolicyEngine.Quote(Request("post", null), serviceLevel, grants);
        var optionlessLegacy = ShippingRatePolicyEngine.Quote(
            Request("post", null, ShippingOptionMatchMode.LegacyFallback),
            serviceLevel,
            grants);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, optionless.Outcome);
        Assert.Equal(120_000m, optionless.ChargeAmount);
        Assert.Equal(optionless, optionlessLegacy);

        // A service with no active rows at all keeps the established "not priced" reason in both modes.
        var unknownStrict = ShippingRatePolicyEngine.Quote(Request("ghost", "standard"), serviceLevel, grants);
        var unknownLegacy = ShippingRatePolicyEngine.Quote(
            Request("ghost", "standard", ShippingOptionMatchMode.LegacyFallback),
            serviceLevel,
            grants);

        Assert.Equal(ShippingReasonCodes.ServiceNotPriced, unknownStrict.ReasonCode);
        Assert.Equal(unknownLegacy.ReasonCode, unknownStrict.ReasonCode);
        Assert.Equal(unknownLegacy.Outcome, unknownStrict.Outcome);
    }

    // (h) Known ambiguity and currency conflicts keep their established fail-closed behaviour in strict.
    [Fact]
    public void Strict_mode_keeps_ambiguity_and_currency_conflicts_fail_closed()
    {
        var ambiguous = new[]
        {
            Policy("tipax", 205_000m, 3, "standard"),
            Policy("tipax", 205_000m, 3, "standard"),
        };
        var grants = new[] { Grant("tipax") };

        var scopeConflict = ShippingRatePolicyEngine.Quote(Request("tipax", "standard"), ambiguous, grants);
        Assert.Equal(ShippingServiceabilityOutcome.ScopeConflict, scopeConflict.Outcome);
        Assert.Equal(ShippingReasonCodes.ScopeAmbiguous, scopeConflict.ReasonCode);

        var conditioned = new[]
        {
            Policy("tipax", 205_000m, 3, "standard", ShippingRateCondition.Unconditioned with
            {
                Weight = ShippingNumericTier.Between(0, 2_000),
            }),
            Policy("tipax", 205_000m, 3, "standard", ShippingRateCondition.Unconditioned with
            {
                Weight = ShippingNumericTier.Between(1_500, 4_000),
            }),
        };
        var tierConflict = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", parcel: ShippingMeasuredParcel.WithWeight(1_800)),
            conditioned,
            grants);
        Assert.Equal(ShippingServiceabilityOutcome.ConditionConflict, tierConflict.Outcome);
        Assert.Equal(ShippingReasonCodes.ConditionAmbiguous, tierConflict.ReasonCode);

        var currencyMismatch = ShippingRatePolicyEngine.Quote(
            new ShippingQuoteRequest(
                "tipax",
                new DestinationIdentifiers(Country, Area),
                1_000_000m,
                "USD",
                "standard")
            {
                OptionMatchMode = ShippingOptionMatchMode.StrictOwnedOption,
            },
            new[] { Policy("tipax", 205_000m, 3, "standard") },
            grants);
        Assert.Equal(ShippingServiceabilityOutcome.CurrencyMismatch, currencyMismatch.Outcome);
        Assert.Equal(ShippingReasonCodes.CurrencyMismatch, currencyMismatch.ReasonCode);
    }

    // The strict flag must survive the read adapter (SQL pre-filter + engine) with rows loaded from a
    // real context: a provider-level row persisted in the DB must not price a strict option request.
    [Fact]
    public async Task Strict_flag_survives_the_rate_quote_lookup_adapter()
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-160-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ShippingDbContext(options);

        context.RatePolicies.Add(Policy("tipax", 185_000m, 3, null, scope: CountryScope));
        context.RatePolicies.Add(Policy("tipax", 205_000m, 3, "standard", scope: CountryScope));
        context.CoverageGrants.Add(ShippingCoverageGrant.Create("tipax", CountryScope, Now));
        await context.SaveChangesAsync();

        var lookup = new ShippingRateQuoteLookup(context);
        var strictStandard = await lookup.QuoteAsync(
            new ShippingQuoteRequest(
                "tipax",
                new DestinationIdentifiers(Country, null, null),
                1_000_000m,
                "IRR",
                "standard")
            {
                OptionMatchMode = ShippingOptionMatchMode.StrictOwnedOption,
            },
            CancellationToken.None);
        var strictExpress = await lookup.QuoteAsync(
            new ShippingQuoteRequest(
                "tipax",
                new DestinationIdentifiers(Country, null, null),
                1_000_000m,
                "IRR",
                "express")
            {
                OptionMatchMode = ShippingOptionMatchMode.StrictOwnedOption,
            },
            CancellationToken.None);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, strictStandard.Outcome);
        Assert.Equal(205_000m, strictStandard.ChargeAmount);

        Assert.False(strictExpress.IsQuoted);
        Assert.Equal(ShippingReasonCodes.OwnedOptionNotPriced, strictExpress.ReasonCode);
    }
}
