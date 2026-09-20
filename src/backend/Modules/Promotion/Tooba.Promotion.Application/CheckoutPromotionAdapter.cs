using Tooba.Promotion.Contracts;
using Tooba.Promotion.Domain;

namespace Tooba.Promotion.Application;

/// <summary>آداپتر در-فرآیند ارزیابی پروموشن برای checkout.</summary>
public sealed class CheckoutPromotionAdapter : ICheckoutPromotionPort
{
    private readonly IPromotionEvaluator _evaluator;

    /// <summary>آداپتر را به ارزیاب موجود وصل می‌کند.</summary>
    public CheckoutPromotionAdapter(IPromotionEvaluator evaluator) => _evaluator = evaluator;

    /// <inheritdoc />
    public async Task<CheckoutPromotionEvaluationResult> EvaluateForCheckoutAsync(
        CheckoutPromotionEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _evaluator.EvaluateAsync(
            new PromotionEvaluationRequest(
                request.OfferId,
                request.CatalogVariantId,
                request.CategoryId,
                request.SellerPartyId,
                request.Market,
                request.SalesChannel,
                request.Currency,
                request.Quantity,
                request.BaseTaxExclusiveAmount,
                request.CustomerPartyId,
                request.OrganizationPartyId,
                request.CouponCode,
                request.At,
                request.RoundingMode),
            cancellationToken);

        return new CheckoutPromotionEvaluationResult(
            result.DiscountAmount,
            result.PostDiscountTaxExclusiveAmount,
            result.Applied.Select(MapApplied).ToArray(),
            result.RejectionReasons);
    }

    private static CheckoutAppliedPromotion MapApplied(AppliedPromotion applied) =>
        new(
            applied.PromotionId,
            applied.Name,
            applied.CouponCode,
            applied.DiscountKind switch
            {
                PromotionDiscountKind.PercentageOff => CheckoutPromotionDiscountKind.PercentageOff,
                PromotionDiscountKind.FixedAmountOff => CheckoutPromotionDiscountKind.FixedAmountOff,
                _ => CheckoutPromotionDiscountKind.PercentageOff,
            });
}
