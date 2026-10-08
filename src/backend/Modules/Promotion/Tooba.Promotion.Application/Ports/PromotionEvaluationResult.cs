using Tooba.Promotion.Domain.ValueObjects;

namespace Tooba.Promotion.Application.Ports;

/// <summary>
/// یک پروموشن اعمال‌شده در نتیجهٔ ارزیابی.
/// </summary>
public sealed record AppliedPromotion(
    Guid PromotionId,
    string Name,
    string? CouponCode,
    PromotionDiscountKind DiscountKind,
    decimal DiscountAmount);

/// <summary>
/// خروجی ارزیابی. Pricing را mutate نمی‌کند و مالیات حساب نمی‌کند.
/// </summary>
public sealed record PromotionEvaluationResult(
    decimal DiscountAmount,
    decimal PostDiscountTaxExclusiveAmount,
    IReadOnlyList<AppliedPromotion> Applied,
    IReadOnlyList<string> RejectionReasons);
