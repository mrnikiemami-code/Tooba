using System.Collections.Concurrent;
using Tooba.Payment.Application;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// درگاه آزمایشی قطعی. موفقیت را از متن callback نمی‌سازد؛ فقط وقتی Verify صریح باشد Succeeded می‌دهد.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    /// <summary>
    /// اگر مرجع درخواست این پسوند را داشته باشد، Verify شکست می‌خورد حتی اگر callback success باشد.
    /// </summary>
    public const string FailOnVerifySuffix = "-FAIL-VERIFY";

    /// <summary>
    /// مراجع sandbox که Host برای شکست کنترل‌شده علامت زده است. PSP واقعی نیست.
    /// </summary>
    public static ConcurrentDictionary<string, byte> SandboxDeclinedReferences { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string ProviderCode => "fake";

    /// <inheritdoc />
    public Task<GatewayInitiation> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        _ = amount;
        _ = currency;
        var reference = $"fake-{paymentId:N}";
        return Task.FromResult(new GatewayInitiation(reference, $"/payment/sandbox?ref={Uri.EscapeDataString(reference)}", DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    /// <inheritdoc />
    public Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = callbackClaimsSuccess;
        if (providerRequestReference.Contains(FailOnVerifySuffix, StringComparison.Ordinal)
            || SandboxDeclinedReferences.ContainsKey(providerRequestReference))
        {
            return Task.FromResult(new GatewayVerification(false, null, "GATEWAY_REJECTED"));
        }

        return Task.FromResult(new GatewayVerification(true, $"txn-{providerRequestReference}", null));
    }
}

/// <summary>
/// درگاه آزمایشی که Verify را رد می‌کند حتی اگر callback success باشد. PSP واقعی نیست.
/// </summary>
public sealed class FakeFailingPaymentGateway : IPaymentGateway
{
    /// <inheritdoc />
    public string ProviderCode => "fake-fail";

    /// <inheritdoc />
    public Task<GatewayInitiation> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        _ = amount;
        _ = currency;
        var reference = $"fake-{paymentId:N}{FakePaymentGateway.FailOnVerifySuffix}";
        return Task.FromResult(new GatewayInitiation(reference, $"https://payments.test/tooba/{reference}", DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    /// <inheritdoc />
    public Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        return new FakePaymentGateway().VerifyAsync(providerRequestReference, callbackClaimsSuccess, cancellationToken);
    }
}

/// <summary>
/// فهرست درگاه. SDK ارائه‌دهنده واقعی اینجا نیست.
/// </summary>
public sealed class PaymentGatewayRegistry : IPaymentGatewayRegistry
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _gateways;

    /// <summary>
    /// رجیستری را از درگاه‌های ثبت‌شده می‌سازد.
    /// </summary>
    public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(x => x.ProviderCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IPaymentGateway Resolve(string providerCode)
    {
        if (!_gateways.TryGetValue(providerCode, out var gateway))
        {
            throw new InvalidOperationException("درگاه پرداخت پیکربندی‌شده پیدا نشد.");
        }

        return gateway;
    }
}
