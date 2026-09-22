using Tooba.Settlement.Application;
using Tooba.Settlement.Contracts.Operations;
using SellerOrderRestoreSettlementGate = Tooba.Settlement.Contracts.Operations.SellerOrderRestoreSettlementGate;

namespace Tooba.Settlement.Infrastructure.Adapters;

/// <summary>
/// Contract-facing adapter over <see cref="ISettlementDirectory"/> so admin order callers never
/// reference Settlement Application/Domain types.
/// </summary>
internal sealed class SettlementOrderAccrualAdapter(ISettlementDirectory directory) : ISettlementOrderAccrualPort
{
    public async Task<IReadOnlyDictionary<Guid, SellerOrderRestoreSettlementGate>> GetRestoreGatesAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        var gates = await directory.GetRestoreSettlementGatesAsync(sellerOrderIds, cancellationToken);
        return gates.ToDictionary(
            pair => pair.Key,
            pair => new SellerOrderRestoreSettlementGate(
                pair.Value.SellerOrderId,
                pair.Value.HasPostedAccrual,
                pair.Value.HasCompletedPayoutEffect));
    }

    public Task NeutralizeUnpaidAccrualForCancelAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken) =>
        directory.NeutralizeUnpaidAccrualForCancelAsync(paymentId, sellerOrderIds, cancellationToken);

    public Task ReinstateAccrualAfterCancelRestoreAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken) =>
        directory.ReinstateAccrualAfterCancelRestoreAsync(paymentId, sellerOrderIds, cancellationToken);

    public Task VoidUnpaidAccrualForPaymentAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken) =>
        directory.VoidUnpaidAccrualForPaymentAsync(paymentId, sellerOrderIds, cancellationToken);
}
