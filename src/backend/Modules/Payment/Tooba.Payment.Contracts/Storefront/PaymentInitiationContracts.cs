namespace Tooba.Payment.Contracts.Storefront;

/// <summary>
/// درخواست شروع پرداخت برای checkout‌ای که Order قبلاً commit کرده است.
/// هیچ قاعدهٔ کسب‌وکاری اینجا تکرار نمی‌شود: actor، مالکیت سبد، انتخاب درگاه، idempotency و
/// رفتار online/wallet/manual همچنان مالکیت Payment است.
/// </summary>
public sealed record StorefrontPaymentInitiationRequest(
    Guid CheckoutId,
    Guid CartId,
    string? GuestSecret,
    string IdempotencyKey,
    bool UseWallet,
    string? ProviderCode,
    Guid? AuthenticatedUserId);

/// <summary>
/// خروجی شروع پرداخت. فقط شناسه‌ها، وضعیت پایدار و اطلاعات هدایت درگاه را منتقل می‌کند.
/// </summary>
public sealed record StorefrontPaymentInitiationSnapshot(
    Guid PaymentId,
    Guid AttemptId,
    Guid CheckoutId,
    string Status,
    string ProviderCode,
    string ProviderRequestReference,
    string RedirectUrl,
    decimal Amount,
    string Currency,
    bool RequiresPspRedirect = true);

/// <summary>
/// درز حداقلی Order → Payment برای شروع یک بار پرداخت روی checkout مالکیت‌دار.
/// این درز هیچ عملیات تأیید/جبران انجام نمی‌دهد و صرفاً به مسیر شروع موجود Payment وصل می‌شود.
/// </summary>
public interface IPaymentInitiationPort
{
    /// <summary>
    /// شروع پرداخت انتخاب‌شده برای checkout مشخص. خطاهای مالکیت/صلاحیت/idempotency از سمت
    /// Payment به‌صورت <c>payment.*</c> منتشر می‌شوند و اینجا بازنویسی نمی‌شوند.
    /// </summary>
    Task<StorefrontPaymentInitiationSnapshot> InitiateForCheckoutAsync(
        StorefrontPaymentInitiationRequest request,
        CancellationToken cancellationToken);
}
