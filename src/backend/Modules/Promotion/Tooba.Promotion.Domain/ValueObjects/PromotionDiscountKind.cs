using Tooba.BuildingBlocks;



namespace Tooba.Promotion.Domain.ValueObjects;

/// <summary>
/// گونهٔ عمل تخفیف. تخفیف جایگزین قیمت تألیف‌شدهٔ Pricing نیست.
/// </summary>
public enum PromotionDiscountKind
{
    /// <summary>
    /// درصد از مبلغ بدون مالیات خط.
    /// </summary>
    PercentageOff = 0,

    /// <summary>
    /// مبلغ ثابت با ارز صریح.
    /// </summary>
    FixedAmountOff = 1,
}
