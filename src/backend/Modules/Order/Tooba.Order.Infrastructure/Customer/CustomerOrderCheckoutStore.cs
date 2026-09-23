using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Customer.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Customer;

/// <summary>خواندن Checkout مشتری با فیلتر مالکیت PlacedByUserId.</summary>
internal sealed class CustomerOrderCheckoutStore(OrderDbContext orders) : ICustomerOrderCheckoutStore
{
    public async Task<IReadOnlyList<CheckoutGroup>> ListByActorAsync(
        Guid actorUserId,
        int take,
        CancellationToken cancellationToken) =>
        await orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .Where(x => x.PlacedByUserId == actorUserId)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<CheckoutGroup?> GetOwnedAsync(
        Guid actorUserId,
        Guid checkoutId,
        CancellationToken cancellationToken) =>
        await orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(
                x => x.CheckoutId == checkoutId && x.PlacedByUserId == actorUserId,
                cancellationToken);
}
