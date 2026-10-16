namespace Tooba.Settlement.Domain.ValueObjects;

/// <summary>
/// وضعیت تخصیص سطر هزینهٔ ارسال per-group. امروز فقط <see cref="PendingAllocation"/> مجاز است: هیچ
/// سیاست تجاری برای سهم carrier/فروشنده/فروشگاه/مالیات مجاز نشده است.
/// </summary>
public enum ShippingGroupFeeAllocationState
{
    /// <summary>charge مشتری ثبت شده اما تخصیص تجاری آن هنوز غیرمشخص است و settlement/payout آن مسدود است.</summary>
    PendingAllocation = 0,
}

/// <summary>
/// نوع سطر: ثبت charge پذیرفته‌شده یا reversal آن. تاریخ بازنویسی نمی‌شود؛ اصلاح فقط با سطر reversal.
/// </summary>
public enum ShippingGroupFeeRecordKind
{
    /// <summary>ثبت charge پذیرفته‌شدهٔ مشتری برای یک گروه پایدار.</summary>
    Charge = 0,

    /// <summary>reversal یک سطر charge؛ مبلغ اصلی دست‌نخورده می‌ماند.</summary>
    Reversal = 1,
}
