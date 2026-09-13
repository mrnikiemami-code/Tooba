namespace Tooba.Order.Application;

/// <summary>تصویر تنظیمات سوءاستفاده از رزرو که قبل از تراکنش اتمی خوانده می‌شود.</summary>
public sealed record CheckoutAbuseSettingsSnapshot(
    Guid StoreId,
    int MaxOpenUnpaidOrdersPerCustomer,
    int ReservationCommitWindowMinutes,
    int MaxCheckoutCommitsPerCustomerInWindow);

/// <summary>مسدود شدن کانونی سقف سفارش باز یا سهمیه شروع رزرو.</summary>
public sealed class CheckoutAbuseLimitException : InvalidOperationException
{
    /// <summary>کد پایدار.</summary>
    public string ErrorCode { get; }

    /// <summary>open_unpaid یا reservation_commit.</summary>
    public string Kind { get; }

    /// <summary>مشتری.</summary>
    public Guid CustomerId { get; }

    /// <summary>فروشگاه.</summary>
    public Guid StoreId { get; }

    /// <summary>شمارش جاری.</summary>
    public int CurrentCount { get; }

    /// <summary>سقف.</summary>
    public int MaxCount { get; }

    /// <summary>اولین زمان مجاز بعدی اگر از رویداد کهنهٔ بازه قابل استخراج باشد.</summary>
    public DateTimeOffset? NextAvailableAt { get; }

    /// <summary>استثنای مسدود با فرادادهٔ مشتری‌ایمن.</summary>
    public CheckoutAbuseLimitException(
        string errorCode,
        string kind,
        Guid storeId,
        Guid customerId,
        int currentCount,
        int maxCount,
        DateTimeOffset? nextAvailableAt)
        : base(errorCode)
    {
        ErrorCode = errorCode;
        Kind = kind;
        StoreId = storeId;
        CustomerId = customerId;
        CurrentCount = currentCount;
        MaxCount = maxCount;
        NextAvailableAt = nextAvailableAt;
    }
}

/// <summary>درگاه اعمال سقف سفارش باز و سهمیه Cycle #1 قبل از رزرو.</summary>
public interface ICheckoutAbuseGate
{
    /// <summary>تنظیم فروشگاه را بدون ورود به تراکنش سفارش می‌خواند.</summary>
    Task<CheckoutAbuseSettingsSnapshot> LoadSettingsAsync(CancellationToken cancellationToken);

    /// <summary>قفل مشتری و شمارش را قبل از رزرو موجودی اعمال می‌کند.</summary>
    Task EnsureCanStartInitialReservationAsync(
        Guid placedByUserId,
        CheckoutAbuseSettingsSnapshot settings,
        CancellationToken cancellationToken);

    /// <summary>رویداد تغییرناپذیر Cycle #1 را به همان DbContext سفارش اضافه می‌کند.</summary>
    void PrepareInitialCommit(
        Guid placedByUserId,
        Guid checkoutId,
        Guid orderId,
        CheckoutAbuseSettingsSnapshot settings,
        DateTimeOffset now);

    /// <summary>مسدود شدن را پس از rollback تراکنش سفارش ثبت می‌کند.</summary>
    Task RecordBlockAsync(CheckoutAbuseLimitException exception, CancellationToken cancellationToken);
}

/// <summary>درگاه خنثی برای آزمون‌های ماژول بدون Host.</summary>
public sealed class NullCheckoutAbuseGate : ICheckoutAbuseGate
{
    /// <inheritdoc />
    public Task<CheckoutAbuseSettingsSnapshot> LoadSettingsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new CheckoutAbuseSettingsSnapshot(Guid.Empty, 2, 30, 3));

    /// <inheritdoc />
    public Task EnsureCanStartInitialReservationAsync(
        Guid placedByUserId,
        CheckoutAbuseSettingsSnapshot settings,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public void PrepareInitialCommit(
        Guid placedByUserId,
        Guid checkoutId,
        Guid orderId,
        CheckoutAbuseSettingsSnapshot settings,
        DateTimeOffset now)
    {
    }

    /// <inheritdoc />
    public Task RecordBlockAsync(CheckoutAbuseLimitException exception, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
