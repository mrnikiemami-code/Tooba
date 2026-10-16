using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Settlement;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Settlement;

/// <summary>
/// درز خواندن گروه‌های ارسال پذیرفته‌شدهٔ سفارش برای Settlement، بدون cross-DbContext و بدون نوشتن.
/// </summary>
public sealed class OrderShippingGroupSettlementBridge : IOrderShippingGroupSettlementReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل را به schema order وصل می‌کند.</summary>
    public OrderShippingGroupSettlementBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderShippingGroupSettlementSnapshot>> GetForSellerOrdersAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        var ids = sellerOrderIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var groups = await _db.OrderShippingGroups.AsNoTracking()
            .Where(x => ids.Contains(x.SellerOrderId))
            .OrderBy(x => x.SellerOrderId)
            .ThenBy(x => x.ShippingGroupId)
            .ToListAsync(cancellationToken);
        return groups
            .Select(group => new OrderShippingGroupSettlementSnapshot(
                group.ShippingGroupId,
                group.CheckoutId,
                group.SellerOrderId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.State,
                group.ShippingChargeSnapshot,
                group.Currency))
            .ToArray();
    }
}
