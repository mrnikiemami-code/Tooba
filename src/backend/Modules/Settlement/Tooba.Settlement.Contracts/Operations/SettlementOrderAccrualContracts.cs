namespace Tooba.Settlement.Contracts.Operations;

/// <summary>Read-only restore gate of one seller order against completed payout effects.</summary>
public sealed record SellerOrderRestoreSettlementGate(
    Guid SellerOrderId,
    bool HasPostedAccrual,
    bool HasCompletedPayoutEffect);

/// <summary>
/// Settlement accrual effects required by admin order cancel/restore/unconfirm flows,
/// exposed without owner Application/Domain types. Implemented by Settlement.Infrastructure.
/// </summary>
public interface ISettlementOrderAccrualPort
{
    /// <summary>Restore gates for the given seller orders.</summary>
    Task<IReadOnlyDictionary<Guid, SellerOrderRestoreSettlementGate>> GetRestoreGatesAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>Neutralize the unpaid accrual with a debit row when the order is cancelled.</summary>
    Task NeutralizeUnpaidAccrualForCancelAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>Reinstate the neutralized accrual with a credit row after a cancel restore.</summary>
    Task ReinstateAccrualAfterCancelRestoreAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>Void the unpaid accrual when a deposit confirmation is reverted.</summary>
    Task VoidUnpaidAccrualForPaymentAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}
