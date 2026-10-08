using Tooba.BuildingBlocks;
using Tooba.Promotion.Application.Composition;
using Tooba.Promotion.Application.Models;
using Tooba.Promotion.Contracts.Errors;
using Tooba.Promotion.Domain.ValueObjects;
using Xunit;

namespace Tooba.Promotion.Tests.Behavior;

public sealed class PromotionCqrsNormalizationTests
{
    [Fact]
    public void Normalizer_preserves_wire_rules_and_uses_clock()
    {
        var now = new DateTimeOffset(2026, 9, 22, 7, 0, 0, TimeSpan.Zero);
        var result = PromotionMutationNormalizer.Normalize(
            new("  Sale  ", "  SAVE20  ", "PercentageOff", 20m, null, null),
            new FixedClock(now));

        Assert.Equal("Sale", result.Name);
        Assert.Equal("SAVE20", result.CouponCode);
        Assert.Equal(.2m, result.PercentageRate);
        Assert.Equal(now, result.EffectiveFrom);
        Assert.Equal(PromotionDiscountKind.PercentageOff, result.DiscountKind);
    }

    [Theory]
    [InlineData("FixedAmountOff")]
    [InlineData("fixed")]
    [InlineData("تومان")]
    public void Fixed_amount_aliases_default_currency(string alias)
    {
        var result = PromotionMutationNormalizer.Normalize(
            new("Sale", "SAVE", alias, 100m, DateTimeOffset.UnixEpoch, null),
            new FixedClock(DateTimeOffset.MaxValue));

        Assert.Equal(PromotionDiscountKind.FixedAmountOff, result.DiscountKind);
        Assert.Equal("IRR", result.FixedAmountCurrency);
        Assert.Equal(100m, result.FixedAmount);
    }

    [Fact]
    public void Normalizer_raises_declared_typed_codes_only()
    {
        var ex = Assert.Throws<ContractOperationException>(() =>
            PromotionMutationNormalizer.Normalize(
                new("", "SAVE", "fixed", 1m, null, null),
                new FixedClock(DateTimeOffset.UnixEpoch)));
        Assert.Equal(PromotionErrorCodes.NameRequired, ex.Code);
        Assert.True(PromotionErrorCodes.IsKnown(ex.Code));
    }

    [Fact]
    public async Task Typed_fault_seam_maps_declared_codes_and_lets_foreign_codes_escape()
    {
        var mapped = await PromotionOperation.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
            throw new ContractOperationException(PromotionErrorCodes.MutationRejected);
        });
        Assert.True(mapped.IsFailure);
        Assert.Equal(PromotionErrorCodes.MutationRejected, mapped.FirstError.Code);

        var foreign = await Assert.ThrowsAsync<ContractOperationException>(() =>
            PromotionOperation.ExecuteAsync(async () =>
            {
                await Task.CompletedTask;
                throw new ContractOperationException("offer.not_found");
            }));
        Assert.Equal("offer.not_found", foreign.Code);
    }
}
