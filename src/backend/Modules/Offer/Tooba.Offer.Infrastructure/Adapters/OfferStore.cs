using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Application.Mappings;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Infrastructure.Persistence;
using DomainOfferStatus = Tooba.Offer.Domain.ValueObjects.OfferStatus;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>EF-backed persistence adapter for Offer aggregates and Host read queries.</summary>
public sealed class OfferStore(OfferDbContext db) : IOfferStore, IOfferQueryGateway
{
    /// <inheritdoc />
    public Task<SellerOffer?> GetByIdAsync(Guid offerId, CancellationToken token) =>
        db.Offers.SingleOrDefaultAsync(x => x.OfferId == offerId, token);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerOffer>> ListBySellerAsync(Guid sellerPartyId, CancellationToken token) =>
        await db.Offers.AsNoTracking().Where(x => x.SellerPartyId == sellerPartyId && x.Status != DomainOfferStatus.Archived)
            .OrderByDescending(x => x.OfferId).ToListAsync(token);

    /// <inheritdoc />
    public Task AddAsync(SellerOffer offer, CancellationToken token)
    {
        db.Offers.Add(offer);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken token) => await db.SaveChangesAsync(token);

    /// <inheritdoc />
    public Task<bool> ExistsActiveListingAsync(Guid sellerId, Guid variantId, Tooba.Offer.Domain.ValueObjects.SalesChannel channel, CancellationToken token) =>
        db.Offers.AnyAsync(x => x.SellerPartyId == sellerId && x.CatalogVariantId == variantId
            && x.Channel == channel && x.Status != DomainOfferStatus.Archived, token);

    /// <inheritdoc />
    public Task<bool> ExistsSellerSkuAsync(Guid sellerId, string sku, Guid? excludingId, CancellationToken token) =>
        db.Offers.AnyAsync(x => x.SellerPartyId == sellerId && x.SellerSku == sku
            && (!excludingId.HasValue || x.OfferId != excludingId.Value), token);

    /// <inheritdoc />
    public async Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken token) =>
        (await db.Offers.AsNoTracking().SingleOrDefaultAsync(x => x.OfferId == offerId, token))?.ToReference();

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(IReadOnlyCollection<Guid> offerIds, CancellationToken token)
    {
        if (offerIds.Count == 0) return new Dictionary<Guid, OfferReference>();
        var ids = offerIds.Distinct().ToArray();
        return (await db.Offers.AsNoTracking().Where(x => ids.Contains(x.OfferId)).ToListAsync(token))
            .ToDictionary(x => x.OfferId, x => x.ToReference());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken token)
    {
        var ids = variantIds.Distinct().ToArray();
        var result = ids.ToDictionary(x => x, _ => 0);
        if (ids.Length == 0) return result;
        var rows = await db.Offers.AsNoTracking().Where(x => ids.Contains(x.CatalogVariantId) && x.Status != DomainOfferStatus.Archived)
            .GroupBy(x => x.CatalogVariantId).Select(x => new { Id = x.Key, Count = x.Count() }).ToListAsync(token);
        foreach (var row in rows) result[row.Id] = row.Count;
        return result;
    }

    /// <inheritdoc />
    public Task<int> CountActiveOffersAsync(CancellationToken cancellationToken) =>
        db.Offers.AsNoTracking().CountAsync(x => x.Status == DomainOfferStatus.Active, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListDistinctSellerPartyIdsAsync(CancellationToken cancellationToken) =>
        await db.Offers.AsNoTracking().Select(x => x.SellerPartyId).Distinct().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferSellerStatusRow>> ListSellerStatusRowsAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Offers.AsNoTracking()
            .Select(x => new { x.SellerPartyId, x.Status })
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new OfferSellerStatusRow(x.SellerPartyId, (OfferStatus)(int)x.Status))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> CountActiveOffersBySellerAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Offers.AsNoTracking()
            .Where(x => x.Status == DomainOfferStatus.Active)
            .GroupBy(x => x.SellerPartyId)
            .Select(g => new { SellerPartyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.SellerPartyId, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> CountAllOffersGroupedByCatalogVariantAsync(
        CancellationToken cancellationToken)
    {
        var rows = await db.Offers.AsNoTracking()
            .GroupBy(o => o.CatalogVariantId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.Key, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Guid>> MapAllOfferIdsToCatalogVariantIdsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await db.Offers.AsNoTracking()
            .Select(o => new { o.OfferId, o.CatalogVariantId })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.OfferId, x => x.CatalogVariantId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferReference>> ListOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        if (catalogVariantIds.Count == 0) return [];
        var ids = catalogVariantIds.Distinct().ToArray();
        var rows = await db.Offers.AsNoTracking()
            .Where(x => ids.Contains(x.CatalogVariantId))
            .ToListAsync(cancellationToken);
        return rows.Select(x => x.ToReference()).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferReference>> ListActiveOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        if (catalogVariantIds.Count == 0) return [];
        var ids = catalogVariantIds.Distinct().ToArray();
        var rows = await db.Offers.AsNoTracking()
            .Where(x => ids.Contains(x.CatalogVariantId) && x.Status == DomainOfferStatus.Active)
            .ToListAsync(cancellationToken);
        return rows.Select(x => x.ToReference()).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferReference>> ListActiveOffersAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Offers.AsNoTracking()
            .Where(x => x.Status == DomainOfferStatus.Active)
            .ToListAsync(cancellationToken);
        return rows.Select(x => x.ToReference()).ToList();
    }

    /// <inheritdoc />
    public Task<bool> AnyOffersForCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        if (catalogVariantIds.Count == 0) return Task.FromResult(false);
        var ids = catalogVariantIds.Distinct().ToArray();
        return db.Offers.AsNoTracking().AnyAsync(x => ids.Contains(x.CatalogVariantId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferListItem>> ListRecentActiveOffersAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 500);
        var rows = await db.Offers.AsNoTracking()
            .Where(x => x.Status == DomainOfferStatus.Active)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return rows.Select(ToListItem).ToList();
    }

    /// <inheritdoc />
    public async Task<OfferReference?> FindLatestBySellerAndVariantAsync(
        Guid sellerPartyId,
        Guid catalogVariantId,
        CancellationToken cancellationToken)
    {
        var row = await db.Offers.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId && x.CatalogVariantId == catalogVariantId)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return row?.ToReference();
    }

    /// <inheritdoc />
    public Task<bool> ExistsBySellerSkuAsync(
        Guid sellerPartyId,
        string sellerSku,
        CancellationToken cancellationToken) =>
        db.Offers.AsNoTracking().AnyAsync(
            x => x.SellerPartyId == sellerPartyId && x.SellerSku == sellerSku,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListOfferIdsBySellerSkuPrefixAsync(
        string sellerSkuPrefix,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sellerSkuPrefix)) return [];
        return await db.Offers.AsNoTracking()
            .Where(x => x.SellerSku != null && x.SellerSku.StartsWith(sellerSkuPrefix))
            .Select(x => x.OfferId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OfferReference?> FindBySellerSkuAsync(
        string sellerSku,
        CancellationToken cancellationToken)
    {
        var row = await db.Offers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerSku == sellerSku, cancellationToken);
        return row?.ToReference();
    }

    private static OfferListItem ToListItem(SellerOffer offer) => new(
        offer.OfferId,
        offer.CatalogVariantId,
        offer.SellerPartyId,
        (SalesChannel)(int)offer.Channel,
        (OfferStatus)(int)offer.Status,
        offer.SellerSku,
        offer.UpdatedAt);
}
