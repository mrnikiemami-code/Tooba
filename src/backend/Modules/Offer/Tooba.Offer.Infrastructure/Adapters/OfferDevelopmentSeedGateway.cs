using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Infrastructure.Persistence;
using DomainOfferStatus = Tooba.Offer.Domain.ValueObjects.OfferStatus;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>Development-only Offer seed mutations owned by Offer.Infrastructure.</summary>
public sealed class OfferDevelopmentSeedGateway(
    OfferDbContext db,
    IIdGenerator ids,
    IClock clock,
    ICatalogVariantLookup catalogVariants) : IOfferDevelopmentSeedGateway
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

        if (offer.Activate(clock.UtcNow).IsFailure)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> EnsureActiveSellerOfferAsync(
        Guid catalogVariantId,
        Guid sellerPartyId,
        string sellerSku,
        CancellationToken cancellationToken)
    {
        var existing = await db.Offers.SingleOrDefaultAsync(x => x.SellerSku == sellerSku, cancellationToken);
        if (existing is not null)
        {
            await EnsureActiveAsync(existing, cancellationToken);
            return existing.OfferId;
        }

        var offer = SellerOffer.Create(
            ids.NewId(),
            catalogVariantId,
            sellerPartyId,
            Tooba.Offer.Domain.ValueObjects.SalesChannel.Marketplace,
            sellerSku,
            clock.UtcNow);
        db.Offers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);
        await EnsureActiveAsync(offer, cancellationToken);
        return offer.OfferId;
    }

    /// <inheritdoc />
    public async Task<Guid?> EnsureActiveCloneFromAnyActiveAsync(
        string sellerSku,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var existing = await db.Offers.SingleOrDefaultAsync(x => x.SellerSku == sellerSku, cancellationToken);

        // TB-288: reuse a surviving dev-seed Offer ONLY when the Catalog variant it points at still exists.
        // A Catalog demo reset regenerates variant ids and deletes the previous variants, so a row that only
        // matches by SellerSku can reference a now-missing variant (surfacing later as
        // inventory.catalog_variant.missing). Such a stale row must not be considered valid.
        if (existing is not null
            && await catalogVariants.FindVariantAsync(existing.CatalogVariantId, cancellationToken) is not null)
        {
            await EnsureActiveAsync(existing, cancellationToken);
            return existing.OfferId;
        }

        var targetVariantId = await ResolveLiveVariantIdAsync(existing, cancellationToken);
        if (targetVariantId is null)
        {
            return null;
        }

        if (existing is not null)
        {
            // Bounded rebuild of marked development-seed data only (the caller owns this SellerSku):
            // remove the stale row so exactly one Offer per SellerSku remains, then recreate it on a live variant.
            db.Offers.Remove(existing);
        }

        var offer = SellerOffer.Create(
            ids.NewId(),
            targetVariantId.Value,
            sellerPartyId,
            Tooba.Offer.Domain.ValueObjects.SalesChannel.Marketplace,
            sellerSku,
            clock.UtcNow);
        db.Offers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);
        await EnsureActiveAsync(offer, cancellationToken);
        return offer.OfferId;
    }

    private async Task<Guid?> ResolveLiveVariantIdAsync(SellerOffer? stale, CancellationToken cancellationToken)
    {
        // Prefer a still-published Catalog variant so the rebuilt seed row is storefront-valid and stable.
        var published = await catalogVariants.GetPublishedVariantIdsAsync(cancellationToken);
        if (published.Count > 0)
        {
            return published[0];
        }

        // Fallback: any Active template (other than the stale row) whose variant still exists.
        var templateVariantIds = await db.Offers.AsNoTracking()
            .Where(x => x.Status == DomainOfferStatus.Active && (stale == null || x.OfferId != stale.OfferId))
            .OrderBy(x => x.OfferId)
            .Select(x => x.CatalogVariantId)
            .ToListAsync(cancellationToken);
        foreach (var variantId in templateVariantIds)
        {
            if (await catalogVariants.FindVariantAsync(variantId, cancellationToken) is not null)
            {
                return variantId;
            }
        }

        return null;
    }

    private async Task EnsureActiveAsync(SellerOffer offer, CancellationToken cancellationToken)
    {
        if (offer.Status == DomainOfferStatus.Active)
        {
            return;
        }

        if (offer.Activate(clock.UtcNow).IsSuccess)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
