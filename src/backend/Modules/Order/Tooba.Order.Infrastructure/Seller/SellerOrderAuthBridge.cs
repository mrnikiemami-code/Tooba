using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Seller;

/// <summary>
/// احراز محدودهٔ سفارش فروشنده برای Fulfillment؛ Catalog fallback داخل Order می‌ماند.
/// </summary>
public sealed class SellerOrderAuthBridge : ISellerOrderAuthReader
{
    private readonly OrderDbContext _db;
    private readonly ICatalogLookupGateway _catalog;

    /// <summary>پل را به Order + Catalog lookup وصل می‌کند.</summary>
    public SellerOrderAuthBridge(OrderDbContext db, ICatalogLookupGateway catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<SellerOrderAuthSnapshot?> GetForSellerAsync(
        Guid sellerOrderId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var order = await _db.SellerOrders.AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(
                x => x.SellerOrderId == sellerOrderId && x.SellerPartyId == sellerPartyId,
                cancellationToken);
        if (order is null)
        {
            return null;
        }

        var missing = order.Lines
            .Where(l => l.CategoryIdSnapshot is null)
            .Select(l => l.CatalogVariantId)
            .Distinct()
            .ToArray();
        var resolved = missing.Length == 0
            ? new Dictionary<Guid, Guid?>()
            : await _catalog.GetPrimaryCategoryIdsByVariantIdsAsync(missing, cancellationToken);

        var lines = order.Lines.Select(l =>
        {
            var categoryId = l.CategoryIdSnapshot ?? resolved.GetValueOrDefault(l.CatalogVariantId);
            return new SellerOrderAuthLine(l.LineId, l.CatalogVariantId, categoryId);
        }).ToArray();

        return new SellerOrderAuthSnapshot(order.SellerOrderId, order.SellerPartyId, lines);
    }
}
