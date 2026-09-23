using Tooba.BuildingBlocks;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.Admin.Supply.Services;
using Tooba.Order.Domain;

using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
namespace Tooba.Order.Application.ReservationCycle.Services;

/// <summary>
/// EnsureOrderSupply را با چرخه رزرو هم‌گام می‌کند؛ PaymentAttempt چرخه نیست.
/// Time via <see cref="IClock"/>; typed fault via <see cref="ContractOperationException"/>.
/// </summary>
public sealed class ReservationCycleCoordinator : IReservationCycleCoordinator
{
    private readonly IReservationCycleDirectory _cycles;
    private readonly IReservationCyclePolicyResolver _policy;
    private readonly OrderSupplyService _supply;
    private readonly IReservationCycleCheckoutLineSource _lines;
    private readonly IClock _clock;

    /// <summary>Directory + policy + supply + checkout line port + clock.</summary>
    public ReservationCycleCoordinator(
        IReservationCycleDirectory cycles,
        IReservationCyclePolicyResolver policy,
        OrderSupplyService supply,
        IReservationCycleCheckoutLineSource lines,
        IClock clock)
    {
        _cycles = cycles;
        _policy = policy;
        _supply = supply;
        _lines = lines;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ReservationCyclePolicySnapshot> ResolveForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var lines = await _lines.LoadPolicyLinesAsync(checkoutId, cancellationToken);
        return await _policy.ResolveAsync(lines, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EnsureOrderSupplyResult> EnsureRetryAfterExpiryAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await _cycles.CloseExpiredDueAsync(now, cancellationToken);
        var policy = await ResolveForCheckoutAsync(checkoutId, cancellationToken);
        var created = await _cycles.CountCreatedAsync(checkoutId, cancellationToken);
        var active = await _cycles.GetActiveAsync(checkoutId, cancellationToken);
        if (active is not null)
        {
            return (await _supply.EnsureAsync(
                checkoutId,
                OrderSupplyMode.EnsureUnpaidRetryHold,
                allowReacquire: false,
                reason: "active-cycle-retry",
                cancellationToken)).Value;
        }

        if (created >= policy.MaxCycles)
        {
            await _cycles.RecordRetryLimitReachedAsync(checkoutId, now, cancellationToken);
            throw new ContractOperationException(ReservationCycleErrors.RetryLimitReached);
        }

        await _cycles.RecordReacquireRequestedAsync(checkoutId, now, cancellationToken);
        var result = (await _supply.EnsureAsync(
            checkoutId,
            OrderSupplyMode.EnsureUnpaidRetryHold,
            allowReacquire: true,
            reason: "unpaid-retry",
            cancellationToken)).Value;
        if (result.Status is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable
            || result.Outcome is OrderSupplyOutcome.Unavailable or OrderSupplyOutcome.PartiallyUnavailable)
        {
            await _cycles.RecordReacquireFailedAsync(checkoutId, now, result.Status.ToString(), cancellationToken);
            return result;
        }

        var reservationIds = result.NewBindingsByOrderLineId.Values.Distinct().ToArray();
        if (reservationIds.Length == 0)
        {
            reservationIds = (await _lines.LoadReservationIdsAsync(checkoutId, cancellationToken)).ToArray();
        }

        await _cycles.StartAsync(
            checkoutId,
            ReservationCycleReason.RetryAfterExpiry,
            now,
            now.AddMinutes(policy.RetryHoldMinutes),
            policy,
            reservationIds,
            actor: "unpaid-retry",
            correlationId: $"cycle:{checkoutId:N}:{created + 1}",
            paymentAttemptId: null,
            cancellationToken);
        return result;
    }
}
