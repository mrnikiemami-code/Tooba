using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Domain;

namespace Tooba.Host.Tests;

internal static class AdminOrderOpsTestMapping
{
    internal static AdminOrderOpsSellerOrderSnapshot ToOps(this SellerOrder order) =>
        new(
            order.SellerOrderId,
            order.SellerPartyId,
            order.Status,
            order.CancelledFromStatus,
            order.Lines.Select(l => new AdminOrderOpsLineSnapshot(l.LineId, l.Quantity)).ToList());

    internal static AdminOrderOpsCheckoutSnapshot ToOps(this CheckoutGroup group) =>
        new(
            group.CheckoutId,
            group.PlacedByUserId,
            group.SellerOrders.Select(ToOps).ToList());
}
