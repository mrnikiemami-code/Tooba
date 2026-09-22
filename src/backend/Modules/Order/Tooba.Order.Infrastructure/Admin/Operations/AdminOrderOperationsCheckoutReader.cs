using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Infrastructure.Persistence;

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
