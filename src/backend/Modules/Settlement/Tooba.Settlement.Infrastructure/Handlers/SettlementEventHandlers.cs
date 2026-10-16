using Tooba.Returns.Contracts.Events;
using Tooba.Payment.Contracts.Events;
using Tooba.BuildingBlocks;
using Tooba.Settlement.Infrastructure.Directories;

namespace Tooba.Settlement.Infrastructure.Handlers;

/// <summary>
/// مصرف‌کننده payment.succeeded.v1 برای accrual idempotent.
/// </summary>
public sealed class SettlementPaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
{
    private readonly SettlementDirectory _settlement;

    /// <summary>handler را به دایرکتوری settlement وصل می‌کند.</summary>
    public SettlementPaymentSucceededHandler(SettlementDirectory settlement) => _settlement = settlement;

    /// <inheritdoc />
    public async Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // حساب‌داری هزینهٔ ارسال per-group پیش از accrual ثبت می‌شود تا ناسازگاری charge پذیرفتهٔ گروه با
        // مبلغ پرداخت‌شده، fail-closed باشد و هیچ اعتبار merchandise فروشنده ساخته نشود.
        await _settlement.RecordShippingGroupFeesAsync(
            integrationEvent.PaymentId,
            integrationEvent.SellerOrderIds,
            cancellationToken);
        await _settlement.AccrueFromPaymentAsync(
            integrationEvent.PaymentId,
            integrationEvent.Metadata.EventId,
            integrationEvent.SellerOrderIds,
            cancellationToken);
    }
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
