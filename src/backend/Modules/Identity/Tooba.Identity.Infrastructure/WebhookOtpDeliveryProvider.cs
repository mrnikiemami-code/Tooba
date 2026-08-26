using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Tooba.Identity.Application;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// Production webhook adapter: POST JSON to configured provider endpoint.
/// </summary>
public sealed class WebhookOtpDeliveryProvider : IOtpDeliveryProvider
{
    private readonly HttpClient _http;
    private readonly OtpDeliveryOptions _options;
    private readonly OtpDeliveryInstrumentation _telemetry;

    /// <summary>Webhook-backed production OTP delivery.</summary>
    public WebhookOtpDeliveryProvider(
        HttpClient http,
        IOptions<OtpDeliveryOptions> options,
        OtpDeliveryInstrumentation telemetry)
    {
        _http = http;
        _options = options.Value;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<OtpDeliveryOutcome> DeliverAsync(OtpDeliveryMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _telemetry.RecordDelivery("misconfigured");
            return new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.Misconfigured);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.WebhookUrl)
        {
            Content = JsonContent.Create(new WebhookPayload(message.Purpose.ToString(), message.Destination, message.OneTimeCode)),
        };
        if (!string.IsNullOrWhiteSpace(_options.WebhookApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.WebhookApiKey);
        }

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                _telemetry.RecordDelivery("rate_limited");
                return new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.RateLimited);
            }

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            {
                _telemetry.RecordDelivery("invalid_destination");
                return new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.InvalidDestination);
            }

            if (!response.IsSuccessStatusCode)
            {
                _telemetry.RecordDelivery("unavailable");
                return new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.Unavailable);
            }

            var correlation = response.Headers.TryGetValues("X-Correlation-Id", out var values)
                ? values.FirstOrDefault()
                : null;
            _telemetry.RecordDelivery("succeeded");
            return new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.Succeeded, correlation);
        }
        catch (TaskCanceledException)
        {
            _telemetry.RecordDelivery("unavailable");
            return new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.Unavailable);
        }
        catch (HttpRequestException)
        {
            _telemetry.RecordDelivery("unavailable");
            return new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.Unavailable);
        }
    }

    private sealed record WebhookPayload(
        [property: JsonPropertyName("purpose")] string Purpose,
        [property: JsonPropertyName("destination")] string Destination,
        [property: JsonPropertyName("code")] string Code);
}
