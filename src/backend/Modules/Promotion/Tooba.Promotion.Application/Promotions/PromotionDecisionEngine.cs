using Tooba.Promotion.Application.Promotions.Ports;
using Tooba.Promotion.Domain.Aggregates;
using Tooba.Promotion.Domain.ValueObjects;

namespace Tooba.Promotion.Application.Promotions;

/// <summary>
/// موتور تصمیم ارزیابی تخفیف روی مجموعهٔ کاندیدهای از پیش خوانده‌شده.
/// قاعدهٔ انتخاب (اثربخشی زمانی، صلاحیت، اولویت، Exclusive/Stackable، سقف موجودی، ارز) همان قاعدهٔ
/// قبلی است و فقط محل خواندن داده از «هر ارزیابی» به «یک‌بار برای مجموعه» منتقل شده است.
/// خروجی هیچ‌گاه Pricing را mutate نمی‌کند.
/// </summary>
public static class PromotionDecisionEngine
{
    /// <summary>
    /// تخفیف را روی مبلغ بدون مالیات یک خط حساب می‌کند. کاندیدها مالک جدول promotion هستند و mutate نمی‌شوند.
    /// </summary>
    public static async Task<PromotionEvaluationResult> EvaluateAsync(
        IReadOnlyList<PromotionDefinition> candidates,
        PromotionEvaluationRequest request,
        Func<Guid, CancellationToken, Task<bool>> canRedeem,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(canRedeem);

        var rejections = new List<string>();
        if (request.Quantity <= 0 || request.BaseTaxExclusiveAmount < 0 || string.IsNullOrWhiteSpace(request.Currency))
        {
            return new PromotionEvaluationResult(0m, request.BaseTaxExclusiveAmount, [], ["INPUT_INVALID"]);
        }

        var facts = new PromotionEligibilityFacts(
            request.OfferId,
            request.CatalogVariantId,
            request.CategoryId,
            request.SellerPartyId,
            request.Market,
            request.SalesChannel,
            request.Currency,
            request.CustomerPartyId,
            request.OrganizationPartyId,
            request.Quantity,
            request.BaseTaxExclusiveAmount,
            request.CouponCode);

        var matching = candidates
            .Where(x => x.IsEffectiveAt(request.At) && x.IsEligible(facts))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.PromotionId)
            .ToList();

        if (matching.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                rejections.Add("COUPON_NOT_APPLICABLE");
            }

            return new PromotionEvaluationResult(0m, request.BaseTaxExclusiveAmount, [], rejections);
        }

        IReadOnlyList<PromotionDefinition> selected;
        var exclusive = matching.Where(x => x.StackingPolicy == PromotionStackingPolicy.Exclusive).ToList();
        if (exclusive.Count > 0)
        {
            selected = [exclusive[0]];
        }
        else
        {
            selected = matching.Where(x => x.StackingPolicy == PromotionStackingPolicy.Stackable).ToList();
        }

        var remaining = request.BaseTaxExclusiveAmount;
        var applied = new List<AppliedPromotion>();
        foreach (var promotion in selected)
        {
            if (!await canRedeem(promotion.PromotionId, cancellationToken))
            {
                rejections.Add("REDEMPTION_BLOCKED");
                continue;
            }

            var discount = promotion.ComputeDiscount(remaining, request.Currency, request.RoundingMode);
            if (discount <= 0)
            {
                if (promotion.DiscountKind == PromotionDiscountKind.FixedAmountOff)
                {
                    rejections.Add("CURRENCY_MISMATCH");
                }

                continue;
            }

            remaining -= discount;
            applied.Add(new AppliedPromotion(
                promotion.PromotionId,
                promotion.Name,
                promotion.CouponCode,
                promotion.DiscountKind,
                discount));
        }

        var totalDiscount = request.BaseTaxExclusiveAmount - remaining;
        var post = remaining;
        return new PromotionEvaluationResult(totalDiscount, post, applied, rejections);
    }
}
