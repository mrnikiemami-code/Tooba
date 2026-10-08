using Tooba.BuildingBlocks;
using Tooba.Promotion.Contracts.Errors;
using Tooba.Promotion.Domain.ValueObjects;

namespace Tooba.Promotion.Application.Models;

/// <summary>ورودی خام تغییر پروموشن از مرز HTTP.</summary>
public sealed record PromotionMutationInput(
    string Name,
    string CouponCode,
    string DiscountKind,
    decimal DiscountValue,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Currency = null,
    decimal? MinimumSubtotal = null);

/// <summary>ورودی نرمال‌شده برای دایرکتوری پروموشن.</summary>
public sealed record NormalizedPromotionMutation(
    string Name,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    PromotionDiscountKind DiscountKind,
    decimal PercentageRate,
    decimal FixedAmount,
    string? FixedAmountCurrency,
    string CouponCode,
    decimal? MinimumSubtotal);

/// <summary>
/// نرمال‌سازی ورودی پروموشن با قواعد سیم ثابت. نقض قاعده با کد پایدار نوع‌دار اعلام می‌شود —
/// نه با متن و نه با heuristic روی Message.
/// </summary>
public static class PromotionMutationNormalizer
{
    /// <summary>ورودی را نرمال می‌کند و قاعدهٔ نام/کوپن را با کد پایدار اعلام می‌کند.</summary>
    public static NormalizedPromotionMutation Normalize(PromotionMutationInput input, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(clock);
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new ContractOperationException(PromotionErrorCodes.NameRequired);
        }

        if (string.IsNullOrWhiteSpace(input.CouponCode))
        {
            throw new ContractOperationException(PromotionErrorCodes.CouponRequired);
        }

        var kind =
            string.Equals(input.DiscountKind, "FixedAmountOff", StringComparison.OrdinalIgnoreCase)
            || string.Equals(input.DiscountKind, "fixed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(input.DiscountKind, "تومان", StringComparison.Ordinal)
                ? PromotionDiscountKind.FixedAmountOff
                : PromotionDiscountKind.PercentageOff;
        var percentage = kind == PromotionDiscountKind.PercentageOff
            ? (input.DiscountValue > 1m ? input.DiscountValue / 100m : input.DiscountValue)
            : 0m;
        var fixedAmount = kind == PromotionDiscountKind.FixedAmountOff ? input.DiscountValue : 0m;
        var currency = kind == PromotionDiscountKind.FixedAmountOff
            ? (string.IsNullOrWhiteSpace(input.Currency) ? "IRR" : input.Currency.Trim().ToUpperInvariant())
            : null;
        return new(
            input.Name.Trim(),
            input.EffectiveFrom ?? clock.UtcNow,
            input.EffectiveTo,
            kind,
            percentage,
            fixedAmount,
            currency,
            input.CouponCode.Trim(),
            input.MinimumSubtotal);
    }
}
