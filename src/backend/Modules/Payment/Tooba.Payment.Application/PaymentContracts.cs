using Tooba.BuildingBlocks;
using Tooba.Payment.Domain;

namespace Tooba.Payment.Application;

/// <summary>
/// تصویر قابل‌پرداخت سفارش. مبلغ را مشتری نمی‌فرستد؛ Payment از این تصویر می‌خواند.
/// </summary>
public sealed record PayableCheckoutSnapshot(
    Guid CheckoutId,
    OrderPaymentMode Mode,
    string Currency,
    IReadOnlyList<PayableSellerOrderSnapshot> SellerOrders);

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
}

/// <summary>
/// نتیجهٔ شروع درگاه. Redirect راز داخلی نیست.
/// </summary>
public sealed record PaymentInitiationResult(
    Guid PaymentId,
    Guid AttemptId,
    PaymentStatus Status,
    string ProviderCode,
    string ProviderRequestReference,
    string? RedirectUrl,
    decimal Amount,
    string Currency);

/// <summary>
/// نتیجهٔ تأیید. متن callback جایگزین این نیست.
/// </summary>
public sealed record PaymentVerificationResult(
    Guid PaymentId,
    PaymentStatus Status,
    bool NewlySucceeded);

/// <summary>
/// فرمان شروع. Amount ندارد چون مشتری مبلغ را انتخاب نمی‌کند.
/// </summary>
public sealed record InitiatePaymentCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    Guid? BuyerPartyId,
    string IdempotencyKey,
    string ProviderCode);

/// <summary>
/// فرمان تأیید. Claim موفقیت در بدنه به‌تنهایی پذیرفته نمی‌شود.
/// </summary>
public sealed record VerifyPaymentCommand(
    Guid PaymentId,
    Guid AttemptId,
    string ProviderRequestReference,
    bool CallbackClaimsSuccess);

/// <summary>
/// قرارداد درگاه خنثی نسبت به PSP واقعی.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// کد پایدار درگاه.
    /// </summary>
    string ProviderCode { get; }

    /// <summary>
    /// شروع پرداخت نزد درگاه. این متد وضعیت Succeeded نمی‌سازد.
    /// </summary>
    Task<GatewayInitiation> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken);

    /// <summary>
    /// حقیقت موفقیت را از درگاه می‌پرسد. مقدار callback را راست نمی‌گیرد.
    /// </summary>
    Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken);
}

/// <summary>
/// خروجی شروع درگاه.
/// </summary>
public sealed record GatewayInitiation(string ProviderRequestReference, string? RedirectUrl, DateTimeOffset? ExpiresAt);

/// <summary>
/// خروجی Verify. فقط وقتی Succeeded است که درگاه واقعاً تأیید کند.
/// </summary>
public sealed record GatewayVerification(bool VerifiedSuccess, string? ProviderTransactionReference, string? FailureCode);

/// <summary>
/// فهرست درگاه‌های ثبت‌شده.
/// </summary>
public interface IPaymentGatewayRegistry
{
    /// <summary>
    /// درگاه را با کد پایدار برمی‌گرداند.
    /// </summary>
    IPaymentGateway Resolve(string providerCode);
}

/// <summary>
/// نتیجهٔ refund از درگاه.
/// </summary>
public sealed record GatewayRefundResult(
    bool Succeeded,
    string? ProviderReference,
    string? FailureCode);

/// <summary>
/// قرارداد refund نزد درگاه. PSP واقعی اینجا نیست.
/// </summary>
public interface IPaymentRefundGateway
{
    /// <summary>
    /// refund را با idempotency نزد درگاه اجرا می‌کند.
    /// </summary>
    Task<GatewayRefundResult> RefundAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken);
}

/// <summary>
/// نگهبان موردکاربرد پرداخت.
/// </summary>
public interface IPaymentUseCaseGuard
{
    /// <summary>
    /// اجازهٔ شروع/تأیید را بررسی می‌کند. شمارهٔ سفارش به‌تنهایی کافی نیست.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// تصویر خواندنی پرداخت.
/// </summary>
public sealed record PaymentSnapshot(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string ProviderCode,
    IReadOnlyList<PaymentAllocationSnapshot> Allocations);

/// <summary>
/// تخصیص خواندنی.
/// </summary>
public sealed record PaymentAllocationSnapshot(Guid SellerOrderId, decimal AllocatedAmount, string Currency);

/// <summary>
/// ارکستراسیون پرداخت. مبلغ را از سفارش می‌خواند نه از کلاینت.
/// </summary>
public interface IPaymentDirectory
{
    /// <summary>
    /// پرداخت را از تصویر سفارش شروع می‌کند. RequestToReserve را الزام به پرداخت نمی‌کند.
    /// </summary>
    Task<PaymentInitiationResult> InitiateAsync(InitiatePaymentCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// callback را Verify می‌کند. متن success به‌تنهایی کافی نیست.
    /// </summary>
    Task<PaymentVerificationResult> VerifyAsync(VerifyPaymentCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// پرداخت را پس از احراز هویت می‌خواند.
    /// </summary>
    Task<PaymentSnapshot?> GetAsync(Guid paymentId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken);

    /// <summary>
    /// آخرین پرداخت checkout را پس از احراز مالکیت سفارش برمی‌گرداند تا مصرف‌کننده وضعیت واقعی
    /// `PendingPayment`، `Paid` یا `Failed` را از ماژول Payment بخواند و از وضعیت Order حدس نزند.
    /// </summary>
    Task<PaymentSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken);
}

/// <summary>
/// reconciliation پرداخت‌های Pending برای callbackهای گم‌شده/دیررسیده.
/// </summary>
public interface IPaymentReconciliationDirectory
{
    /// <summary>
    /// پرداخت‌های Pending قدیمی‌تر از minAge را Verify می‌کند.
    /// </summary>
    Task<int> ReconcileStalePendingAsync(
        DateTimeOffset asOf,
        TimeSpan minAge,
        int batchSize,
        CancellationToken cancellationToken);
}

/// <summary>
/// رویداد پایدار موفقیت Verify. تصویر Paid سفارش فقط از مصرف این قرارداد ساخته می‌شود نه از تراکنش همزمان Payment.
/// </summary>
public sealed class PaymentSucceededIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار قرارداد Outbox.
    /// </summary>
    public const string EventTypeName = "payment.succeeded.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پرداخت تأییدشده.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// checkout مرجع بدون FK.
    /// </summary>
    public Guid CheckoutId { get; set; }

    /// <summary>
    /// مبلغ تصویر سفارش.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ارز تصویر سفارش.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// مرجع تراکنش تأییدشدهٔ درگاه.
    /// </summary>
    public string ProviderTransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// سفارش‌های فروشندهٔ هدف تخصیص.
    /// </summary>
    public Guid[] SellerOrderIds { get; set; } = [];
}
