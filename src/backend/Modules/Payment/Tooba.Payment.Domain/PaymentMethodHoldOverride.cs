namespace Tooba.Payment.Domain;

/// <summary>override مهلت برای یک روش پرداخت پایدار (کد درگاه).</summary>
public sealed class PaymentMethodHoldOverride
{
    /// <summary>کد پایدار درگاه.</summary>
    public string ProviderCode { get; init; } = string.Empty;

    /// <summary>مهلت آنلاین این روش.</summary>
    public int? OnlinePaymentHoldHours { get; private set; }

    /// <summary>مهلت ثبت مدرک دستی این روش.</summary>
    public int? ManualPaymentInitialHoldHours { get; private set; }

    /// <summary>مهلت بررسی دستی این روش.</summary>
    public int? ManualPaymentReviewHoldHours { get; private set; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف روش پرداخت را می‌سازد.</summary>
    public static PaymentMethodHoldOverride Create(string providerCode, DateTimeOffset now) => new()
    {
        ProviderCode = providerCode.Trim().ToLowerInvariant(),
        UpdatedAt = now,
    };

    /// <summary>مقادیر اختیاری روش را با بازهٔ معتبر می‌نویسد.</summary>
    public void Replace(
        int? onlinePaymentHoldHours,
        int? manualPaymentInitialHoldHours,
        int? manualPaymentReviewHoldHours,
        DateTimeOffset now)
    {
        OnlinePaymentHoldHours = ClampOptional(onlinePaymentHoldHours);
        ManualPaymentInitialHoldHours = ClampOptional(manualPaymentInitialHoldHours);
        ManualPaymentReviewHoldHours = ClampOptional(manualPaymentReviewHoldHours);
        UpdatedAt = now;
    }

    private static int? ClampOptional(int? value)
    {
        if (value is null)
        {
            return null;
        }

        return Math.Clamp(value.Value, 1, 24 * 30);
    }
}
