using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Domain;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

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
