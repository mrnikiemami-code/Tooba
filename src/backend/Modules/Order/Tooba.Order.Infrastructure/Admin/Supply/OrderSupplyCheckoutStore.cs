using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Admin.Supply.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Infrastructure.Admin.Supply;

/// <summary>OrderDbContext-backed checkout store for supply/recovery composition.</summary>
internal sealed class OrderSupplyCheckoutStore(OrderDbContext orders) : IOrderSupplyCheckoutStore
{
    public async Task<OrderSupplyCheckoutSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var group = await orders.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        return group is null ? null : Map(group);
    }

    public async Task<IReadOnlyList<OrderSupplyCheckoutSnapshot>> ListRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var list = await orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<OrderSupplyCheckoutSnapshot>> GetManyAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
        {
            return [];
        }

        var ids = checkoutIds.Distinct().ToList();
        var list = await orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => ids.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task ReplaceReservationsAsync(
        Guid checkoutId,
        IReadOnlyDictionary<Guid, Guid> bindingsByOrderLineId,
        CancellationToken cancellationToken)
    {
        if (bindingsByOrderLineId.Count == 0)
        {
            return;
        }

        var group = await orders.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("order.operation.invalid");

        foreach (var pair in bindingsByOrderLineId)
        {
            var orderLine = group.SellerOrders.SelectMany(o => o.Lines).Single(x => x.LineId == pair.Key);
            if (orderLine.ReservationId != pair.Value)
            {
                orderLine.ReplaceReservation(pair.Value);
            }
        }

        await orders.SaveChangesAsync(cancellationToken);
    }

    private static OrderSupplyCheckoutSnapshot Map(CheckoutGroup group) =>
        new(
            group.CheckoutId,
            group.SubmittedAt,
            group.SellerOrders.Select(o => new OrderSupplySellerOrderSnapshot(
                o.SellerOrderId,
                o.OrderNumber,
                o.Status,
                o.Lines.Select(l => new OrderSupplyCheckoutLineSnapshot(
                    l.LineId,
                    l.OfferId,
                    l.CategoryIdSnapshot,
                    l.ReservationId,
                    l.Quantity,
                    l.UnitDisplaySnapshot,
                    l.UnitCodeSnapshot)).ToList())).ToList());
}
