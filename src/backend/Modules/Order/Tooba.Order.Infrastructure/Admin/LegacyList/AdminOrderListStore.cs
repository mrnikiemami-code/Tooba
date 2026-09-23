using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Admin.LegacyList.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Admin.LegacyList;

/// <summary>خواندن آخرین CheckoutGroupها برای فهرست سازگاری Admin Orders.</summary>
internal sealed class AdminOrderListStore(OrderDbContext orders) : IAdminOrderListStore
{
    public async Task<IReadOnlyList<CheckoutGroup>> ListLatestGroupsAsync(
        int take,
        CancellationToken cancellationToken) =>
        await orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
}
