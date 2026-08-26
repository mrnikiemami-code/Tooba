using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Payment.Application;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// مصرف‌کننده payment.succeeded.v1 برای ایجاد idempotent fulfillment.
/// </summary>
public sealed class FulfillmentPaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
{
    private readonly FulfillmentDirectory _fulfillment;

    /// <summary>
    /// handler را به دایرکتوری fulfillment وصل می‌کند.
    /// </summary>
    public FulfillmentPaymentSucceededHandler(FulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    /// <inheritdoc />
    public Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        _fulfillment.CreateFromPaidSellerOrdersAsync(
            integrationEvent.PaymentId,
            integrationEvent.Metadata.EventId,
            integrationEvent.SellerOrderIds,
            cancellationToken);
}
