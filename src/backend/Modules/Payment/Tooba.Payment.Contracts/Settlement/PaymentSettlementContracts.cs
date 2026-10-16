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

/// <summary>
/// Per-group shipping charge actually allocated by the customer payment for one stable
/// <c>ShippingGroupId</c>. It is the customer-facing charge only: never a carrier cost, seller or
/// marketplace revenue share.
/// </summary>
public sealed record PaymentSettlementShippingGroupAllocationSnapshot(
    Guid ShippingGroupId,
    decimal AllocatedAmount,
    string Currency);

/// <summary>Cross-module payment read for Settlement accrual.</summary>
public interface IPaymentSettlementReader
{
    /// <summary>Reads payment snapshot.</summary>
    Task<PaymentSettlementSnapshot?> GetAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>Reads seller-order allocations for a payment.</summary>
    Task<IReadOnlyList<PaymentSettlementAllocationSnapshot>> GetAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads per-group shipping charge allocations for a payment. A legacy ungrouped order keeps its
    /// single <c>StoreShipping</c> sentinel allocation and therefore yields no group rows here.
    /// </summary>
    Task<IReadOnlyList<PaymentSettlementShippingGroupAllocationSnapshot>> GetShippingGroupAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken);
}
