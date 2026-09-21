using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Contracts.Checkout;

/// <summary>گونهٔ تخفیف برای درز checkout (بدون نشت Domain entity).</summary>
public enum CheckoutPromotionDiscountKind
{
    /// <summary>درصد از مبلغ بدون مالیات.</summary>
    PercentageOff = 0,

    /// <summary>مبلغ ثابت.</summary>
    FixedAmountOff = 1,
}

/// <summary>ورودی ارزیابی پروموشن برای هماهنگی checkout.</summary>
public sealed record CheckoutPromotionEvaluationRequest(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid? CategoryId,
    Guid SellerPartyId,
    string Market,
    string SalesChannel,
    string Currency,
    decimal Quantity,
    decimal BaseTaxExclusiveAmount,
    Guid? CustomerPartyId,
    Guid? OrganizationPartyId,
    string? CouponCode,
    DateTimeOffset At,
    QuantityRoundingMode RoundingMode = QuantityRoundingMode.Nearest);

/// <summary>یک پروموشن اعمال‌شده در نتیجهٔ ارزیابی checkout.</summary>
public sealed record CheckoutAppliedPromotion(
    Guid PromotionId,
    string Name,
    string? CouponCode,
    CheckoutPromotionDiscountKind DiscountKind);

/// <summary>خروجی ارزیابی checkout؛ Pricing را mutate نمی‌کند.</summary>
public sealed record CheckoutPromotionEvaluationResult(
    decimal DiscountAmount,
    decimal PostDiscountTaxExclusiveAmount,
    IReadOnlyList<CheckoutAppliedPromotion> Applied,
    IReadOnlyList<string> RejectionReasons);

/// <summary>
/// درز پایدار ارزیابی پروموشن برای checkout.
/// قواعد صلاحیت/مصرف در Promotion می‌ماند؛ آمادهٔ آداپتر آیندهٔ HTTP/gRPC.
/// </summary>
public interface ICheckoutPromotionPort
{
    /// <summary>تخفیف خط checkout را روی مبلغ بدون مالیات ارزیابی می‌کند.</summary>
    Task<CheckoutPromotionEvaluationResult> EvaluateForCheckoutAsync(
        CheckoutPromotionEvaluationRequest request,
        CancellationToken cancellationToken);
}
