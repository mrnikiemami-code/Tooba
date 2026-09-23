using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Admin.Operations.Ports;
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

namespace Tooba.Order.Infrastructure.Admin.Operations;

internal sealed class AdminOrderOperationsCheckoutReader(OrderDbContext orders) : IAdminOrderOperationsCheckoutReader
{
    public async Task<AdminOrderOpsCheckoutSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var group = await orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        return new AdminOrderOpsCheckoutSnapshot(
            group.CheckoutId,
            group.PlacedByUserId,
            group.SellerOrders.Select(order => new AdminOrderOpsSellerOrderSnapshot(
                order.SellerOrderId,
                order.SellerPartyId,
                order.Status,
                order.CancelledFromStatus,
                order.Lines.Select(line => new AdminOrderOpsLineSnapshot(line.LineId, line.Quantity)).ToList()))
            .ToList());
    }
}
