namespace Tooba.Catalog.Domain;

/// <summary>یک ردیف override فروشگاه برای ماندگاری سبد و مهلت پرداخت/بازبینی.</summary>
public sealed class StoreHoldPolicySettings
{
    /// <summary>شناسه تک‌ردیفی تنظیم فروشگاه.</summary>
    public static readonly Guid SingletonId = Guid.Parse("01900000-0000-7000-8000-00000000bb01");

    /// <summary>کلید ردیف.</summary>
    public Guid SettingsId { get; init; }

    /// <summary>ساعت ماندگاری سبد؛ تهی یعنی ارث از platform.</summary>
    public int? CartPersistenceHours { get; private set; }

    /// <summary>مهلت پرداخت آنلاین فروشگاه.</summary>
    public int? OnlinePaymentHoldHours { get; private set; }

    /// <summary>مهلت ثبت مدرک کارت‌به‌کارت.</summary>
    public int? ManualPaymentInitialHoldHours { get; private set; }

    /// <summary>مهلت بررسی مدرک کارت‌به‌کارت.</summary>
    public int? ManualPaymentReviewHoldHours { get; private set; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف خالی با ارث کامل از platform.</summary>
    public static StoreHoldPolicySettings CreateDefault(DateTimeOffset now) => new()
    {
        SettingsId = SingletonId,
        UpdatedAt = now,
    };

    /// <summary>override فروشگاه را با اعتبارسنجی بازه ذخیره می‌کند.</summary>
    public void Replace(
        int? cartPersistenceHours,
        int? onlinePaymentHoldHours,
        int? manualPaymentInitialHoldHours,
        int? manualPaymentReviewHoldHours,
        DateTimeOffset now)
    {
        CartPersistenceHours = ClampOptional(cartPersistenceHours, 1, 24 * 90);
        OnlinePaymentHoldHours = ClampOptional(onlinePaymentHoldHours, 1, 24 * 30);
        ManualPaymentInitialHoldHours = ClampOptional(manualPaymentInitialHoldHours, 1, 24 * 30);
        ManualPaymentReviewHoldHours = ClampOptional(manualPaymentReviewHoldHours, 1, 24 * 30);
        UpdatedAt = now;
    }

    private static int? ClampOptional(int? value, int min, int max)
    {
        if (value is null)
        {
            return null;
        }

        return Math.Clamp(value.Value, min, max);
    }
}
