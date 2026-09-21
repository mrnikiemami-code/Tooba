

namespace Tooba.Inventory.Domain.ValueObjects;

/// <summary>
/// گونهٔ اصلاح موجودی. مقدار را مستقیماً از بیرون روی ردیف نمی‌نویسند.
/// </summary>
public enum StockAdjustmentKind
{
    /// <summary>
    /// افزایش OnHand مثل رسید.
    /// </summary>
    Increase = 0,

    /// <summary>
    /// کاهش OnHand مثل ضایعات، بدون عبور از رزرو.
    /// </summary>
    Decrease = 1,

    /// <summary>
    /// تصحیح شمارش؛ دلتا از مقدار فعلی محاسبه می‌شود.
    /// </summary>
    Set = 2,
}
