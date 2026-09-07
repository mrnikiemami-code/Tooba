using Tooba.Payment.Application;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// درگاه کارت‌به‌کارت/دستی قابل پیکربندی. Verify بدون تأیید اپراتور Succeeded نمی‌سازد.
/// تأیید واقعی از مسیر PaymentDirectory.ConfirmDepositAsync (ApplyVerifiedSuccess) است.
/// </summary>
public sealed class ManualPaymentGateway : IPaymentGateway
{
    /// <summary>کد پایدار درگاه دستی.</summary>
    public const string ProviderCodeValue = "manual";

    /// <inheritdoc />
    public string ProviderCode => ProviderCodeValue;

    /// <inheritdoc />
    public Task<GatewayInitiation> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        _ = amount;
        _ = currency;
        var reference = $"manual-{paymentId:N}";
        return Task.FromResult(new GatewayInitiation(
            reference,
            RedirectUrl: $"/payment/manual?ref={Uri.EscapeDataString(reference)}&paymentId={paymentId:D}",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(48)));
    }

    /// <inheritdoc />
    public Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = providerRequestReference;
        _ = callbackClaimsSuccess;
        // حقیقت موفقیت فقط با ConfirmDeposit اپراتور (دامنه Payment) ثبت می‌شود؛
        // Reconcile/callback به تنهایی Succeeded نمی‌سازد.
        return Task.FromResult(new GatewayVerification(false, null, "MANUAL_DEPOSIT_PENDING"));
    }

    /// <summary>آیا کد درگاه دستی است.</summary>
    public static bool IsManual(string? providerCode) =>
        string.Equals(providerCode, ProviderCodeValue, StringComparison.OrdinalIgnoreCase);
}
