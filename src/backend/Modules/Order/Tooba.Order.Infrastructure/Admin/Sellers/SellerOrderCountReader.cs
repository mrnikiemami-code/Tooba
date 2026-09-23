using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Admin.Sellers.Ports;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Admin.Sellers;

/// <summary>شمارش SellerOrder به ازای SellerPartyId.</summary>
internal sealed class SellerOrderCountReader(OrderDbContext orders) : ISellerOrderCountReader
{
    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsBySellerAsync(
        IReadOnlyList<Guid> sellerPartyIds,
        CancellationToken cancellationToken)
    {
        if (sellerPartyIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await orders.SellerOrders.AsNoTracking()
            .Where(x => sellerPartyIds.Contains(x.SellerPartyId))
            .GroupBy(x => x.SellerPartyId)
            .Select(g => new { SellerPartyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.SellerPartyId, x => x.Count);
    }
}
