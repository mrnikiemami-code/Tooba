using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// webhook امضاشده را dedup می‌کند و Verify را صدا می‌زند؛ متن status حقیقت نیست.
/// </summary>
public sealed class PaymentWebhookHandler : IPaymentWebhookHandler
{
    private readonly PaymentDbContext _db;
    private readonly IPaymentDirectory _payments;
    private readonly PaymentGatewayInstrumentation _telemetry;

    /// <summary>
    /// handler را به schema payment و دایرکتوری وصل می‌کند.
    /// </summary>
    public PaymentWebhookHandler(
        PaymentDbContext db,
        IPaymentDirectory payments,
        PaymentGatewayInstrumentation telemetry)
    {
        _db = db;
        _payments = payments;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<PaymentWebhookHandleResult> HandleAsync(
        string providerCode,
        PaymentWebhookNotification notification,
        CancellationToken cancellationToken)
    {
        var eventId = notification.ProviderEventId.Trim();
        if (string.IsNullOrWhiteSpace(eventId))
        {
            _telemetry.RecordWebhook("invalid_payload");
            return new PaymentWebhookHandleResult(false, false, "payment.webhook.invalid_payload");
        }

        var duplicate = await _db.WebhookInbox.AsNoTracking().AnyAsync(
            x => x.ProviderCode == providerCode && x.ProviderEventId == eventId,
            cancellationToken).ConfigureAwait(false);
        if (duplicate)
        {
            _telemetry.RecordWebhook("duplicate");
            return new PaymentWebhookHandleResult(true, true, null);
        }

        var payment = await _db.Payments.SingleOrDefaultAsync(
            x => x.PaymentId == notification.PaymentId,
            cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            _telemetry.RecordWebhook("missing_payment");
            return new PaymentWebhookHandleResult(false, false, "payment.missing");
        }

        if (!string.Equals(payment.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase))
        {
            _telemetry.RecordWebhook("provider_mismatch");
            return new PaymentWebhookHandleResult(false, false, "payment.webhook.provider_mismatch");
        }

        if (payment.Amount != notification.Amount
            || !string.Equals(payment.Currency, notification.Currency, StringComparison.OrdinalIgnoreCase))
        {
            _telemetry.RecordWebhook("amount_mismatch");
            return new PaymentWebhookHandleResult(false, false, "payment.webhook.amount_mismatch");
        }

        var attempt = await _db.Attempts.SingleOrDefaultAsync(
            x => x.AttemptId == notification.AttemptId && x.PaymentId == payment.PaymentId,
            cancellationToken).ConfigureAwait(false);
        if (attempt is null
            || attempt.ProviderRequestReference != notification.ProviderRequestReference)
        {
            _telemetry.RecordWebhook("attempt_mismatch");
            return new PaymentWebhookHandleResult(false, false, "payment.webhook.attempt_mismatch");
        }

        _db.WebhookInbox.Add(PaymentWebhookInboxRecord.Create(
            providerCode,
            eventId,
            payment.PaymentId,
            DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var claimsSuccess = string.Equals(notification.Status, "succeeded", StringComparison.OrdinalIgnoreCase);
        await _payments.VerifyAsync(
            new VerifyPaymentCommand(
                notification.PaymentId,
                notification.AttemptId,
                notification.ProviderRequestReference,
                claimsSuccess),
            cancellationToken).ConfigureAwait(false);

        _telemetry.RecordWebhook("accepted");
        return new PaymentWebhookHandleResult(true, false, null);
    }
}
