using System.Collections.Concurrent;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Infrastructure.Providers;

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

    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>درگاه جعلی با ساعت/شناسه تزریقی.</summary>
    public FakePaymentGateway(IClock clock, IIdGenerator ids)
    {
        _clock = clock;
        _ids = ids;
    }

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
        var reference = $"fake-{paymentId:N}-{_ids.NewId():N}";
        return Task.FromResult(new GatewayInitiation(reference, $"/payment/sandbox?ref={Uri.EscapeDataString(reference)}", _clock.UtcNow.AddMinutes(15)));
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
    private readonly IClock _clock;

    /// <summary>درگاه شکست‌خورده با ساعت تزریقی.</summary>
    public FakeFailingPaymentGateway(IClock clock) => _clock = clock;

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
        return Task.FromResult(new GatewayInitiation(reference, $"https://payments.test/tooba/{reference}", _clock.UtcNow.AddMinutes(15)));
    }

    /// <inheritdoc />
    public Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = providerRequestReference;
        _ = callbackClaimsSuccess;
        return Task.FromResult(new GatewayVerification(false, null, "GATEWAY_REJECTED"));
    }
}

/// <summary>
/// رجیستری درگاه‌های ثبت‌شده؛ کد ناشناخته را Fail-Closed به Fake نگه‌نمی‌دارد.
/// </summary>
public sealed class PaymentGatewayRegistry : IPaymentGatewayRegistry
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _gateways;

    /// <summary>رجیستری را از درگاه‌های DI می‌سازد.</summary>
    public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(x => x.ProviderCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IPaymentGateway Resolve(string providerCode)
    {
        if (_gateways.TryGetValue(providerCode.Trim(), out var gateway))
        {
            return gateway;
        }

        throw new InvalidOperationException("payment.gateway.unknown");
    }
}
