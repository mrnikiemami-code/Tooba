using Tooba.BuildingBlocks;
using Tooba.Payment.Application;
using Tooba.Returns.Application;
using Tooba.Settlement.Application;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// مصرف‌کننده payment.succeeded.v1 برای accrual idempotent.
/// </summary>
public sealed class SettlementPaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
{
    private readonly SettlementDirectory _settlement;

    /// <summary>handler را به دایرکتوری settlement وصل می‌کند.</summary>
    public SettlementPaymentSucceededHandler(SettlementDirectory settlement) => _settlement = settlement;

    /// <inheritdoc />
    public Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        _settlement.AccrueFromPaymentAsync(
            integrationEvent.PaymentId,
            integrationEvent.Metadata.EventId,
            integrationEvent.SellerOrderIds,
            cancellationToken);
}

/// <summary>
/// مصرف‌کننده refund.succeeded.v1 برای adjustment idempotent.
/// </summary>
public sealed class SettlementRefundSucceededHandler : IIntegrationEventHandler<RefundSucceededIntegrationEvent>
{
    private readonly SettlementDirectory _settlement;

    /// <summary>handler را به دایرکتوری settlement وصل می‌کند.</summary>
    public SettlementRefundSucceededHandler(SettlementDirectory settlement) => _settlement = settlement;

    /// <inheritdoc />
    public Task HandleAsync(RefundSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        _settlement.AdjustFromRefundAsync(
            integrationEvent.ReturnRequestId,
            integrationEvent.RefundAmount,
            integrationEvent.Currency,
            integrationEvent.Metadata.EventId,
            cancellationToken);
}
