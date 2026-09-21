using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Infrastructure.Providers;

/// <summary>
/// درگاه کارت‌به‌کارت/دستی قابل پیکربندی. Verify بدون تأیید اپراتور Succeeded نمی‌سازد.
/// تأیید واقعی از مسیر PaymentDirectory.ConfirmDepositAsync (ApplyVerifiedSuccess) است.
/// </summary>
public sealed class ManualPaymentGateway : IPaymentGateway
{
    /// <summary>کد پایدار درگاه دستی.</summary>
    public const string ProviderCodeValue = "manual";

    private readonly IClock _clock;

    /// <summary>درگاه دستی با ساعت تزریقی.</summary>
    public ManualPaymentGateway(IClock clock) => _clock = clock;

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
            ExpiresAt: _clock.UtcNow.AddHours(48)));
    }

    /// <inheritdoc />
    public Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = providerRequestReference;
        _ = callbackClaimsSuccess;
        return Task.FromResult(new GatewayVerification(false, null, "MANUAL_DEPOSIT_PENDING"));
    }

    /// <summary>آیا کد درگاه دستی است.</summary>
    public static bool IsManual(string? providerCode) =>
        string.Equals(providerCode, ProviderCodeValue, StringComparison.OrdinalIgnoreCase);
}
