namespace Tooba.Payment.Application;

/// <summary>
/// حل مهلت سبد/پرداخت: روش پرداخت &gt; فروشگاه &gt; platform. مقدار جادویی در caller نیست.
/// </summary>
public interface ICommerceHoldPolicy
{
    /// <summary>ساعت ماندگاری سبد؛ رزرو موجودی نیست.</summary>
    int ResolveCartPersistenceHours();

    /// <summary>مهلت پرداخت آنلاین به ساعت.</summary>
    int ResolveOnlineHoldHours(string? providerCode);

    /// <summary>مهلت انتظار مدرک کارت‌به‌کارت به ساعت.</summary>
    int ResolveManualInitialHoldHours(string? providerCode);

    /// <summary>مهلت بررسی مدرک کارت‌به‌کارت به ساعت.</summary>
    int ResolveManualReviewHoldHours(string? providerCode);

    /// <summary>ExpiresAt رزرو اولیهٔ سفارش پس از commit.</summary>
    DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow);

    /// <summary>مهلت unpaid برای یک پرداخت با کد درگاه.</summary>
    DateTimeOffset ResolveUnpaidTimeoutAt(string providerCode, DateTimeOffset utcNow);

    /// <summary>مهلت بازبینی دستی.</summary>
    DateTimeOffset ResolveManualReviewExpiresAt(DateTimeOffset utcNow);
}

/// <summary>انقضای پرداخت‌نشده و بازگشایی تلاش پس از EnsureOrderSupply.</summary>
public interface IPaymentExpiryDirectory
{
    /// <summary>
    /// پرداخت‌های سررسید unpaid را با قفل دسته‌ای منقضی می‌کند و CheckoutIdها را برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyList<Guid>> ExpireDueUnpaidAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>پرداخت Expired را با تلاش جدید و مهلت تازه باز می‌کند؛ رزرو نمی‌سازد.</summary>
    Task ReopenExpiredForRetryAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken);
}
