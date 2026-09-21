using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// کدهای پایدار reason برای eligibility مرجوعی.
/// </summary>
public static class ReturnEligibilityReasonCodes
{
    /// <summary>سفارش Paid نیست.</summary>
    public const string NotPaid = "not_paid";

    /// <summary>تحویلی ثبت نشده.</summary>
    public const string NotDelivered = "not_delivered";

    /// <summary>مهلت مرجوعی گذشته.</summary>
    public const string WindowExpired = "window_expired";

    /// <summary>تعداد قابل مرجوعی نمانده.</summary>
    public const string NothingReturnable = "nothing_returnable";

    /// <summary>سیاست snapshot خط غیرقابل مرجوعی است.</summary>
    public const string NonReturnable = "non_returnable";

    /// <summary>واجد شرایط.</summary>
    public const string Eligible = "eligible";

    /// <summary>سفارش پیدا نشد.</summary>
    public const string OrderMissing = "order_missing";

    /// <summary>fulfillment پیدا نشد.</summary>
    public const string FulfillmentMissing = "fulfillment_missing";

    /// <summary>پیام فارسی قابل‌نمایش برای UI/API از ReasonCode.</summary>
    public static string ToFaMessage(string reasonCode) => reasonCode switch
    {
        NotPaid => "مرجوعی فقط برای سفارش Paid مجاز است.",
        NotDelivered => "هنوز تحویلی ثبت نشده است.",
        WindowExpired => "مهلت مرجوعی تمام شده است.",
        NothingReturnable => "تعداد قابل مرجوعی باقی نمانده است.",
        NonReturnable => "این کالا طبق سیاست سفارش قابل مرجوعی نیست.",
        OrderMissing => "سفارش برای مرجوعی پیدا نشد.",
        FulfillmentMissing => "اطلاعات fulfillment برای مرجوعی پیدا نشد.",
        Eligible => "سفارش واجد شرایط مرجوعی است.",
        _ => "مرجوعی برای این سفارش مجاز نیست.",
    };

    /// <summary>کد پایدار HTTP برای ReasonCode دامنه.</summary>
    public static string ToErrorCode(string reasonCode) => reasonCode switch
    {
        WindowExpired => "return.expired",
        NonReturnable => "return.non_returnable",
        NothingReturnable => "return.quantity_exceeded",
        NotDelivered => "return.not_delivered",
        NotPaid => "return.not_paid",
        OrderMissing => "return.missing",
        FulfillmentMissing => "return.fulfillment_missing",
        _ => "return.rejected",
    };
}
