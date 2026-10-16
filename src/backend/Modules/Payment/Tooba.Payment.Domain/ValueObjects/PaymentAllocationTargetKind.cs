namespace Tooba.Payment.Domain.ValueObjects;

/// <summary>
/// هدف مالی تخصیص پرداخت. فروشنده و هزینهٔ ارسال فروشگاه را قاطی نمی‌کند.
/// </summary>
public enum PaymentAllocationTargetKind
{
    /// <summary>سهم سفارش فروشنده.</summary>
    SellerOrder = 0,

    /// <summary>
    /// هزینهٔ ارسال متعلق به Store/platform؛ به SellerOrder نسبت داده نمی‌شود.
    /// </summary>
    StoreShipping = 1,

    /// <summary>
    /// هزینهٔ ارسال یک گروه ارسال پایدار سفارش (زمانی که سفارش چند گروه دارد). هدف همان
    /// <c>ShippingGroupId</c> است، نه حساب فروشنده و نه sentinel قدیمی.
    /// </summary>
    StoreShippingGroup = 2,
}
