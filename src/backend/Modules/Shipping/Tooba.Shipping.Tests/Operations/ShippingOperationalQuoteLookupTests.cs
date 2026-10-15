using Microsoft.EntityFrameworkCore;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Infrastructure.Operations;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Operations;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-076 — the read-only operational seam over the real module-owned
/// DbContext: the same data produces the same auditable answer, and the four narrow queries never
/// cross into another module's schema.
/// </summary>
public sealed class ShippingOperationalQuoteLookupTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static ShippingDbContext Context(out ShippingOperationalQuoteLookup lookup)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-076-{Guid.NewGuid():N}")
            .Options;
        var context = new ShippingDbContext(options);
        lookup = new ShippingOperationalQuoteLookup(context);
        return context;
    }

    private static ShippingOperationalQuoteRequest Request(DateTimeOffset? pickupAtUtc = null, long? weight = null)
        => new(
            "tipax",
            new DestinationIdentifiers(CountryId, AreaId),
            500_000m,
            "IRR",
            null,
            weight is null ? null : ShippingMeasuredParcel.WithWeight(weight.Value),
            pickupAtUtc);

    [Fact]
    public async Task Stored_policy_coverage_and_packaging_produce_one_auditable_answer()
    {
        await using var context = Context(out var lookup);

        context.RatePolicies.Add(ShippingRatePolicy.Create(
            "tipax",
            ShippingDestinationScope.ForAdministrativeArea(CountryId, AreaId),
            120_000m,
            "IRR",
            1,
            2,
            null,
            Now));
        context.CoverageGrants.Add(ShippingCoverageGrant.Create(
            "tipax",
            ShippingDestinationScope.ForCountry(CountryId),
            Now));
        context.PackagingPolicies.Add(ShippingPackagingPolicy.Create(
            "tipax",
            ShippingDestinationScope.ForAdministrativeArea(CountryId, AreaId),
            12_000m,
            "IRR",
            ShippingFreightPayerMode.CollectOnDelivery,
            Now));
        await context.SaveChangesAsync();

        var quote = await lookup.QuoteAsync(Request(), default);

        Assert.True(quote.IsQuoted);
        Assert.Equal(120_000m, quote.FreightAmount);
        Assert.Equal(12_000m, quote.PackagingAmount);
        Assert.Equal(132_000m, quote.TotalServiceExpense);
        Assert.Equal(12_000m, quote.AmountPayableAtCheckout);
        Assert.Equal(120_000m, quote.AmountCollectOnDelivery);
        Assert.Equal(ShippingScheduleAvailability.NotConfigured, quote.Schedule.Availability);
        Assert.True(quote.IsOperationallyEligible);
    }

    [Fact]
    public async Task An_empty_schema_is_a_fail_closed_rejection_not_a_zero_price()
    {
        await using var context = Context(out var lookup);

        var quote = await lookup.QuoteAsync(Request(), default);

        Assert.Equal(ShippingOperationalQuoteOutcome.FreightNotQuoted, quote.Outcome);
        Assert.Null(quote.TotalServiceExpense);
        Assert.Null(quote.AmountPayableAtCheckout);
        Assert.False(quote.IsOperationallyEligible);
    }

    [Fact]
    public async Task A_weight_tier_boundary_is_resolved_from_stored_data()
    {
        await using var context = Context(out var lookup);

        context.RatePolicies.Add(ShippingRatePolicy.Create(
            "tipax",
            ShippingDestinationScope.ForAdministrativeArea(CountryId, AreaId),
            100_000m,
            "IRR",
            1,
            2,
            null,
            Now,
            optionCode: null,
            condition: new ShippingRateCondition(Weight: ShippingNumericTier.Between(0, 2_000))));
        context.RatePolicies.Add(ShippingRatePolicy.Create(
            "tipax",
            ShippingDestinationScope.ForAdministrativeArea(CountryId, AreaId),
            200_000m,
            "IRR",
            1,
            2,
            null,
            Now,
            optionCode: null,
            condition: new ShippingRateCondition(Weight: ShippingNumericTier.Between(2_000, 5_000))));
        context.CoverageGrants.Add(ShippingCoverageGrant.Create(
            "tipax",
            ShippingDestinationScope.ForCountry(CountryId),
            Now));
        await context.SaveChangesAsync();

        var at1999 = await lookup.QuoteAsync(Request(weight: 1_999), default);
        var at2000 = await lookup.QuoteAsync(Request(weight: 2_000), default);

        Assert.Equal(100_000m, at1999.FreightAmount);
        Assert.Equal(200_000m, at2000.FreightAmount);
    }

    [Fact]
    public async Task A_stored_schedule_decides_carrier_availability_for_the_explicit_instant()
    {
        await using var context = Context(out var lookup);

        context.RatePolicies.Add(ShippingRatePolicy.Create(
            "tipax",
            ShippingDestinationScope.ForAdministrativeArea(CountryId, AreaId),
            120_000m,
            "IRR",
            1,
            2,
            null,
            Now));
        context.CoverageGrants.Add(ShippingCoverageGrant.Create(
            "tipax",
            ShippingDestinationScope.ForCountry(CountryId),
            Now));
        context.OperatingSchedules.Add(Domain.Scheduling.ShippingOperatingSchedule.Create(
            "tipax",
            "Asia/Tehran",
            [new Domain.Scheduling.ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0))],
            Now));
        await context.SaveChangesAsync();

        // 2026-10-18 is a Sunday; 10:00Z is 13:30 local (inside) and 10:30Z is 14:00 local (outside).
        var inside = await lookup.QuoteAsync(
            Request(new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero)),
            default);
        var outside = await lookup.QuoteAsync(
            Request(new DateTimeOffset(2026, 10, 18, 10, 30, 0, TimeSpan.Zero)),
            default);

        Assert.Equal(ShippingScheduleAvailability.Available, inside.Schedule.Availability);
        Assert.True(inside.IsOperationallyEligible);
        Assert.Equal(ShippingScheduleAvailability.Unavailable, outside.Schedule.Availability);
        // A priced quote outside the carrier window is still priced, but NOT operationally eligible.
        Assert.True(outside.IsQuoted);
        Assert.False(outside.IsOperationallyEligible);
    }

    [Fact]
    public async Task The_lookup_reads_only_this_modules_tables()
    {
        await using var context = Context(out var lookup);

        await lookup.QuoteAsync(Request(), default);

        // Every table the seam touches belongs to the shipping schema; no foreign module type is loaded.
        var model = context.Model;
        var schemas = model.GetEntityTypes().Select(x => x.GetSchema()).Distinct().ToArray();

        Assert.Equal(new[] { "shipping" }, schemas);
    }

    [Fact]
    public void The_evaluation_input_has_no_body_service_code_so_the_route_stays_authoritative()
    {
        // A body that could carry its own service identity would let a client bypass the route; the
        // evaluation contract deliberately has no such property.
        var properties = typeof(ShippingOperationalQuoteEvaluationRequest)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("ServiceCode", properties);
        Assert.Contains("Scope", properties);
        Assert.Contains("RequestedPickupAtUtc", properties);
        Assert.DoesNotContain("AllowAnonymous", properties);
    }

    [Fact]
    public void The_admin_scope_contract_is_reused_so_no_second_scope_shape_exists()
    {
        // Reusing ShippingDestinationScopeWrite keeps one destination-scope shape for the whole module.
        var scopeProperty = typeof(ShippingOperationalQuoteEvaluationRequest)
            .GetProperty(nameof(ShippingOperationalQuoteEvaluationRequest.Scope));

        Assert.NotNull(scopeProperty);
        Assert.Equal(typeof(ShippingDestinationScopeWrite), scopeProperty!.PropertyType);
    }
}
