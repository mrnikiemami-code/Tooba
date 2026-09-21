

namespace Tooba.Promotion.Domain.ValueObjects;

/// <summary>
/// سیاست ترکیب با سایر پروموشن‌های منطبق.
/// </summary>
public enum PromotionStackingPolicy
{
    /// <summary>
    /// با سایر Stackableها به ترتیب اولویت قطعی جمع می‌شود.
    /// </summary>
    Stackable = 0,

    /// <summary>
    /// با هیچ پروموشن دیگری جمع نمی‌شود؛ بین چند Exclusive برنده با اولویت سپس شناسه انتخاب می‌شود.
    /// </summary>
    Exclusive = 1,
}
