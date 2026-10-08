using Tooba.Promotion.Domain.ValueObjects;

namespace Tooba.Promotion.Application.Ports;

/// <summary>
/// مرجع پروموشن برای ادمین/فروشنده/آزمون. قیمت تألیف‌شده نیست.
/// </summary>
public sealed record PromotionReference(
    Guid PromotionId,
    string Name,
    PromotionStatus Status,
    int Priority,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    PromotionStackingPolicy StackingPolicy,
    PromotionDiscountKind DiscountKind,
    decimal PercentageRate,
    decimal FixedAmount,
    string? FixedAmountCurrency,
    string? CouponCode,
    Guid? SellerPartyId = null,
    decimal? MinimumSubtotal = null);
