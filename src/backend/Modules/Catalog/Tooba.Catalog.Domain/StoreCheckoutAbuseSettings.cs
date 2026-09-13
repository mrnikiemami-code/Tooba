namespace Tooba.Catalog.Domain;

/// <summary>تنظیم فروشگاهی سقف سفارش باز و سهمیه شروع رزرو.</summary>
public sealed class StoreCheckoutAbuseSettings
{
    /// <summary>شناسه تک‌ردیفی فروشگاه.</summary>
    public static readonly Guid SingletonId = Guid.Parse("01900000-0000-7000-8000-00000000cc23");

    /// <summary>پیش‌فرض سقف سفارش باز.</summary>
    public const int DefaultMaxOpenUnpaid = 2;

    /// <summary>پیش‌فرض بازه سهمیه رزرو (دقیقه).</summary>
    public const int DefaultWindowMinutes = 30;

    /// <summary>پیش‌فرض سقف شروع رزرو در بازه.</summary>
    public const int DefaultMaxCommits = 3;

    /// <summary>حداقل مقدار صحیح پذیرفته‌شده.</summary>
    public const int MinValue = 1;

    /// <summary>سقف سفارش باز.</summary>
    public const int MaxOpenUnpaidCap = 20;

    /// <summary>سقف بازه به دقیقه (۷ روز).</summary>
    public const int MaxWindowMinutesCap = 7 * 24 * 60;

    /// <summary>سقف سهمیه شروع رزرو.</summary>
    public const int MaxCommitsCap = 30;

    /// <summary>کلید ردیف.</summary>
    public Guid SettingsId { get; init; }

    /// <summary>حداکثر سفارش‌های بازِ پرداخت‌نشده برای هر مشتری.</summary>
    public int MaxOpenUnpaidOrdersPerCustomer { get; private set; } = DefaultMaxOpenUnpaid;

    /// <summary>بازه‌ای که رویدادهای شروع رزرو در آن شمرده می‌شوند.</summary>
    public int ReservationCommitWindowMinutes { get; private set; } = DefaultWindowMinutes;

    /// <summary>سقف شروع رزرو Cycle #1 در بازه.</summary>
    public int MaxCheckoutCommitsPerCustomerInWindow { get; private set; } = DefaultMaxCommits;

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف با مقادیر پیشنهادی محصول.</summary>
    public static StoreCheckoutAbuseSettings CreateDefault(DateTimeOffset now) => new()
    {
        SettingsId = SingletonId,
        MaxOpenUnpaidOrdersPerCustomer = DefaultMaxOpenUnpaid,
        ReservationCommitWindowMinutes = DefaultWindowMinutes,
        MaxCheckoutCommitsPerCustomerInWindow = DefaultMaxCommits,
        UpdatedAt = now,
    };

    /// <summary>جایگزینی با اعتبارسنجی صریح؛ clamp خاموش ندارد.</summary>
    public void Replace(
        int maxOpenUnpaidOrdersPerCustomer,
        int reservationCommitWindowMinutes,
        int maxCheckoutCommitsPerCustomerInWindow,
        DateTimeOffset now)
    {
        MaxOpenUnpaidOrdersPerCustomer = Require(
            maxOpenUnpaidOrdersPerCustomer,
            MinValue,
            MaxOpenUnpaidCap,
            "settings.max_open_unpaid.invalid");
        ReservationCommitWindowMinutes = Require(
            reservationCommitWindowMinutes,
            MinValue,
            MaxWindowMinutesCap,
            "settings.reservation_commit_window.invalid");
        MaxCheckoutCommitsPerCustomerInWindow = Require(
            maxCheckoutCommitsPerCustomerInWindow,
            MinValue,
            MaxCommitsCap,
            "settings.max_checkout_commits.invalid");
        UpdatedAt = now;
    }

    private static int Require(int value, int min, int max, string errorCode)
    {
        if (value < min || value > max)
        {
            throw new InvalidOperationException(errorCode);
        }

        return value;
    }
}
