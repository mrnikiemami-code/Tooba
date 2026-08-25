using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Wishlist.Application;
using Tooba.Wishlist.Domain;
using Tooba.Wishlist.Infrastructure.Persistence;

namespace Tooba.Wishlist.Infrastructure;

/// <summary>پیاده‌سازی Wishlist که فقط schema خود و درگاه کاربردی Catalog را مصرف می‌کند.</summary>
public sealed class WishlistDirectory : IWishlistDirectory
{
    private readonly WishlistDbContext _db;
    private readonly ICatalogLookupGateway _catalog;

    /// <summary>وابستگی‌های مالک را بدون DbContext خارجی دریافت می‌کند.</summary>
    public WishlistDirectory(WishlistDbContext db, ICatalogLookupGateway catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<WishlistAddResult> AddAsync(Guid actorUserId, Guid productId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var product = await _catalog.FindReviewableProductByIdAsync(productId, cancellationToken);
        if (product is null || product.Status != CatalogPublicationStatus.Published)
            throw new InvalidOperationException("محصول منتشرشده پیدا نشد.");
        var existing = await _db.Items.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId && x.ProductId == productId, cancellationToken);
        if (existing is not null) return new(existing.WishlistItemId, false);

        var item = WishlistItem.Create(actorUserId, productId, DateTimeOffset.UtcNow);
        _db.Items.Add(item);
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            _db.Entry(item).State = EntityState.Detached;
            existing = await _db.Items.AsNoTracking()
                .SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId && x.ProductId == productId, cancellationToken);
            if (existing is not null) return new(existing.WishlistItemId, false);
            throw;
        }
        return new(item.WishlistItemId, true);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid actorUserId, Guid productId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var item = await _db.Items.SingleOrDefaultAsync(
            x => x.OwnerUserId == actorUserId && x.ProductId == productId, cancellationToken);
        if (item is null) return;
        _db.Items.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WishlistEntry>> ListAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        return await _db.Items.AsNoTracking().Where(x => x.OwnerUserId == actorUserId)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.WishlistItemId)
            .Select(x => new WishlistEntry(x.WishlistItemId, x.ProductId, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetMembershipAsync(
        Guid actorUserId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        if (productIds.Count == 0) return new HashSet<Guid>();
        var ids = await _db.Items.AsNoTracking()
            .Where(x => x.OwnerUserId == actorUserId && productIds.Contains(x.ProductId))
            .Select(x => x.ProductId).ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    /// <inheritdoc />
    public Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        return _db.Items.LongCountAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
    }

    private static void EnsureActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty) throw new InvalidOperationException("Actor معتبر الزامی است.");
    }
}
