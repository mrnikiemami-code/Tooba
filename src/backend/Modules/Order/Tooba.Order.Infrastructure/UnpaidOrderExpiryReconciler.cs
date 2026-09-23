using Tooba.BuildingBlocks;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Customer;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// Order-owned unpaid expiry reconciliation: Payment.Contracts expiry + payment projection +
/// reservation cycle directory + <see cref="IClock"/>.
/// </summary>
public sealed class UnpaidOrderExpiryReconciler : IUnpaidOrderExpiryReconciler
{
    private readonly IPaymentCustomerGateway _expiry;
    private readonly IOrderPaymentProjectionPort _projection;
    private readonly IReservationCycleDirectory _cycles;
    private readonly IClock _clock;

    /// <summary>Payment gateway + Order projection + cycle directory + clock.</summary>
    public UnpaidOrderExpiryReconciler(
        IPaymentCustomerGateway expiry,
        IOrderPaymentProjectionPort projection,
        IReservationCycleDirectory cycles,
        IClock clock)
    {
        _expiry = expiry;
        _projection = projection;
        _cycles = cycles;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<int> ReconcileAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await _cycles.CloseExpiredDueAsync(now, cancellationToken).ConfigureAwait(false);
        var checkoutIds = await _expiry.ExpireDueUnpaidAsync(
            now,
            batchSize,
            cancellationToken).ConfigureAwait(false);
        foreach (var checkoutId in checkoutIds.Distinct())
        {
            await _projection.ReleaseReservationsAfterManualRejectAsync(checkoutId, cancellationToken)
                .ConfigureAwait(false);
            var active = await _cycles.GetActiveAsync(checkoutId, cancellationToken).ConfigureAwait(false);
            if (active is not null)
            {
                await _cycles.CloseActiveAsync(
                    checkoutId,
                    ReservationCycleStatus.ReleasedByPolicy,
                    now,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return checkoutIds.Count;
    }
}
