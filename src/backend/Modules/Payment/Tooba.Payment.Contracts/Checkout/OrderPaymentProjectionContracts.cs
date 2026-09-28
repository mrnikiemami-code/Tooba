namespace Tooba.Payment.Contracts.Checkout;

/// <summary>
/// حالت تجاری سفارش از دید پرداخت. با Status درگاه یکی نیست.
/// </summary>
public enum OrderPaymentMode
{
    /// <summary>
    /// درخواست رزرو؛ شروع پرداخت الزامی نیست.
    /// </summary>
    RequestToReserve = 0,

    /// <summary>
    /// خرید آنلاین؛ می‌تواند وارد جریان پرداخت شود.
    /// </summary>
    OnlinePurchase = 1,
}

/// <summary>
/// سهم سفارش فروشنده از مبلغ قابل پرداخت.
/// </summary>
public sealed record PayableSellerOrderSnapshot(
    Guid SellerOrderId,
    decimal PayableAmount,
    string Currency,
    bool PendingPayment = true);

/// <summary>
/// تصویر قابل‌پرداخت سفارش. مبلغ را مشتری نمی‌فرستد؛ Payment از این تصویر می‌خواند.
/// Order این تصویر را از schema سفارش می‌سازد؛ هیچ DbContext پرداخت اینجا باز نمی‌شود.
/// </summary>
public sealed record PayableCheckoutSnapshot(
    Guid CheckoutId,
    OrderPaymentMode Mode,
    string Currency,
    IReadOnlyList<PayableSellerOrderSnapshot> SellerOrders,
    decimal ShippingAmount = 0m);

/// <summary>
/// خواندن تصویر مالی سفارش بدون DbContext سفارش.
/// </summary>
public interface IPayableCheckoutReader
{
    /// <summary>
    /// تصویر قابل پرداخت را پس از احراز هویت برمی‌گرداند. مبلغ را از کلاینت قبول نمی‌کند.
    /// </summary>
    Task<PayableCheckoutSnapshot?> GetPayableAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken);
}

/// <summary>
/// اعمال موفقیت تأییدشدهٔ پرداخت روی سفارش. فقط مصرف‌کنندهٔ Outbox این درز را صدا می‌زند؛ دایرکتوری Payment پس از SaveChanges آن را صدا نمی‌زند.
/// </summary>
public interface IOrderPaymentProjection
{
    /// <summary>
    /// سفارش‌های واجد شرایط خرید آنلاین را پس از Verify به Paid می‌برد. شروع درگاه کافی نیست و این متد به‌تنهایی منبع حقیقت Verify نیست.
    /// </summary>
    Task ApplyVerifiedSuccessAsync(
        Guid checkoutId,
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Paid را پس از برگشت تأیید واریز دستی به PendingPayment برمی‌گرداند.
    /// </summary>
    Task RevertVerifiedSuccessAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// پس از ثبت موفق مدرک/پیگیری دستی، رزرو خطوط را از TTL سبد به مهلت بررسی ارتقا می‌دهد؛
    /// در صورت Released، بازگیری معتبر می‌کند (بدون زنده کردن رزرو قدیمی).
    /// </summary>
    Task PromoteReservationsForManualPaymentReviewAsync(
        Guid checkoutId,
        DateTimeOffset reviewExpiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// پس از رد واریز دستی، رزروهای Held بررسی را آزاد می‌کند (تاریخچهٔ Released حفظ می‌شود).
    /// </summary>
    Task ReleaseReservationsAfterManualRejectAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}
