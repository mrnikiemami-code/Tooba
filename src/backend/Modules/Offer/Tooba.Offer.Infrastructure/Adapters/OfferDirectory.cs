using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts;
using Tooba.Offer.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Domain;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>
/// نگهبان باز موردکاربرد. ماتریس Seller Portal اینجا نیست.
/// </summary>
public sealed class OpenOfferUseCaseGuard : IOfferUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// نوشتن Offer با قرارداد Catalog و Party. DbContext آن ماژول‌ها لمس نمی‌شود تا مرز Persistence حفظ شود.
/// </summary>
public sealed class OfferDirectory : IOfferDirectory, IOfferLookupGateway
{
    private readonly OfferDbContext _db;
    private readonly IOfferUseCaseGuard _guard;
    private readonly ICatalogLookupGateway _catalog;
    private readonly IPartyLookupGateway _party;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>
    /// دایرکتوری را به schema Offer و درزهای قرارداد وصل می‌کند نه به join بین‌schema.
    /// </summary>
    public OfferDirectory(
        OfferDbContext db,
        IOfferUseCaseGuard guard,
        ICatalogLookupGateway catalog,
        IPartyLookupGateway party,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        _guard = guard;
        _catalog = catalog;
        _party = party;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public async Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await _db.Offers.AsNoTracking().SingleOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);
        return offer is null ? null : ToReference(offer);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offerIds);
        if (offerIds.Count == 0)
        {
            return new Dictionary<Guid, OfferReference>();
        }

        var ids = offerIds.Distinct().ToArray();
        var rows = await _db.Offers.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.OfferId, ToReference);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalogVariantIds);
        if (catalogVariantIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var ids = catalogVariantIds.Distinct().ToArray();
        var rows = await _db.Offers.AsNoTracking()
            .Where(x => ids.Contains(x.CatalogVariantId) && x.Status != OfferStatus.Archived)
            .GroupBy(x => x.CatalogVariantId)
            .Select(g => new { VariantId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = ids.ToDictionary(id => id, _ => 0);
        foreach (var row in rows)
        {
            result[row.VariantId] = row.Count;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<OfferReference> CreateOfferAsync(
        Guid catalogVariantId,
        Guid sellerPartyId,
        SalesChannel channel,
        string? sellerSku,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (await _catalog.FindVariantAsync(catalogVariantId, cancellationToken) is null)
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.CatalogVariantMissing));
        }

        var seller = await _party.FindByIdAsync(sellerPartyId, cancellationToken)
            ?? throw new SemanticException(new SemanticError(OfferErrorCodes.SellerMissing));
        if (seller.Kind != PartyKind.Organization)
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.SellerNotOrganization));
        }

        var exists = await _db.Offers.AnyAsync(
            x => x.SellerPartyId == sellerPartyId
                 && x.CatalogVariantId == catalogVariantId
                 && x.Channel == channel
                 && x.Status != OfferStatus.Archived,
            cancellationToken);
        if (exists)
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateActiveListing));
        }

        if (!string.IsNullOrWhiteSpace(sellerSku)
            && await _db.Offers.AnyAsync(x => x.SellerPartyId == sellerPartyId && x.SellerSku == sellerSku.Trim(), cancellationToken))
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateSellerSku));
        }

        var now = _clock.UtcNow;
        var offer = SellerOffer.Create(_ids.NewId(), catalogVariantId, sellerPartyId, channel, sellerSku, now);
        _db.Offers.Add(offer);
        await _db.SaveChangesAsync(cancellationToken);
        return ToReference(offer);
    }

    /// <inheritdoc />
    public async Task ActivateAsync(Guid offerId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == offerId, cancellationToken);
        offer.Activate(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SuspendAsync(Guid offerId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == offerId, cancellationToken);
        offer.Suspend(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveAsync(Guid offerId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == offerId, cancellationToken);
        offer.Archive(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static OfferReference ToReference(SellerOffer offer) =>
        new(
            offer.OfferId,
            offer.CatalogVariantId,
            offer.SellerPartyId,
            offer.Channel,
            offer.Status,
            offer.SellerSku,
            offer.ReturnPolicyChoice,
            offer.CustomReturnWindowDays,
            offer.MinimumOrderQuantity,
            offer.MaximumOrderQuantity);
}
