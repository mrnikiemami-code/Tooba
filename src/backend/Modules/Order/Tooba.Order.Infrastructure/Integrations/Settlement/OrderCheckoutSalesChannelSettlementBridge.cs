using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Settlement;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Settlement;

/// <summary>
/// درز خواندن کانال فروش پایدار checkout برای Settlement، بدون cross-DbContext و بدون نوشتن.
/// کانال فقط از وضعیت پایدار <see cref="Tooba.Order.Domain.Aggregates.CheckoutGroup"/> خوانده می‌شود.
/// </summary>
public sealed class OrderCheckoutSalesChannelSettlementBridge : IOrderCheckoutSalesChannelSettlementReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل را به schema order وصل می‌کند.</summary>
    public OrderCheckoutSalesChannelSettlementBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<OrderCheckoutSalesChannelSnapshot?> GetChannelAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty)
        {
            return null;
        }

        var channel = await _db.Checkouts.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .Select(x => (Tooba.Offer.Contracts.Dtos.SalesChannel?)x.Channel)
            .SingleOrDefaultAsync(cancellationToken);
        return channel is null
            ? null
            : new OrderCheckoutSalesChannelSnapshot(checkoutId, channel.Value);
    }
}
