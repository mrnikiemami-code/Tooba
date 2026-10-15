using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-073 — نویسندگی ادمین پله‌های وزن/تعداد: رفت‌وبرگشت واقعی، رد
/// پله‌های هم‌پوشان پیش از هر نوشتن، و دست‌نخورده‌ماندن ردیف‌های بی‌شرط قدیمی.
/// </summary>
public sealed class ShippingWeightCountTierAdminTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ShippingRateCoverageDirectory Directory(out ShippingDbContext context)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-073-{Guid.NewGuid():N}")
            .Options;
        context = new ShippingDbContext(options);
        return new ShippingRateCoverageDirectory(context);
    }

    private static ShippingRatePolicyWriteRequest WeightTier(
        long min,
        long? max,
        decimal charge,
        string? optionCode = null) =>
        new(
            "tipax",
            new ShippingDestinationScopeWrite(CountryId, AreaId),
            charge,
            "IRR",
            1,
            2,
            null,
            optionCode,
            new ShippingRateConditionWrite(new ShippingNumericTierWrite(min, max)));

    private static ShippingRatePolicyWriteRequest Unconditioned(decimal charge) =>
        new(
            "tipax",
            new ShippingDestinationScopeWrite(CountryId, AreaId),
            charge,
            "IRR",
            1,
            2);

    [Fact]
    public async Task Two_adjacent_weight_tiers_roundtrip_and_reload_with_their_conditions()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var low = await directory.CreateRateAsync("tipax", WeightTier(0, 2000, 120_000m), default);
        var high = await directory.CreateRateAsync("tipax", WeightTier(2000, 5000, 260_000m), default);

        Assert.True(low.IsSuccess);
        Assert.True(high.IsSuccess);
        Assert.Equal(0, low.Value.Condition!.WeightGrams!.Min);
        Assert.Equal(2000, low.Value.Condition!.WeightGrams!.Max);
        Assert.Null(low.Value.Condition!.ItemCount);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.True(reloaded.IsSuccess);
        Assert.Equal(2, reloaded.Value.RatePolicies.Count);
        Assert.All(reloaded.Value.RatePolicies, x => Assert.NotNull(x.Condition));
        Assert.Contains(reloaded.Value.RatePolicies, x => x.ChargeAmount == 260_000m && x.Condition!.WeightGrams!.Min == 2000);
    }

    [Fact]
    public async Task An_overlapping_weight_tier_is_refused_before_any_row_is_written()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync("tipax", WeightTier(0, 3000, 120_000m), default)).IsSuccess);

        var overlap = await directory.CreateRateAsync("tipax", WeightTier(2000, 5000, 260_000m), default);
        Assert.True(overlap.IsFailure);
        Assert.Equal(ShippingErrorCodes.RatePolicyTierOverlap, overlap.FirstError.Code);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.Single(reloaded.Value.RatePolicies);
    }

    [Fact]
    public async Task An_unconditioned_base_row_coexists_with_a_tier_on_the_same_identity()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var baseRow = await directory.CreateRateAsync("tipax", Unconditioned(90_000m), default);
        var tier = await directory.CreateRateAsync("tipax", WeightTier(0, 2000, 260_000m), default);

        Assert.True(baseRow.IsSuccess);
        Assert.True(tier.IsSuccess);
        Assert.Null(baseRow.Value.Condition);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.Equal(2, reloaded.Value.RatePolicies.Count);
    }

    [Fact]
    public async Task A_reversed_tier_is_refused_with_a_stable_code_and_writes_nothing()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var reversed = await directory.CreateRateAsync("tipax", WeightTier(3000, 2000, 120_000m), default);

        Assert.True(reversed.IsFailure);
        Assert.Equal(ShippingErrorCodes.WeightTierInvalid, reversed.FirstError.Code);
        Assert.Empty((await directory.ListAsync("tipax", default)).Value.RatePolicies);
    }

    [Fact]
    public async Task Editing_an_existing_row_replaces_its_tier_and_ignores_its_own_previous_range()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateRateAsync("tipax", WeightTier(0, 2000, 120_000m), default);
        Assert.True(created.IsSuccess);

        // Widening the same row must not collide with itself; the row is excluded from its own check.
        var widened = await directory.UpdateRateAsync(
            created.Value.RatePolicyId,
            "tipax",
            WeightTier(0, 5000, 150_000m),
            default);

        Assert.True(widened.IsSuccess);
        Assert.Equal(5000, widened.Value.Condition!.WeightGrams!.Max);
        Assert.Equal(150_000m, widened.Value.ChargeAmount);
    }

    [Fact]
    public async Task Editing_a_row_into_an_overlap_with_a_sibling_is_refused_and_leaves_it_untouched()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var low = await directory.CreateRateAsync("tipax", WeightTier(0, 2000, 120_000m), default);
        Assert.True((await directory.CreateRateAsync("tipax", WeightTier(2000, 5000, 260_000m), default)).IsSuccess);

        var overlappingEdit = await directory.UpdateRateAsync(
            low.Value.RatePolicyId,
            "tipax",
            WeightTier(0, 3000, 999_000m),
            default);

        Assert.True(overlappingEdit.IsFailure);
        Assert.Equal(ShippingErrorCodes.RatePolicyTierOverlap, overlappingEdit.FirstError.Code);

        var reloaded = await directory.ListAsync("tipax", default);
        var untouched = Assert.Single(reloaded.Value.RatePolicies, x => x.RatePolicyId == low.Value.RatePolicyId);
        Assert.Equal(120_000m, untouched.ChargeAmount);
        Assert.Equal(2000, untouched.Condition!.WeightGrams!.Max);
    }

    [Fact]
    public async Task A_tier_is_scoped_to_its_own_option_and_never_blocks_another_option()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync("tipax", WeightTier(0, 2000, 120_000m, "standard"), default)).IsSuccess);

        // The same range on a different option is a different identity, so it is not an overlap.
        var express = await directory.CreateRateAsync("tipax", WeightTier(0, 2000, 260_000m, "express"), default);

        Assert.True(express.IsSuccess);
    }

    [Fact]
    public void Transport_shape_invalid_tiers_are_denied_with_stable_codes()
    {
        var validator = new ShippingRatePolicyWriteRequestValidator();

        Assert.Contains(
            validator.Validate(WeightTier(-1, 2000, 1m)).Errors,
            e => e.ErrorCode == ShippingValidationCodes.TierMinNegative);

        Assert.Contains(
            validator.Validate(WeightTier(2000, 2000, 1m)).Errors,
            e => e.ErrorCode == ShippingValidationCodes.TierRangeInvalid);

        Assert.Contains(
            validator.Validate(WeightTier(0, ShippingRatePolicy.WeightGramsMaxValue + 1, 1m)).Errors,
            e => e.ErrorCode == ShippingValidationCodes.TierOutOfRange);

        Assert.Contains(
            validator.Validate(new ShippingRatePolicyWriteRequest(
                "tipax",
                new ShippingDestinationScopeWrite(CountryId, AreaId),
                1m,
                "IRR",
                1,
                2,
                null,
                null,
                new ShippingRateConditionWrite(null, new ShippingNumericTierWrite(6, 6)))).Errors,
            e => e.ErrorCode == ShippingValidationCodes.TierRangeInvalid);

        // An unconditioned body stays valid, so every pre-073 client keeps working.
        Assert.True(validator.Validate(Unconditioned(120_000m)).IsValid);
    }
}
