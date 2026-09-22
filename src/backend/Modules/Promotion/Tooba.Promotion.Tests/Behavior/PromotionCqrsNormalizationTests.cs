using Tooba.BuildingBlocks;
using Tooba.Promotion.Application.Errors;
using Tooba.Promotion.Application.Models;
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
    public void Validation_and_exception_mapping_are_exact_codes_only()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PromotionMutationNormalizer.Normalize(
                new("", "SAVE", "fixed", 1m, null, null),
                new FixedClock(DateTimeOffset.UnixEpoch)));
        Assert.Equal(PromotionErrorCodes.NameRequired, ex.Message);
        Assert.True(PromotionExceptionMapper.TryMapExact(PromotionErrorCodes.NameRequired, out _));
        Assert.False(PromotionExceptionMapper.TryMapExact("promotion.name.required extra", out _));
        Assert.False(PromotionExceptionMapper.TryMapExact("نام لازم است", out _));
    }
}
