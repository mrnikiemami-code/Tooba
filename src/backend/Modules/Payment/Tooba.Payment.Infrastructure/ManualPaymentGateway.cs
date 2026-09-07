using System.Collections.Concurrent;
using Tooba.Payment.Application;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// درگاه کارت‌به‌کارت/دستی LOCAL. Verify فقط پس از تأیید اپراتور (ConfirmDeposit) موفق می‌شود.
/// </summary>
public sealed class ManualPaymentGateway : IPaymentGateway
{
    /// <summary>کد پایدار درگاه دستی.</summary>
    public const string ProviderCodeValue = "manual";

    /// <summary>مراجع تأییدشده توسط اپراتور تا Verify موفق شود.</summary>
    public static ConcurrentDictionary<string, byte> ConfirmedReferences { get; } = new(StringComparer.Ordinal);

    /// <summary>مراجع ردشده توسط اپراتور.</summary>
    public static ConcurrentDictionary<string, byte> RejectedReferences { get; } = new(StringComparer.Ordinal);

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
            RedirectUrl: $"/payment/manual?ref={Uri.EscapeDataString(reference)}",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24)));
    }

    /// <inheritdoc />
    public Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = callbackClaimsSuccess;
        if (RejectedReferences.ContainsKey(providerRequestReference))
        {
            return Task.FromResult(new GatewayVerification(false, null, "MANUAL_DEPOSIT_REJECTED"));
        }

        if (!ConfirmedReferences.ContainsKey(providerRequestReference))
        {
            return Task.FromResult(new GatewayVerification(false, null, "MANUAL_DEPOSIT_PENDING"));
        }

        return Task.FromResult(new GatewayVerification(true, $"manual-txn-{providerRequestReference}", null));
    }

    /// <summary>تأیید واریز برای مرجع درگاه.</summary>
    public static void Confirm(string providerRequestReference)
    {
        RejectedReferences.TryRemove(providerRequestReference, out _);
        ConfirmedReferences[providerRequestReference] = 1;
    }

    /// <summary>رد واریز برای مرجع درگاه.</summary>
    public static void Reject(string providerRequestReference)
    {
        ConfirmedReferences.TryRemove(providerRequestReference, out _);
        RejectedReferences[providerRequestReference] = 1;
    }

    /// <summary>آیا کد درگاه دستی است.</summary>
    public static bool IsManual(string? providerCode) =>
        string.Equals(providerCode, ProviderCodeValue, StringComparison.OrdinalIgnoreCase);
}
