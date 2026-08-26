using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Tooba.Payment.Application;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// Production adapter: Verify از StatusQuery می‌خواند؛ متن callback حقیقت نیست.
/// </summary>
public sealed class WebhookPaymentGateway : IPaymentGateway
{
    /// <summary>
    /// کد پایدار درگاه webhook-backed.
    /// </summary>
    public const string ProviderCodeValue = "webhook";

    /// <summary>
    /// override آزمایشی برای Verify بدون HTTP واقعی. PSP واقعی نیست.
    /// </summary>
    public static ConcurrentDictionary<string, GatewayVerification> TestStatusOverrides { get; } =
        new(StringComparer.Ordinal);

    private readonly HttpClient _http;
    private readonly PaymentGatewayOptions _options;
    private readonly PaymentGatewayInstrumentation _telemetry;

    /// <summary>
    /// درگاه Production با StatusQuery و webhook signing.
    /// </summary>
    public WebhookPaymentGateway(
        HttpClient http,
        IOptions<PaymentGatewayOptions> options,
        PaymentGatewayInstrumentation telemetry)
    {
        _http = http;
        _options = options.Value;
        _telemetry = telemetry;
        _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 3, 120));
    }

    /// <inheritdoc />
    public string ProviderCode => ProviderCodeValue;

    /// <inheritdoc />
    public Task<GatewayInitiation> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSigningSecret)
            || string.IsNullOrWhiteSpace(_options.StatusQueryBaseUrl))
        {
            _telemetry.RecordInitiate("misconfigured");
            throw new InvalidOperationException("payment.gateway.unconfigured");
        }

        _ = amount;
        _ = currency;
        var reference = $"wh-{paymentId:N}";
        _telemetry.RecordInitiate("succeeded");
        return Task.FromResult(new GatewayInitiation(
            reference,
            $"/payment/result?paymentId={paymentId:D}&ref={Uri.EscapeDataString(reference)}",
            DateTimeOffset.UtcNow.AddMinutes(30)));
    }

    /// <inheritdoc />
    public async Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = callbackClaimsSuccess;
        if (TestStatusOverrides.TryGetValue(providerRequestReference, out var testOverride))
        {
            _telemetry.RecordVerify(testOverride.VerifiedSuccess ? "succeeded" : "failed");
            return testOverride;
        }

        if (string.IsNullOrWhiteSpace(_options.StatusQueryBaseUrl))
        {
            _telemetry.RecordVerify("misconfigured");
            return new GatewayVerification(false, null, "GATEWAY_MISCONFIGURED");
        }

        var url = _options.StatusQueryBaseUrl.TrimEnd('/')
            + "?reference=" + Uri.EscapeDataString(providerRequestReference);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(_options.StatusQueryApiKey))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.StatusQueryApiKey);
        }

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                _telemetry.RecordVerify("rate_limited");
                return new GatewayVerification(false, null, "GATEWAY_RATE_LIMITED");
            }

            if (!response.IsSuccessStatusCode)
            {
                _telemetry.RecordVerify("unavailable");
                return new GatewayVerification(false, null, "GATEWAY_UNAVAILABLE");
            }

            var payload = await response.Content.ReadFromJsonAsync<StatusQueryPayload>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (payload is null)
            {
                _telemetry.RecordVerify("invalid_response");
                return new GatewayVerification(false, null, "GATEWAY_INVALID_RESPONSE");
            }

            if (payload.VerifiedSuccess
                && !string.IsNullOrWhiteSpace(payload.ProviderTransactionReference))
            {
                _telemetry.RecordVerify("succeeded");
                return new GatewayVerification(true, payload.ProviderTransactionReference, null);
            }

            _telemetry.RecordVerify("failed");
            return new GatewayVerification(false, null, payload.FailureCode ?? "GATEWAY_REJECTED");
        }
        catch (TaskCanceledException)
        {
            _telemetry.RecordVerify("timeout");
            return new GatewayVerification(false, null, "GATEWAY_TIMEOUT");
        }
        catch (HttpRequestException)
        {
            _telemetry.RecordVerify("unavailable");
            return new GatewayVerification(false, null, "GATEWAY_UNAVAILABLE");
        }
    }

    private sealed record StatusQueryPayload(
        [property: JsonPropertyName("verifiedSuccess")] bool VerifiedSuccess,
        [property: JsonPropertyName("providerTransactionReference")] string? ProviderTransactionReference,
        [property: JsonPropertyName("failureCode")] string? FailureCode);
}
