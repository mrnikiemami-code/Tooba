using Microsoft.EntityFrameworkCore;
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
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>Order DbContext-backed checkout line source for reservation-cycle orchestration.</summary>
public sealed class ReservationCycleCheckoutLineSource(OrderDbContext orders) : IReservationCycleCheckoutLineSource
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ReservationCyclePolicyLine>> LoadPolicyLinesAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await orders.Checkouts.AsNoTracking()
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> LoadReservationIdsAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await orders.Checkouts.AsNoTracking()
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
