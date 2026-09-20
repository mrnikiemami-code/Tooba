using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Infrastructure.Persistence;
using DomainOfferStatus = Tooba.Offer.Domain.ValueObjects.OfferStatus;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>Development-only Offer seed mutations owned by Offer.Infrastructure.</summary>
public sealed class OfferDevelopmentSeedGateway(OfferDbContext db, IIdGenerator ids) : IOfferDevelopmentSeedGateway
{
    /// <inheritdoc />
    public async Task EnsureActiveAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await db.Offers.SingleOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);
        if (offer is null)
        {
            return;
        }

        if (offer.Status == DomainOfferStatus.Active)
        {
            return;
        }

        offer.Activate(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> EnsureActiveCloneFromAnyActiveAsync(
        string sellerSku,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var existing = await db.Offers.SingleOrDefaultAsync(x => x.SellerSku == sellerSku, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status != DomainOfferStatus.Active)
            {
                existing.Activate(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(cancellationToken);
            }

            return existing.OfferId;
        }

        var template = await db.Offers.AsNoTracking()
            .Where(x => x.Status == DomainOfferStatus.Active)
            .OrderBy(x => x.OfferId)
            .FirstOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            return null;
        }

        var offer = SellerOffer.Create(
            ids.NewId(),
            template.CatalogVariantId,
            sellerPartyId,
            template.Channel,
            sellerSku,
            DateTimeOffset.UtcNow);
        db.Offers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);
        offer.Activate(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return offer.OfferId;
    }
}
