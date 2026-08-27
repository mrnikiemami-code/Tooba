using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Tooba.Payment.Application;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// Production adapter boundary: Initiate از InitiateBaseUrl؛ Verify از StatusQuery؛ متن callback حقیقت نیست.
/// هیچ برند تجاری PSP در کد انتخاب نمی‌شود — فقط پیکربندی امن.
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
        _ = cancellationToken;
        if (!IsFullyConfigured())
        {
            _telemetry.RecordInitiate("misconfigured");
            throw new InvalidOperationException("payment.gateway.unconfigured");
        }

        if (!TryValidateOutboundUrl(_options.InitiateBaseUrl, out _))
        {
            _telemetry.RecordInitiate("misconfigured");
            throw new InvalidOperationException("payment.gateway.unconfigured");
        }

        var reference = $"wh-{paymentId:N}";
        var redirect = BuildInitiateRedirect(paymentId, amount, currency, reference);
        _telemetry.RecordInitiate("succeeded");
        return Task.FromResult(new GatewayInitiation(
            reference,
            redirect,
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

        if (string.IsNullOrWhiteSpace(_options.StatusQueryBaseUrl)
            || string.IsNullOrWhiteSpace(_options.WebhookSigningSecret))
        {
            _telemetry.RecordVerify("misconfigured");
            return new GatewayVerification(false, null, "GATEWAY_MISCONFIGURED");
        }

        if (!TryValidateOutboundUrl(_options.StatusQueryBaseUrl, out var statusBase))
        {
            _telemetry.RecordVerify("ssrf_blocked");
            return new GatewayVerification(false, null, "GATEWAY_MISCONFIGURED");
        }

        var maxAttempts = Math.Clamp(_options.VerifyMaxAttempts, 1, 5);
        GatewayVerification last = new(false, null, "GATEWAY_UNAVAILABLE");
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            last = await QueryStatusOnceAsync(statusBase!, providerRequestReference, cancellationToken)
                .ConfigureAwait(false);
            if (last.VerifiedSuccess
                || !PaymentGatewayOutcomes.IsIndeterminate(last.FailureCode))
            {
                return last;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return last;
    }

    private async Task<GatewayVerification> QueryStatusOnceAsync(
        string statusBase,
        string providerRequestReference,
        CancellationToken cancellationToken)
    {
        var url = statusBase.TrimEnd('/')
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

            var status = (payload.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (status is "pending" or "unknown" or "processing")
            {
                _telemetry.RecordVerify("pending");
                return new GatewayVerification(false, null, "GATEWAY_PENDING");
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

    private bool IsFullyConfigured() =>
        !string.IsNullOrWhiteSpace(_options.WebhookSigningSecret)
        && !string.IsNullOrWhiteSpace(_options.StatusQueryBaseUrl)
        && !string.IsNullOrWhiteSpace(_options.InitiateBaseUrl);

    private string BuildInitiateRedirect(Guid paymentId, decimal amount, string currency, string reference)
    {
        var sep = _options.InitiateBaseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return _options.InitiateBaseUrl.TrimEnd('/')
            + sep
            + "paymentId=" + Uri.EscapeDataString(paymentId.ToString("D"))
            + "&amount=" + Uri.EscapeDataString(amount.ToString(System.Globalization.CultureInfo.InvariantCulture))
            + "&currency=" + Uri.EscapeDataString(currency)
            + "&reference=" + Uri.EscapeDataString(reference)
            + "&returnPath=" + Uri.EscapeDataString($"/payment/result?paymentId={paymentId:D}");
    }

    private bool TryValidateOutboundUrl(string raw, out string? normalized)
    {
        normalized = null;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Production boundary: prefer https; allow http only for explicitly listed hosts (test harness).
        var host = uri.Host;
        if (_options.AllowedStatusQueryHosts is { Length: > 0 })
        {
            if (!_options.AllowedStatusQueryHosts.Any(h =>
                    string.Equals(h, host, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }
        else
        {
            if (IsLoopbackOrPrivate(host))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return true;
    }

    private static bool IsLoopbackOrPrivate(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(host, out var ip))
        {
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            var bytes = ip.GetAddressBytes();
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && bytes.Length >= 2)
            {
                if (bytes[0] == 10
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 169 && bytes[1] == 254))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private sealed record StatusQueryPayload(
        [property: JsonPropertyName("verifiedSuccess")] bool VerifiedSuccess,
        [property: JsonPropertyName("providerTransactionReference")] string? ProviderTransactionReference,
        [property: JsonPropertyName("failureCode")] string? FailureCode,
        [property: JsonPropertyName("status")] string? Status);
}
