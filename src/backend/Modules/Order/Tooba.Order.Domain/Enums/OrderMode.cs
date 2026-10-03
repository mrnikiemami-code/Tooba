namespace Tooba.Order.Domain.Enums;

/// <summary>
/// حالت سفارش. از وضعیت پرداخت استنتاج نمی‌شود و سبد نیست.
/// </summary>
public enum OrderMode
{
    /// <summary>
    /// درخواست رزرو؛ پرداخت الزامی نیست و خرید آنلاین پرداخت‌نشده نیست.
    /// </summary>
    RequestToReserve = 0,

    /// <summary>
    /// خرید آنلاین؛ چرخهٔ پرداخت جدا است و اینجا Paid ثبت نمی‌شود.
    /// </summary>
    OnlinePurchase = 1,
}
