using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Packaging.Validation;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Infrastructure.Packaging;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Packaging;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-075 — نویسندگی ادمین سیاست بسته‌بندی: رفت‌وبرگشت واقعی، رد تکرار
/// دامنه، دست‌نخورده‌ماندن دامنه/گزینه در ویرایش، و شکل پایدار transport.
/// </summary>
public sealed class ShippingPackagingPolicyAdminTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ShippingPackagingPolicyDirectory Directory(out ShippingDbContext context)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-075-{Guid.NewGuid():N}")
            .Options;
        context = new ShippingDbContext(options);
        return new ShippingPackagingPolicyDirectory(context);
    }

    private static ShippingPackagingPolicyWriteRequest Write(
        decimal fee,
        Guid? cityId = null,
        string? optionCode = null,
        ShippingFreightPayerMode? payer = null) =>
        new(
            "tipax",
            new ShippingDestinationScopeWrite(CountryId, AreaId, cityId),
            fee,
            "IRR",
            payer,
            optionCode);

    [Fact]
    public async Task A_packaging_policy_roundtrips_and_reloads_with_its_payer_mode()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateAsync(
            "tipax",
            Write(12_000m, payer: ShippingFreightPayerMode.CollectOnDelivery),
            default);

        Assert.True(created.IsSuccess);
        Assert.Equal(12_000m, created.Value.FeeAmount);
        Assert.Equal("IRR", created.Value.CurrencyCode);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, created.Value.FreightPayerMode);
        Assert.True(created.Value.IsActive);
        Assert.Null(created.Value.OptionCode);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.True(reloaded.IsSuccess);
        var only = Assert.Single(reloaded.Value.PackagingPolicies);
        Assert.Equal(created.Value.PackagingPolicyId, only.PackagingPolicyId);
        Assert.Equal(12_000m, only.FeeAmount);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, only.FreightPayerMode);
    }

    [Fact]
    public async Task An_omitted_payer_mode_is_persisted_as_the_prepaid_default()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateAsync("tipax", Write(12_000m), default);

        Assert.True(created.IsSuccess);
        Assert.Equal(ShippingFreightPayerMode.Prepaid, created.Value.FreightPayerMode);
    }

    [Fact]
    public async Task A_duplicate_scope_for_the_same_service_and_option_is_refused_before_any_write()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateAsync("tipax", Write(12_000m), default)).IsSuccess);

        var duplicate = await directory.CreateAsync("tipax", Write(9_000m), default);

        Assert.True(duplicate.IsFailure);
        Assert.Equal(ShippingErrorCodes.PackagingPolicyScopeDuplicate, duplicate.FirstError.Code);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.Single(reloaded.Value.PackagingPolicies);
    }

    [Fact]
    public async Task The_same_scope_on_a_different_option_is_a_distinct_identity()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var serviceLevel = await directory.CreateAsync("tipax", Write(5_000m), default);
        var express = await directory.CreateAsync("tipax", Write(9_000m, optionCode: "express"), default);

        Assert.True(serviceLevel.IsSuccess);
        Assert.True(express.IsSuccess);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.Equal(2, reloaded.Value.PackagingPolicies.Count);
    }

    [Fact]
    public async Task The_same_option_and_service_on_a_deeper_scope_is_a_distinct_identity()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateAsync("tipax", Write(5_000m), default)).IsSuccess);
        Assert.True((await directory.CreateAsync("tipax", Write(8_000m, cityId: CityId), default)).IsSuccess);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.Equal(2, reloaded.Value.PackagingPolicies.Count);
        Assert.Contains(reloaded.Value.PackagingPolicies, x => x.Level == Contracts.Rates.ShippingDestinationLevel.City);
    }

    [Fact]
    public async Task Editing_updates_fee_currency_and_payer_mode_without_moving_scope_or_option()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateAsync("tipax", Write(12_000m), default);
        Assert.True(created.IsSuccess);

        var updated = await directory.UpdateAsync(
            created.Value.PackagingPolicyId,
            "tipax",
            Write(15_000m, payer: ShippingFreightPayerMode.CollectOnDelivery),
            default);

        Assert.True(updated.IsSuccess);
        Assert.Equal(15_000m, updated.Value.FeeAmount);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, updated.Value.FreightPayerMode);
        Assert.Equal(created.Value.PackagingPolicyId, updated.Value.PackagingPolicyId);
        Assert.Null(updated.Value.OptionCode);
    }

    [Fact]
    public async Task A_cross_service_id_is_refused_instead_of_being_edited_through_another_path()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateAsync("tipax", Write(12_000m), default);
        Assert.True(created.IsSuccess);

        var crossService = await directory.UpdateAsync(
            created.Value.PackagingPolicyId,
            "post",
            Write(15_000m),
            default);

        Assert.True(crossService.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceScopeMismatch, crossService.FirstError.Code);

        var untouched = Assert.Single((await directory.ListAsync("tipax", default)).Value.PackagingPolicies);
        Assert.Equal(12_000m, untouched.FeeAmount);
    }

    [Fact]
    public async Task Re_pointing_an_existing_policy_to_another_option_is_refused()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateAsync("tipax", Write(12_000m, optionCode: "express"), default);
        Assert.True(created.IsSuccess);

        var rePointed = await directory.UpdateAsync(
            created.Value.PackagingPolicyId,
            "tipax",
            Write(15_000m, optionCode: "standard"),
            default);

        Assert.True(rePointed.IsFailure);
        Assert.Equal(ShippingErrorCodes.PackagingPolicyOptionMismatch, rePointed.FirstError.Code);
    }

    [Fact]
    public async Task An_unknown_policy_id_is_reported_with_a_stable_not_found_code()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var missing = await directory.SetActiveAsync(Guid.NewGuid(), "tipax", false, default);

        Assert.True(missing.IsFailure);
        Assert.Equal(ShippingErrorCodes.PackagingPolicyNotFound, missing.FirstError.Code);
    }

    [Fact]
    public async Task Deactivating_and_reactivating_keeps_the_row_and_flips_only_the_flag()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateAsync("tipax", Write(12_000m), default);
        Assert.True(created.IsSuccess);

        var deactivated = await directory.SetActiveAsync(created.Value.PackagingPolicyId, "tipax", false, default);
        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value.IsActive);

        var reactivated = await directory.SetActiveAsync(created.Value.PackagingPolicyId, "tipax", true, default);
        Assert.True(reactivated.IsSuccess);
        Assert.True(reactivated.Value.IsActive);

        Assert.Single((await directory.ListAsync("tipax", default)).Value.PackagingPolicies);
    }

    [Fact]
    public void Transport_shape_invalid_fees_currency_and_payer_mode_are_denied_with_stable_codes()
    {
        var validator = new ShippingPackagingPolicyWriteRequestValidator();

        Assert.Contains(
            validator.Validate(Write(-1m)).Errors,
            e => e.ErrorCode == ShippingValidationCodes.PackagingFeeNegative);

        Assert.Contains(
            validator.Validate(Write(ShippingPackagingPolicy.FeeAmountMaxValue + 1m)).Errors,
            e => e.ErrorCode == ShippingValidationCodes.PackagingFeeOutOfRange);

        Assert.Contains(
            validator.Validate(new ShippingPackagingPolicyWriteRequest(
                "tipax",
                new ShippingDestinationScopeWrite(CountryId, AreaId),
                1m,
                "IR")).Errors,
            e => e.ErrorCode == ShippingValidationCodes.PackagingCurrencyInvalid);

        Assert.Contains(
            validator.Validate(new ShippingPackagingPolicyWriteRequest(
                "tipax",
                new ShippingDestinationScopeWrite(CountryId, AreaId),
                1m,
                "IRR",
                (ShippingFreightPayerMode)42)).Errors,
            e => e.ErrorCode == ShippingValidationCodes.PackagingFreightPayerModeInvalid);

        // The documented defaults stay valid: an omitted payer mode and a zero fee are both accepted.
        Assert.True(validator.Validate(Write(0m)).IsValid);
    }

    [Fact]
    public void A_body_service_code_that_disagrees_with_the_route_is_denied()
    {
        var validator = new CreateShippingPackagingPolicyCommandValidator();

        var mismatched = validator.Validate(new Application.Packaging.Commands.CreateShippingPackagingPolicyCommand(
            "tipax",
            new ShippingPackagingPolicyWriteRequest(
                "post",
                new ShippingDestinationScopeWrite(CountryId, AreaId),
                12_000m,
                "IRR")));

        Assert.Contains(mismatched.Errors, e => e.ErrorCode == ShippingValidationCodes.BodyServiceCodeMismatch);
    }

    [Fact]
    public async Task An_empty_service_code_list_is_a_real_zero_answer_not_a_failure()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var empty = await directory.ListAsync("post", default);

        Assert.True(empty.IsSuccess);
        Assert.Empty(empty.Value.PackagingPolicies);
    }
}
