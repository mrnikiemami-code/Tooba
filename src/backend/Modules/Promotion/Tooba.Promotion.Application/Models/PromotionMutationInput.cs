using Tooba.BuildingBlocks;
using Tooba.Promotion.Application.Errors;
using Tooba.Promotion.Domain.ValueObjects;
namespace Tooba.Promotion.Application.Models;
public sealed record PromotionMutationInput(string Name,string CouponCode,string DiscountKind,decimal DiscountValue,DateTimeOffset? EffectiveFrom,DateTimeOffset? EffectiveTo,string? Currency=null,decimal? MinimumSubtotal=null);
public sealed record NormalizedPromotionMutation(string Name,DateTimeOffset EffectiveFrom,DateTimeOffset? EffectiveTo,PromotionDiscountKind DiscountKind,decimal PercentageRate,decimal FixedAmount,string? FixedAmountCurrency,string CouponCode,decimal? MinimumSubtotal);
public static class PromotionMutationNormalizer
{
 public static NormalizedPromotionMutation Normalize(PromotionMutationInput input,IClock clock){ArgumentNullException.ThrowIfNull(input);ArgumentNullException.ThrowIfNull(clock);if(string.IsNullOrWhiteSpace(input.Name))throw new InvalidOperationException(PromotionErrorCodes.NameRequired);if(string.IsNullOrWhiteSpace(input.CouponCode))throw new InvalidOperationException(PromotionErrorCodes.CouponRequired);var kind=string.Equals(input.DiscountKind,"FixedAmountOff",StringComparison.OrdinalIgnoreCase)||string.Equals(input.DiscountKind,"fixed",StringComparison.OrdinalIgnoreCase)||string.Equals(input.DiscountKind,"تومان",StringComparison.Ordinal)?PromotionDiscountKind.FixedAmountOff:PromotionDiscountKind.PercentageOff;var percentage=kind==PromotionDiscountKind.PercentageOff?(input.DiscountValue>1m?input.DiscountValue/100m:input.DiscountValue):0m;var fixedAmount=kind==PromotionDiscountKind.FixedAmountOff?input.DiscountValue:0m;var currency=kind==PromotionDiscountKind.FixedAmountOff?(string.IsNullOrWhiteSpace(input.Currency)?"IRR":input.Currency.Trim().ToUpperInvariant()):null;return new(input.Name.Trim(),input.EffectiveFrom??clock.UtcNow,input.EffectiveTo,kind,percentage,fixedAmount,currency,input.CouponCode.Trim(),input.MinimumSubtotal);}
}
