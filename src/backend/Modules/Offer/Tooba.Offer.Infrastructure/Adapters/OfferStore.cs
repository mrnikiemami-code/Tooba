using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Application;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;
using Tooba.Offer.Infrastructure.Persistence;
using DomainOfferStatus = Tooba.Offer.Domain.ValueObjects.OfferStatus;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>EF-backed persistence adapter for Offer aggregates.</summary>
public sealed class OfferStore(OfferDbContext db) : IOfferStore, IOfferLookupGateway
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
        var rows = await db.Offers.AsNoTracking().Where(x => ids.Contains(x.CatalogVariantId) && x.Status != DomainOfferStatus.Archived)
            .GroupBy(x => x.CatalogVariantId).Select(x => new { Id = x.Key, Count = x.Count() }).ToListAsync(token);
        foreach (var row in rows) result[row.Id] = row.Count;
        return result;
    }
}
