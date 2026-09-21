using Tooba.Payment.Contracts.Settlement;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Infrastructure.Bridges;

/// <summary>
/// پل snapshot پرداخت برای Settlement از درز Payment.Contracts.
/// </summary>
public sealed class SettlementPaymentBridge : ISettlementPaymentReader
{
    private readonly IPaymentSettlementReader _payments;

    /// <summary>پل را به reader پرداخت وصل می‌کند.</summary>
    public SettlementPaymentBridge(IPaymentSettlementReader payments) => _payments = payments;

    /// <inheritdoc />
    public async Task<SettlementPaymentSnapshot?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetAsync(paymentId, cancellationToken);
        return payment is null
            ? null
            : new SettlementPaymentSnapshot(
                payment.PaymentId,
                payment.CheckoutId,
                payment.Amount,
                payment.Currency,
                payment.Succeeded);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SettlementPaymentAllocationSnapshot>> GetAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var allocations = await _payments.GetAllocationsAsync(paymentId, cancellationToken);
        return allocations
            .Select(x => new SettlementPaymentAllocationSnapshot(x.SellerOrderId, x.AllocatedAmount, x.Currency))
            .ToArray();
    }
}
