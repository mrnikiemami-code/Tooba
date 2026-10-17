using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Rates;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// TB-SHIPPING-OWNER-OPTION-COVERAGE-161 — تحدید جغرافیایی مستقل هر روش canonical.
/// </summary>
/// <remarks>
/// <para>
/// این آزمون‌ها مرزهای زیر را روی موتور/آداپتور/درز نوشتن واقعی اثبات می‌کنند:
/// <list type="bullet">
/// <item>روش محدودشده فقط با مجوزهای <em>همان روش</em> سنجیده می‌شود؛</item>
/// <item>روش بدون اعلام صریح، «بدون محدودیت» است اما بدون نرخ مالک‌شده هم قیمت نمی‌گیرد؛</item>
/// <item>مجوز سطح‌خدمت (provider-level) هرگز روش گزینه‌دار را مجاز نمی‌کند؛</item>
/// <item>مجموعهٔ مجاز خالی به «هیچ مقصدی» تفسیر می‌شود، نه «بدون محدودیت»؛</item>
/// <item>مسیر پیش‌فرض legacy و ردیف‌های سطح‌خدمت بیت‌به‌بیت دست‌نخورده می‌مانند.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ShippingMethodCoverageRestrictionTests
{
    private static readonly Guid Country = Guid.Parse("e3451206-e503-51a8-91ad-afa9ab70e330");
    private static readonly Guid Area = Guid.Parse("1f8050a8-c325-59fa-959f-82d76b5b2bc5");
    private static readonly Guid CityX = Guid.Parse("c841be1e-0f47-54aa-8be8-a7b5c33f6f64");
    private static readonly Guid CityY = Guid.Parse("3bfdb19d-20e2-5f04-a4ab-43b8a6d9d2c0");
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 0, 30, 0, TimeSpan.Zero);

    private static ShippingDestinationScope CountryScope => ShippingDestinationScope.ForCountry(Country);

    private static ShippingDestinationScope CityScope(Guid city) => ShippingDestinationScope.ForCity(Country, Area, city);

    private static ShippingQuoteRequest Request(
        string service,
        string? option,
        Guid city,
        ShippingOptionMatchMode mode = ShippingOptionMatchMode.StrictOwnedOption,
        ShippingMeasuredParcel? parcel = null) =>
        new(service, new DestinationIdentifiers(Country, Area, city), 1_000_000m, "IRR", option, parcel)
        {
            OptionMatchMode = mode,
        };

    private static ShippingRatePolicy Policy(
        string service,
        decimal charge,
        string? option,
        ShippingRateCondition? condition = null) =>
        ShippingRatePolicy.Create(
            service,
            CountryScope,
            charge,
            "IRR",
            1,
            2,
            null,
            Now,
            ratePolicyId: null,
            optionCode: option,
            condition: condition);

    private static ShippingCoverageGrant Grant(string service, ShippingDestinationScope scope, string? option = null) =>
        ShippingCoverageGrant.Create(service, scope, Now, coverageGrantId: null, optionCode: option);

    private static ShippingMethodCoverage Declare(
        string service,
        string? option,
        ShippingCoverageMode mode) =>
        ShippingMethodCoverage.Create(service, option, mode, Now);

    // (a) A method's own restriction denies a destination it does not grant.
    [Fact]
    public void Restricted_method_is_denied_outside_its_own_allowed_city()
    {
        var policies = new[] { Policy("tipax", 185_000m, "standard") };
        var grants = new[] { Grant("tipax", CityScope(CityX), "standard") };
        var declarations = new[] { Declare("tipax", "standard", ShippingCoverageMode.Restricted) };

        var quote = ShippingRatePolicyEngine.Quote(Request("tipax", "standard", CityY), policies, grants, declarations);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
        Assert.False(quote.IsQuoted);
        Assert.Equal(ShippingReasonCodes.CoverageMethodRestricted, quote.ReasonCode);
        Assert.Equal(0m, quote.ChargeAmount);
        Assert.Null(quote.CurrencyCode);
    }

    // (b) The same method is allowed inside its own allowed city.
    [Fact]
    public void Restricted_method_is_quoted_inside_its_own_allowed_city()
    {
        var policies = new[] { Policy("tipax", 185_000m, "standard") };
        var grants = new[] { Grant("tipax", CityScope(CityX), "standard") };
        var declarations = new[] { Declare("tipax", "standard", ShippingCoverageMode.Restricted) };

        var quote = ShippingRatePolicyEngine.Quote(Request("tipax", "standard", CityX), policies, grants, declarations);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, quote.Outcome);
        Assert.Equal(185_000m, quote.ChargeAmount);
        Assert.Equal("IRR", quote.CurrencyCode);
        Assert.Equal("standard", quote.OptionCode);
    }

    // (c) A sibling's restriction never narrows an unrestricted method: express stays quotable in CityY.
    [Fact]
    public void Sibling_restriction_never_narrows_an_unrestricted_method()
    {
        var policies = new[]
        {
            Policy("tipax", 185_000m, "standard"),
            Policy("tipax", 260_000m, "express"),
        };
        var grants = new[] { Grant("tipax", CityScope(CityX), "standard") };
        var declarations = new[] { Declare("tipax", "standard", ShippingCoverageMode.Restricted) };

        var express = ShippingRatePolicyEngine.Quote(Request("tipax", "express", CityY), policies, grants, declarations);
        var standard = ShippingRatePolicyEngine.Quote(Request("tipax", "standard", CityY), policies, grants, declarations);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, express.Outcome);
        Assert.Equal(260_000m, express.ChargeAmount);
        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, standard.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMethodRestricted, standard.ReasonCode);
    }

    // (d) Provider-level legacy grants never authorize an option-specific strict method.
    [Fact]
    public void Provider_level_grant_never_authorizes_a_restricted_option_method()
    {
        var policies = new[] { Policy("tipax", 185_000m, "standard") };
        var grants = new[] { Grant("tipax", CountryScope) };
        var declarations = new[] { Declare("tipax", "standard", ShippingCoverageMode.Restricted) };

        var quote = ShippingRatePolicyEngine.Quote(Request("tipax", "standard", CityX), policies, grants, declarations);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMethodRestricted, quote.ReasonCode);
    }

    // (e) An empty allowed set means "nowhere", never "unrestricted".
    [Fact]
    public void Restricted_method_with_zero_grants_denies_every_destination()
    {
        var policies = new[] { Policy("tipax", 185_000m, "standard") };
        var declarations = new[] { Declare("tipax", "standard", ShippingCoverageMode.Restricted) };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", CityX),
            policies,
            Array.Empty<ShippingCoverageGrant>(),
            declarations);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMethodRestricted, quote.ReasonCode);
    }

    // (f) No declaration = no Admin restriction, but pricing readiness is still mandatory.
    [Fact]
    public void Missing_declaration_is_unrestricted_but_still_needs_an_owned_rate()
    {
        var grants = new[] { Grant("tipax", CityScope(CityX), "express") };

        var unpriced = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", CityY),
            new[] { Policy("tipax", 260_000m, "express") },
            grants,
            Array.Empty<ShippingMethodCoverage>());

        Assert.False(unpriced.IsQuoted);
        Assert.Equal(ShippingReasonCodes.OwnedOptionNotPriced, unpriced.ReasonCode);
        Assert.Equal(0m, unpriced.ChargeAmount);
        Assert.Null(unpriced.CurrencyCode);

        var priced = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", CityY),
            new[] { Policy("tipax", 185_000m, "standard") },
            grants,
            Array.Empty<ShippingMethodCoverage>());

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, priced.Outcome);
    }

    // (g) Unrestricted never rescues a conditioned rate that lacks trusted measurement.
    [Fact]
    public void Unrestricted_method_keeps_conditioned_rate_fail_closed_without_measurement()
    {
        var conditions = new ShippingRateCondition(
            ShippingNumericTier.Between(1_000, 2_000),
            null);
        var policies = new[] { Policy("tipax", 185_000m, "standard", conditions) };
        var declarations = new[] { Declare("tipax", "standard", ShippingCoverageMode.Unrestricted) };

        var quote = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", CityX),
            policies,
            Array.Empty<ShippingCoverageGrant>(),
            declarations);

        Assert.Equal(ShippingServiceabilityOutcome.MeasurementRequired, quote.Outcome);
        Assert.Equal(ShippingReasonCodes.MeasurementRequired, quote.ReasonCode);
        Assert.False(quote.IsQuoted);
    }

    // (h) Default legacy behaviour is untouched: option-scoped grants neither widen nor narrow it.
    [Fact]
    public void Legacy_default_ignores_option_scoped_grants_and_still_requires_a_service_level_grant()
    {
        var policies = new[] { Policy("tipax", 185_000m, null) };
        var optionOnly = new[] { Grant("tipax", CountryScope, "standard") };

        var withoutServiceGrant = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", CityX, ShippingOptionMatchMode.LegacyFallback),
            policies,
            optionOnly);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, withoutServiceGrant.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMissing, withoutServiceGrant.ReasonCode);

        var serviceGrant = optionOnly.Append(Grant("tipax", CountryScope)).ToArray();
        var legacy = ShippingRatePolicyEngine.Quote(
            Request("tipax", "standard", CityX, ShippingOptionMatchMode.LegacyFallback),
            policies,
            serviceGrant);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, legacy.Outcome);
        Assert.Equal(185_000m, legacy.ChargeAmount);
    }

    // (i) An Unrestricted declaration does not silently borrow a sibling's allowed set either.
    [Fact]
    public void Unrestricted_method_is_quoted_even_where_a_sibling_is_restricted()
    {
        var policies = new[]
        {
            Policy("tipax", 185_000m, "standard"),
            Policy("tipax", 260_000m, "express"),
        };
        var grants = new[] { Grant("tipax", CityScope(CityX), "standard") };
        var declarations = new[]
        {
            Declare("tipax", "standard", ShippingCoverageMode.Restricted),
            Declare("tipax", "express", ShippingCoverageMode.Unrestricted),
        };

        var express = ShippingRatePolicyEngine.Quote(Request("tipax", "express", CityY), policies, grants, declarations);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, express.Outcome);
        Assert.Equal(260_000m, express.ChargeAmount);
    }

    // (j) The service-level declaration restricts the optionless method only.
    [Fact]
    public void Service_level_declaration_does_not_restrict_an_option_method()
    {
        var policies = new[]
        {
            Policy("tipax", 185_000m, null),
            Policy("tipax", 260_000m, "express"),
        };
        var grants = new[] { Grant("tipax", CityScope(CityX)) };
        var declarations = new[] { Declare("tipax", null, ShippingCoverageMode.Restricted) };

        var express = ShippingRatePolicyEngine.Quote(Request("tipax", "express", CityY), policies, grants, declarations);
        var optionless = ShippingRatePolicyEngine.Quote(Request("tipax", null, CityY), policies, grants, declarations);

        Assert.Equal(ShippingServiceabilityOutcome.Quoted, express.Outcome);
        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, optionless.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMethodRestricted, optionless.ReasonCode);
    }

    // (k) Persisted declaration + option-scoped grant are what the quote adapter actually reads.
    [Fact]
    public async Task Persisted_restriction_is_enforced_by_the_quote_adapter()
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-161-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ShippingDbContext(options);
        var directory = new ShippingRateCoverageDirectory(context);

        var rate = await directory.CreateRateAsync(
            "tipax",
            new ShippingRatePolicyWriteRequest(
                "tipax",
                new ShippingDestinationScopeWrite(Country),
                185_000m,
                "IRR",
                1,
                2,
                OptionCode: "standard"),
            CancellationToken.None);
        Assert.True(rate.IsSuccess);

        var grant = await directory.CreateGrantAsync(
            "tipax",
            new ShippingCoverageGrantWriteRequest(
                "tipax",
                new ShippingDestinationScopeWrite(Country, Area, CityX),
                "standard"),
            CancellationToken.None);
        Assert.True(grant.IsSuccess);
        Assert.Equal("standard", grant.Value.OptionCode);

        var declared = await directory.SetMethodCoverageAsync(
            "tipax",
            new ShippingMethodCoverageWriteRequest("standard", ShippingCoverageMode.Restricted),
            CancellationToken.None);
        Assert.True(declared.IsSuccess);
        Assert.Equal(ShippingCoverageMode.Restricted, declared.Value.Mode);

        // Re-writing the same method edits the same row instead of creating a second declaration.
        var again = await directory.SetMethodCoverageAsync(
            "tipax",
            new ShippingMethodCoverageWriteRequest("STANDARD", ShippingCoverageMode.Restricted),
            CancellationToken.None);
        Assert.True(again.IsSuccess);
        Assert.Equal(declared.Value.MethodCoverageId, again.Value.MethodCoverageId);
        Assert.Equal(1, await context.MethodCoverages.CountAsync());

        var read = await directory.ListAsync("tipax", CancellationToken.None);
        Assert.True(read.IsSuccess);
        Assert.Single(read.Value.MethodCoverages!);
        Assert.Equal("standard", Assert.Single(read.Value.CoverageGrants).OptionCode);

        var lookup = new ShippingRateQuoteLookup(context);
        var denied = await lookup.QuoteAsync(Request("tipax", "standard", CityY), CancellationToken.None);
        var allowed = await lookup.QuoteAsync(Request("tipax", "standard", CityX), CancellationToken.None);

        Assert.Equal(ShippingServiceabilityOutcome.NotCovered, denied.Outcome);
        Assert.Equal(ShippingReasonCodes.CoverageMethodRestricted, denied.ReasonCode);
        Assert.Equal(ShippingServiceabilityOutcome.Quoted, allowed.Outcome);
        Assert.Equal(185_000m, allowed.ChargeAmount);
    }
}
