#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.Admin.Supply.Services;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Host;

/// <summary>EnsureOrderSupply را با چرخه رزرو هم‌گام می‌کند؛ PaymentAttempt چرخه نیست.</summary>
public sealed class ReservationCycleCoordinator
{
    private readonly IReservationCycleDirectory _cycles;
    private readonly IReservationCyclePolicyResolver _policy;
    private readonly OrderSupplyService _supply;
    private readonly OrderDbContext _orders;

    public ReservationCycleCoordinator(
        IReservationCycleDirectory cycles,
        IReservationCyclePolicyResolver policy,
        OrderSupplyService supply,
        OrderDbContext orders)
    {
        _cycles = cycles;
        _policy = policy;
        _supply = supply;
        _orders = orders;
    }

    public async Task<ReservationCyclePolicySnapshot> ResolveForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var lines = await LoadPolicyLinesAsync(checkoutId, cancellationToken);
        return await _policy.ResolveAsync(lines, cancellationToken);
    }

    public async Task<EnsureOrderSupplyResult> EnsureRetryAfterExpiryAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
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
            throw new InvalidOperationException(ReservationCycleErrors.RetryLimitReachedFa);
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
            reservationIds = (await LoadReservationIdsAsync(checkoutId, cancellationToken)).ToArray();
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

    private async Task<IReadOnlyList<ReservationCyclePolicyLine>> LoadPolicyLinesAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return [];
        }

        return group.SellerOrders
            .Where(x => x.Status != SellerOrderStatus.Cancelled)
            .SelectMany(x => x.Lines)
            .Select(x => new ReservationCyclePolicyLine(x.OfferId, x.CategoryIdSnapshot))
            .ToArray();
    }

    private async Task<IReadOnlyList<Guid>> LoadReservationIdsAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return [];
        }

        return group.SellerOrders
            .SelectMany(x => x.Lines)
            .Where(x => x.ReservationId.HasValue)
            .Select(x => x.ReservationId!.Value)
            .Distinct()
            .ToArray();
    }
}
