using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Rates.Queries;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-METHOD-FINANCIAL-EDIT-156 — the real Shipping-owned write seam (the same
/// <see cref="ShippingRateCoverageDirectory"/> the admin endpoints use) can store an OWNED fixed rule
/// for exactly one canonical method, and the TB-155 read model confirms it. Sibling options stay
/// untouched, provider-level legacy rows are never overwritten, and a competing duplicate is denied
/// fail-closed instead of silently normalized.
/// </summary>
public sealed class ShippingMethodFinancialEditAuthorityTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ShippingRateCoverageDirectory Directory(out ShippingDbContext context)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-156-{Guid.NewGuid():N}")
            .Options;
        context = new ShippingDbContext(options);
        return new ShippingRateCoverageDirectory(context);
    }

    private static ShippingRatePolicyWriteRequest Request(
        string serviceCode, decimal charge, string? optionCode, string currency = "IRR")
        => new(
            serviceCode,
            new ShippingDestinationScopeWrite(CountryId, AreaId, CityId),
            charge,
            currency,
            2,
            3,
            null,
            optionCode);

    private static async Task<ShippingMethodFinancialStatusView> ReadAsync(
        ShippingRateCoverageDirectory directory, string serviceCode, string? optionCode)
    {
        var result = await new GetShippingMethodFinancialStatusHandler(directory)
            .Handle(new GetShippingMethodFinancialStatusQuery(serviceCode, optionCode), default);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public async Task Saving_a_fixed_IRR_rule_for_one_method_reads_back_as_its_own_proven_amount()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateRateAsync("tipax", Request("tipax", 185000m, "standard"), default);
        Assert.True(created.IsSuccess);
        Assert.Equal(185000m, created.Value.ChargeAmount);
        Assert.Equal("IRR", created.Value.CurrencyCode);
        Assert.Equal("standard", created.Value.OptionCode);

        var status = await ReadAsync(directory, "tipax", "standard");
        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, status.State);
        Assert.Equal(185000m, Assert.Single(status.Rules).ChargeAmount);
        Assert.Equal("IRR", status.CurrencyCode);

        // Sibling isolation: the same provider's other option is still unconfigured, never sharing this money.
        var sibling = await ReadAsync(directory, "tipax", "express");
        Assert.Equal(ShippingMethodFinancialState.NeedsConfiguration, sibling.State);
        Assert.Empty(sibling.Rules);
        Assert.Equal(1, sibling.ActiveSiblingOptionRuleCount);
    }

    [Fact]
    public async Task Editing_the_owned_amount_changes_only_that_method_and_is_visible_after_read_back()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateRateAsync("post", Request("post", 100000m, "standard"), default);
        Assert.True(created.IsSuccess);

        var updated = await directory.UpdateRateAsync(
            created.Value.RatePolicyId,
            "post",
            Request("post", 142000m, "standard"),
            default);
        Assert.True(updated.IsSuccess);
        Assert.Equal(142000m, updated.Value.ChargeAmount);

        var status = await ReadAsync(directory, "post", "standard");
        Assert.Equal(142000m, Assert.Single(status.Rules).ChargeAmount);
        // The other post option was never created and is not invented by the edit.
        Assert.Equal(ShippingMethodFinancialState.NeedsConfiguration, (await ReadAsync(directory, "post", "express")).State);
    }

    [Fact]
    public async Task Creating_an_owned_rule_preserves_the_legacy_service_level_row_untouched()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var legacy = await directory.CreateRateAsync("tipax", Request("tipax", 99000m, null), default);
        Assert.True(legacy.IsSuccess);
        Assert.Equal(ShippingMethodFinancialState.LegacySharedUnresolved, (await ReadAsync(directory, "tipax", "standard")).State);

        var owned = await directory.CreateRateAsync("tipax", Request("tipax", 210000m, "standard"), default);
        Assert.True(owned.IsSuccess);

        // The legacy row is neither edited nor deleted: it stays readable, and only the owned row is this method's money.
        var status = await ReadAsync(directory, "tipax", "standard");
        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, status.State);
        Assert.Equal(210000m, Assert.Single(status.Rules).ChargeAmount);
        Assert.Equal(1, status.ActiveServiceLevelRuleCount);
        Assert.NotEqual(legacy.Value.RatePolicyId, status.Rules[0].RatePolicyId);
    }

    [Fact]
    public async Task A_competing_identical_scope_is_denied_fail_closed_instead_of_overwriting()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync("tipax", Request("tipax", 210000m, "standard"), default)).IsSuccess);

        var duplicate = await directory.CreateRateAsync("tipax", Request("tipax", 260000m, "standard"), default);
        Assert.True(duplicate.IsFailure);

        var status = await ReadAsync(directory, "tipax", "standard");
        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, status.State);
        Assert.Equal(210000m, Assert.Single(status.Rules).ChargeAmount);
        Assert.Equal(1, status.RuleCount);
    }

    [Fact]
    public async Task An_owned_rule_is_never_attributed_to_the_optionless_legacy_identity()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync("tipax", Request("tipax", 175000m, "standard"), default)).IsSuccess);

        // Asking for the service-level identity (no option) must not borrow the option-owned row.
        var serviceLevel = await ReadAsync(directory, "tipax", null);
        Assert.Equal(ShippingMethodFinancialState.NeedsConfiguration, serviceLevel.State);
        Assert.Empty(serviceLevel.Rules);
        Assert.Equal(1, serviceLevel.ActiveSiblingOptionRuleCount);
    }
}
