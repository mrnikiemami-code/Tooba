using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>خواندن QuantityShipped برای OrderSupply بدون افشای FulfillmentDbContext به Host.</summary>
public sealed class FulfillmentShippedQuantityReader : IFulfillmentShippedQuantityReader
{
    private readonly FulfillmentDbContext _db;

    /// <summary>Reader را به schema fulfillment وصل می‌کند.</summary>
    public FulfillmentShippedQuantityReader(FulfillmentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var fulfillmentIds = await _db.Fulfillments.AsNoTracking()
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .Select(x => x.FulfillmentId)
            .ToListAsync(cancellationToken);
        if (fulfillmentIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var rows = await _db.Items.AsNoTracking()
            .Where(x => fulfillmentIds.Contains(x.FulfillmentId))
            .Select(x => new { x.OrderLineId, x.QuantityShipped })
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityShipped));
    }
}
