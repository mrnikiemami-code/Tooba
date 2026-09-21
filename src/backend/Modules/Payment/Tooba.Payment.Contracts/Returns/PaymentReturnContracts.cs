namespace Tooba.Payment.Contracts.Returns;

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
/// snapshot پرداخت برای مسیر مرجوعی بدون وابستگی به Payment.Domain.
/// Status مقادیر نام وضعیت دامنه را حمل می‌کند (مثلاً Succeeded).
/// </summary>
public sealed record PaymentReturnSnapshot(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    string Status);

/// <summary>
/// خواندن پرداخت برای Returns بدون Directory کامل Payment.
/// </summary>
public interface IPaymentReturnReader
{
    /// <summary>
    /// پرداخت را پس از احراز هویت می‌خواند.
    /// </summary>
    Task<PaymentReturnSnapshot?> GetAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken);

    /// <summary>
    /// آخرین پرداخت checkout را برای مسیر مرجوعی برمی‌گرداند.
    /// </summary>
    Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken);
}
