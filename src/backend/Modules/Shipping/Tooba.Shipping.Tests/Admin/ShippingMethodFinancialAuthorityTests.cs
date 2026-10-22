using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Rates.Queries;
using Tooba.Shipping.Application.Rates.ReadModels;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155 — سرور، تنها مرجع وضعیت مالی یک روش canonical است.
///
/// این تست‌ها روی ارزیابی واقعی سرور (ردیف‌های واقعی نرخ/پوشش در context واقعی Shipping) اجرا
/// می‌شوند، نه روی نشان «آماده» ساختگی: جداسازی گزینهٔ هم‌خانواده، ردیف مشترک قدیمی، نبود داده ≠
/// رایگان، مبلغ صفر ≠ حالت رایگان و ابهام حل‌نشده همه اینجا اثبات می‌شوند.
/// </summary>
public sealed class ShippingMethodFinancialAuthorityTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ShippingRateCoverageDirectory Directory(out ShippingDbContext context)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-155-{Guid.NewGuid():N}")
            .Options;
        context = new ShippingDbContext(options);
        return new ShippingRateCoverageDirectory(context);
    }

    private static ShippingRatePolicyWriteRequest RateRequest(
        string serviceCode,
        decimal charge = 25000m,
        string? optionCode = null,
        ShippingDestinationScopeWrite? scope = null,
        string currency = "IRR",
        ShippingRateConditionWrite? condition = null)
        => new(
            serviceCode,
            scope ?? new ShippingDestinationScopeWrite(CountryId, AreaId, CityId),
            charge,
            currency,
            2,
            3,
            null,
            optionCode,
            condition);

    private static async Task<ShippingMethodFinancialStatusView> ReadAsync(
        ShippingRateCoverageDirectory directory,
        string serviceCode,
        string? optionCode)
    {
        var handler = new GetShippingMethodFinancialStatusHandler(directory);
        var result = await handler.Handle(new GetShippingMethodFinancialStatusQuery(serviceCode, optionCode), default);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public async Task An_owned_active_rule_is_reported_as_fixed_amount_with_its_own_currency()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateRateAsync("post", RateRequest("post", 32000m, "standard"), default);
        Assert.True(created.IsSuccess);

        var status = await ReadAsync(directory, "post", "standard");

        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, status.State);
        Assert.Equal(ShippingMethodFinancialMode.FixedAmount, status.Mode);
        Assert.Equal("IRR", status.CurrencyCode);
        Assert.Equal(1, status.RuleCount);
        Assert.Equal(32000m, Assert.Single(status.Rules).ChargeAmount);
        Assert.Equal(ShippingMethodFinancialReasonCodes.OwnedRulesPresent, status.ReasonCode);
    }

    [Fact]
    public async Task Sibling_option_rates_never_leak_between_tipax_express_and_tipax_standard()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var standard = await directory.CreateRateAsync("tipax", RateRequest("tipax", 41000m, "standard"), default);
        Assert.True(standard.IsSuccess);

        var expressOnly = await ReadAsync(directory, "tipax", "express");
        Assert.Equal(ShippingMethodFinancialState.NeedsConfiguration, expressOnly.State);
        Assert.Equal(ShippingMethodFinancialMode.None, expressOnly.Mode);
        Assert.Null(expressOnly.CurrencyCode);
        Assert.Empty(expressOnly.Rules);
        Assert.Equal(0, expressOnly.RuleCount);
        // The sibling row exists and is COUNTED so the operator can see why nothing is attributed here.
        Assert.Equal(1, expressOnly.ActiveSiblingOptionRuleCount);
        Assert.Equal(ShippingMethodFinancialReasonCodes.NoOwnedRules, expressOnly.ReasonCode);

        var standardOnly = await ReadAsync(directory, "tipax", "standard");
        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, standardOnly.State);
        Assert.Equal(41000m, Assert.Single(standardOnly.Rules).ChargeAmount);
        // No other option of this service is priced, so nothing sibling-related can be counted.
        Assert.Equal(0, standardOnly.ActiveSiblingOptionRuleCount);
    }

    [Fact]
    public async Task Post_standard_and_post_express_are_evaluated_independently()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync(
            "post", RateRequest("post", 18000m, "standard"), default)).IsSuccess);
        Assert.True((await directory.CreateRateAsync(
            "post", RateRequest("post", 49000m, "express"), default)).IsSuccess);

        var standard = await ReadAsync(directory, "post", "standard");
        var express = await ReadAsync(directory, "post", "express");

        Assert.Equal(18000m, Assert.Single(standard.Rules).ChargeAmount);
        Assert.Equal(49000m, Assert.Single(express.Rules).ChargeAmount);
        Assert.DoesNotContain(express.Rules, r => r.RatePolicyId == Assert.Single(standard.Rules).RatePolicyId);
    }

    [Fact]
    public async Task A_service_level_legacy_row_is_not_the_tariff_of_an_option_bearing_method()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        // The historical provider-level tariff: service code only, no option identity.
        Assert.True((await directory.CreateRateAsync(
            "tipax", RateRequest("tipax", 99000m, null), default)).IsSuccess);

        var status = await ReadAsync(directory, "tipax", "standard");

        Assert.Equal(ShippingMethodFinancialState.LegacySharedUnresolved, status.State);
        Assert.Equal(ShippingMethodFinancialMode.None, status.Mode);
        // The shared amount is never projected as this method's money.
        Assert.Null(status.CurrencyCode);
        Assert.Empty(status.Rules);
        Assert.Equal(0, status.RuleCount);
        Assert.Equal(1, status.ActiveServiceLevelRuleCount);
        Assert.Equal(ShippingMethodFinancialReasonCodes.LegacyServiceLevelShared, status.ReasonCode);
    }

    [Fact]
    public async Task An_optionless_method_owns_its_service_level_rows_and_is_never_legacy_shared()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync(
            "store_courier", RateRequest("store_courier", 30000m, null), default)).IsSuccess);

        var status = await ReadAsync(directory, "store_courier", null);

        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, status.State);
        Assert.Equal(30000m, Assert.Single(status.Rules).ChargeAmount);
        Assert.Equal(ShippingMethodFinancialReasonCodes.OwnedRulesPresent, status.ReasonCode);
    }

    [Fact]
    public async Task A_missing_tariff_is_never_reported_as_free_or_as_a_zero_amount()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var status = await ReadAsync(directory, "snapp_courier", "default");

        Assert.Equal(ShippingMethodFinancialState.NeedsConfiguration, status.State);
        Assert.Equal(ShippingMethodFinancialMode.None, status.Mode);
        Assert.Null(status.CurrencyCode);
        Assert.Empty(status.Rules);
        Assert.False(status.HasZeroChargeRule);
        Assert.Equal(ShippingMethodFinancialReasonCodes.NoOwnedRules, status.ReasonCode);
    }

    [Fact]
    public async Task An_active_method_without_any_coverage_grant_stays_configured_but_not_quoted_ready()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync(
            "in_person", RateRequest("in_person", 12000m, "default"), default)).IsSuccess);

        var withoutCoverage = await ReadAsync(directory, "in_person", "default");
        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, withoutCoverage.State);
        Assert.Equal(0, withoutCoverage.ActiveCoverageGrantCount);

        Assert.True((await directory.CreateGrantAsync(
            "in_person",
            new ShippingCoverageGrantWriteRequest(
                "in_person",
                new ShippingDestinationScopeWrite(CountryId, AreaId, CityId)),
            default)).IsSuccess);

        var withCoverage = await ReadAsync(directory, "in_person", "default");
        Assert.Equal(1, withCoverage.ActiveCoverageGrantCount);
        Assert.Equal(12000m, Assert.Single(withCoverage.Rules).ChargeAmount);
    }

    [Fact]
    public async Task A_stored_zero_amount_is_a_data_fact_and_never_an_approved_free_mode()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync(
            "post", RateRequest("post", 0m, "standard"), default)).IsSuccess);

        var status = await ReadAsync(directory, "post", "standard");

        // The schema has no explicit FREE mode, so a zero amount is reported as stored data only:
        // the reader must not translate it into a free policy of its own.
        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, status.State);
        Assert.Equal(ShippingMethodFinancialMode.FixedAmount, status.Mode);
        Assert.True(status.HasZeroChargeRule);
        Assert.DoesNotContain(
            Enum.GetNames<ShippingMethodFinancialMode>(),
            name => name.Contains("Free", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Two_equally_specific_owned_rules_are_reported_as_an_unresolved_conflict()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        // Legacy duplicate rows for the SAME method/scope/condition cannot be authored through the
        // guarded seam any more, so they are seeded directly: pre-existing data must still be
        // reported as unresolved instead of silently picking the first row.
        var scope = new ShippingDestinationScope(CountryId, AreaId, CityId);
        var now = DateTimeOffset.UnixEpoch;
        context.RatePolicies.Add(ShippingRatePolicy.Create(
            "tipax", scope, 20000m, "IRR", 2, 3, null, now, Guid.NewGuid(), "standard"));
        context.RatePolicies.Add(ShippingRatePolicy.Create(
            "tipax", scope, 21000m, "IRR", 2, 3, null, now, Guid.NewGuid(), "standard"));
        await context.SaveChangesAsync();

        var status = await ReadAsync(directory, "tipax", "standard");

        Assert.Equal(ShippingMethodFinancialState.Conflicting, status.State);
        Assert.Equal(ShippingMethodFinancialReasonCodes.AmbiguousOwnedScope, status.ReasonCode);
        Assert.Null(status.CurrencyCode);
        Assert.Equal(2, status.RuleCount);
    }

    [Fact]
    public async Task Owned_rules_in_two_different_currencies_are_reported_as_an_unresolved_conflict()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var tehran = new ShippingDestinationScopeWrite(CountryId, AreaId, CityId);
        var shiraz = new ShippingDestinationScopeWrite(CountryId, AreaId, Guid.NewGuid());
        Assert.True((await directory.CreateRateAsync(
            "tipax", RateRequest("tipax", 20000m, "standard", tehran, "IRR"), default)).IsSuccess);
        Assert.True((await directory.CreateRateAsync(
            "tipax", RateRequest("tipax", 15m, "standard", shiraz, "USD"), default)).IsSuccess);

        var status = await ReadAsync(directory, "tipax", "standard");

        Assert.Equal(ShippingMethodFinancialState.Conflicting, status.State);
        Assert.Equal(ShippingMethodFinancialReasonCodes.MixedCurrency, status.ReasonCode);
        Assert.Null(status.CurrencyCode);
        Assert.Equal(2, status.RuleCount);
    }

    [Fact]
    public async Task An_owned_rule_with_a_condition_is_counted_without_becoming_a_different_mode()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync(
            "post",
            RateRequest(
                "post",
                9000m,
                "standard",
                condition: new ShippingRateConditionWrite(
                    new ShippingNumericTierWrite(0, 2000),
                    null)),
            default)).IsSuccess);

        var status = await ReadAsync(directory, "post", "standard");

        Assert.Equal(ShippingMethodFinancialState.FinanciallyConfigured, status.State);
        Assert.Equal(1, status.ConditionedRuleCount);
        Assert.True(Assert.Single(status.Rules).IsConditioned);
    }

    [Fact]
    public async Task A_deactivated_owned_rule_does_not_configure_the_method()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateRateAsync("post", RateRequest("post", 15000m, "standard"), default);
        Assert.True(created.IsSuccess);
        Assert.True((await directory.SetRateActiveAsync(
            created.Value.RatePolicyId, "post", false, default)).IsSuccess);

        var status = await ReadAsync(directory, "post", "standard");

        Assert.Equal(ShippingMethodFinancialState.NeedsConfiguration, status.State);
        Assert.Equal(ShippingMethodFinancialReasonCodes.NoOwnedRules, status.ReasonCode);
    }

    [Fact]
    public void The_query_validator_denies_an_empty_service_code_and_an_oversized_option()
    {
        var validator = new GetShippingMethodFinancialStatusQueryValidator();

        var emptyService = validator.Validate(new GetShippingMethodFinancialStatusQuery(string.Empty, null));
        Assert.Contains(
            emptyService.Errors,
            e => e.ErrorCode == ShippingValidationCodes.ServiceCodeRequired);

        var longOption = validator.Validate(new GetShippingMethodFinancialStatusQuery(
            "tipax",
            new string('x', ShippingRatePolicy.OptionCodeMaxLength + 1)));
        Assert.Contains(
            longOption.Errors,
            e => e.ErrorCode == ShippingValidationCodes.OptionCodeTooLong);

        Assert.True(validator.Validate(new GetShippingMethodFinancialStatusQuery("tipax", null)).IsValid);
    }

    [Fact]
    public void The_route_is_read_only_authorized_and_mapped_by_the_module()
    {
        var endpointSource = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "Shipping", "Tooba.Shipping.Endpoints",
            "Admin", "ShippingMethodFinancialStatusEndpoints.cs"));

        Assert.Contains(
            "MapGet(\"/shipping-services/{serviceCode}/financial-status\"",
            endpointSource,
            StringComparison.Ordinal);
        Assert.Contains("ShippingAdminPermissions.View", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ShippingAdminPermissions.Manage", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch", endpointSource, StringComparison.Ordinal);

        var moduleSource = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "Shipping", "Tooba.Shipping.Endpoints",
            "ShippingEndpointModule.cs"));
        Assert.Contains("ShippingMethodFinancialStatusEndpoints.Map(admin);", moduleSource, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
