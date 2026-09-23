using Microsoft.EntityFrameworkCore;
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

namespace Tooba.Order.Infrastructure.Fulfillment;

/// <summary>OrderDbContext-backed checkout gate for fulfillment admin ops.</summary>
public sealed class AdminOrderFulfillmentCheckoutReader : IAdminOrderFulfillmentCheckoutReader
{
    private readonly OrderDbContext _orders;

    /// <summary>Reader را می‌سازد.</summary>
    public AdminOrderFulfillmentCheckoutReader(OrderDbContext orders) => _orders = orders;

    /// <inheritdoc />
    public async Task<AdminOrderFulfillmentCheckoutSnapshot?> GetAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        var sellerIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var cancelled = sellerIds.Count > 0
            && group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled);
        return new AdminOrderFulfillmentCheckoutSnapshot(cancelled, sellerIds);
    }
}
