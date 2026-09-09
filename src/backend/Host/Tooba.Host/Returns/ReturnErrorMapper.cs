using Tooba.Returns.Application;

namespace Tooba.Host.Returns;

/// <summary>نگاشت خطاهای مرجوعی/بازگشت وجه به کد پایدار + پیام فارسی.</summary>
internal static class ReturnErrorMapper
{
    public static (string Code, string Fa) Map(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return ("return.rejected", "درخواست مرجوعی انجام نشد.");
        }

        var trimmed = message.Trim();
        if (trimmed.StartsWith("return.", StringComparison.Ordinal)
            || trimmed.StartsWith("refund.", StringComparison.Ordinal))
        {
            return (trimmed, ToFa(trimmed));
        }

        return trimmed switch
        {
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.WindowExpired)
                => ("return.expired", m),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NonReturnable)
                => ("return.non_returnable", m),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NothingReturnable)
                => ("return.quantity_exceeded", m),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NotDelivered)
                => ("return.not_delivered", m),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NotPaid)
                => ("return.not_paid", m),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.OrderMissing)
                => ("return.missing", m),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.FulfillmentMissing)
                => ("return.fulfillment_missing", m),
            "تعداد مرجوعی از باقیماندهٔ تحویل‌شده بیشتر است." =>
                ("return.quantity_exceeded", "تعداد مرجوعی از باقیماندهٔ تحویل‌شده بیشتر است."),
            "تعداد مرجوعی باید مثبت باشد." =>
                ("return.quantity_invalid", "تعداد مرجوعی باید مثبت باشد."),
            "انتقال وضعیت از این حالت مجاز نیست." =>
                ("return.stale", "وضعیت مرجوعی تغییر کرده است. صفحه را تازه کنید."),
            "retry فقط برای RefundFailed مجاز است." =>
                ("refund.retry.invalid_state", "تلاش مجدد بازگشت وجه فقط برای وضعیت ناموفق مجاز است."),
            "refund فقط برای پرداخت Succeeded مجاز است." =>
                ("refund.already_started", "بازگشت وجه فقط برای پرداخت موفق مجاز است."),
            "پرداخت موفق برای refund پیدا نشد." =>
                ("refund.payment_missing", "پرداخت مرجع برای بازگشت وجه پیدا نشد."),
            "پرداخت مرجع پیدا نشد." =>
                ("refund.payment_missing", "پرداخت مرجع برای بازگشت وجه پیدا نشد."),
            "درخواست مرجوعی پیدا نشد." =>
                ("return.missing", "درخواست مرجوعی پیدا نشد."),
            "خط سفارش پیدا نشد." =>
                ("return.line_missing", "قلم سفارش برای مرجوعی پیدا نشد."),
            "درخواست‌دهنده مالک سفارش نیست." =>
                ("return.not_owner", "فقط مالک سفارش می‌تواند مرجوعی ثبت کند."),
            "کلید idempotency الزامی است." =>
                ("return.idempotency_required", "کلید تکرارناپذیری الزامی است."),
            "مقصد بازگشت وجه نامعتبر است." =>
                ("refund.destination.invalid", "مقصد بازگشت وجه نامعتبر است."),
            _ => ("return.rejected", trimmed.Contains("Bad Request", StringComparison.OrdinalIgnoreCase)
                ? "درخواست مرجوعی انجام نشد."
                : trimmed),
        };
    }

    public static string ToFa(string code) => code switch
    {
        "return.expired" => ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.WindowExpired),
        "return.non_returnable" => ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NonReturnable),
        "return.quantity_exceeded" => "تعداد مرجوعی از باقیماندهٔ قابل مرجوعی بیشتر است.",
        "return.quantity_invalid" => "تعداد مرجوعی باید مثبت باشد.",
        "return.not_delivered" => ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NotDelivered),
        "return.not_paid" => ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NotPaid),
        "return.missing" => "درخواست مرجوعی پیدا نشد.",
        "return.fulfillment_missing" => ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.FulfillmentMissing),
        "return.stale" => "وضعیت مرجوعی تغییر کرده است. صفحه را تازه کنید.",
        "return.already_approved" => "این مرجوعی قبلاً تأیید شده است.",
        "return.already_rejected" => "این مرجوعی قبلاً رد شده است.",
        "refund.retry.invalid_state" => "تلاش مجدد بازگشت وجه فقط برای وضعیت ناموفق مجاز است.",
        "refund.already_started" => "بازگشت وجه قبلاً آغاز شده است.",
        "refund.already_completed" => "بازگشت وجه قبلاً تکمیل شده است.",
        "refund.payment_missing" => "پرداخت مرجع برای بازگشت وجه پیدا نشد.",
        "refund.destination.invalid" => "مقصد بازگشت وجه نامعتبر است.",
        "return.rejected" => "درخواست مرجوعی انجام نشد.",
        _ => "درخواست مرجوعی انجام نشد.",
    };
}
