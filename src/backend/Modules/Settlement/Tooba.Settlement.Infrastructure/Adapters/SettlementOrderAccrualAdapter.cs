using Tooba.BuildingBlocks;
using Tooba.Settlement.Application;
using Tooba.Settlement.Application.Errors;
using Tooba.Settlement.Contracts.Operations;
using SellerOrderRestoreSettlementGate = Tooba.Settlement.Contracts.Operations.SellerOrderRestoreSettlementGate;

namespace Tooba.Settlement.Infrastructure.Adapters;

/// <summary>
/// Contract-facing adapter over <see cref="ISettlementDirectory"/> so admin order callers never
/// reference Settlement Application/Domain types.
/// Expected failures cross the boundary as <see cref="ContractOperationException"/> (stable Code).
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
        GuardAsync(() => directory.NeutralizeUnpaidAccrualForCancelAsync(paymentId, sellerOrderIds, cancellationToken));

    public Task ReinstateAccrualAfterCancelRestoreAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken) =>
        GuardAsync(() => directory.ReinstateAccrualAfterCancelRestoreAsync(paymentId, sellerOrderIds, cancellationToken));

    public Task VoidUnpaidAccrualForPaymentAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken) =>
        GuardAsync(() => directory.VoidUnpaidAccrualForPaymentAsync(paymentId, sellerOrderIds, cancellationToken));

    private static async Task GuardAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (TryPromote(ex, out var fault))
        {
            throw fault;
        }
    }

    private static bool TryPromote(InvalidOperationException ex, out ContractOperationException fault)
    {
        if (SettlementExceptionMapper.TryMapExact(ex.Message, out var error))
        {
            fault = new ContractOperationException(error.Code, ex);
            return true;
        }

        if (ContractOperationFault.LooksLikeStableCode(ex.Message))
        {
            fault = new ContractOperationException(ex.Message, ex);
            return true;
        }

        fault = null!;
        return false;
    }
}
