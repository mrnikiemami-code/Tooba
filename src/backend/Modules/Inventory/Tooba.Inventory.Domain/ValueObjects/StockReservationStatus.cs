

namespace Tooba.Inventory.Domain.ValueObjects;

/// <summary>
/// وضعیت رزرو موجودی. سبد خرید یا سفارش نیست.
/// </summary>
public enum StockReservationStatus
{
    /// <summary>
    /// مقدار روی موقعیت قفل شده و هنوز مصرف نشده.
    /// </summary>
    Held = 0,

    /// <summary>
    /// قفل آزاد شده و به موجودی قابل‌فروش برگشته.
    /// </summary>
    Released = 1,

    /// <summary>
    /// رزرو به خروج از OnHand تبدیل شده است.
    /// </summary>
    Consumed = 2,
}
