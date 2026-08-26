using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure;

namespace Tooba.Host.Payments;

/// <summary>
/// HTTP webhook/callback پرداخت با اعتبارسنجی امضا و dedup inbox.
/// </summary>
public static class PaymentWebhookEndpoints
{
    /// <summary>
    /// مسیر webhook پرداخت را ثبت می‌کند.
    /// </summary>
    public static void MapPaymentWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/v1/payments/webhooks/{providerCode}", HandleWebhookAsync);
    }

    private static async Task<IResult> HandleWebhookAsync(
        string providerCode,
        HttpRequest request,
        IPaymentWebhookHandler handler,
        IOptions<PaymentGatewayOptions> options,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var bodyText = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        request.Body.Position = 0;
        var bodyBytes = Encoding.UTF8.GetBytes(bodyText);

        if (!PaymentWebhookSignatureValidator.TryValidate(
                options.Value.WebhookSigningSecret,
                bodyBytes,
                request.Headers[PaymentWebhookSignatureValidator.SignatureHeaderName],
                out var signatureError))
        {
            return Results.Json(
                new { title = "Unauthorized", errorCode = signatureError, detail = "امضای webhook معتبر نیست." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        PaymentWebhookNotification? notification;
        try
        {
            notification = JsonSerializer.Deserialize<PaymentWebhookNotification>(
                bodyText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = "payment.webhook.invalid_payload", detail = "بدنهٔ webhook نامعتبر است." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (notification is null
            || notification.PaymentId == Guid.Empty
            || notification.AttemptId == Guid.Empty
            || string.IsNullOrWhiteSpace(notification.ProviderEventId)
            || string.IsNullOrWhiteSpace(notification.ProviderRequestReference))
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = "payment.webhook.invalid_payload", detail = "فیلدهای webhook ناقص است." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await handler.HandleAsync(providerCode.Trim(), notification, cancellationToken).ConfigureAwait(false);
        if (!result.Accepted)
        {
            var status = result.ErrorCode switch
            {
                "payment.missing" => StatusCodes.Status404NotFound,
                "payment.webhook.amount_mismatch"
                    or "payment.webhook.attempt_mismatch"
                    or "payment.webhook.provider_mismatch" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };
            return Results.Json(
                new { title = "Rejected", errorCode = result.ErrorCode, detail = "webhook پرداخت رد شد." },
                statusCode: status);
        }

        return Results.Json(new
        {
            accepted = true,
            duplicate = result.Duplicate,
        });
    }
}
