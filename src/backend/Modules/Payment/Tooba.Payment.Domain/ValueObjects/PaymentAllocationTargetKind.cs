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
}
