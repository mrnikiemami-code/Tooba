namespace Tooba.Payment.Contracts.Settlement;

/// <summary>Payment allocation snapshot for Settlement consumers.</summary>
public sealed record PaymentSettlementAllocationSnapshot(
    Guid SellerOrderId,
    decimal AllocatedAmount,
    string Currency);

/// <summary>Payment snapshot for Settlement without Payment.Domain leakage.</summary>
public sealed record PaymentSettlementSnapshot(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    bool Succeeded);

/// <summary>Cross-module payment read for Settlement accrual.</summary>
public interface IPaymentSettlementReader
{
    /// <summary>Reads payment snapshot.</summary>
    Task<PaymentSettlementSnapshot?> GetAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>Reads seller-order allocations for a payment.</summary>
    Task<IReadOnlyList<PaymentSettlementAllocationSnapshot>> GetAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken);
}
