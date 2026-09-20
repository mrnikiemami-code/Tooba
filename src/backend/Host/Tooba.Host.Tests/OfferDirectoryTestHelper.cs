using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Offer.Application;
using Tooba.Offer.Application.Commands.ActivateOffer;
using Tooba.Offer.Application.Commands.CreateOffer;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Domain;

namespace Tooba.Offer.Infrastructure.Adapters;

internal sealed class OpenOfferUseCaseGuard : IOfferUseCaseGuard
{
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Legacy test fixture helper; production use cases dispatch through MediatR.</summary>
internal sealed class OfferDirectory : IOfferLookupGateway
{
    private readonly OfferDbContext _db;
    private readonly IOfferUseCaseGuard _guard;
    private readonly ICatalogLookupGateway _catalog;
    private readonly IPartyLookupGateway _party;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    public OfferDirectory(OfferDbContext db, IOfferUseCaseGuard guard, ICatalogLookupGateway catalog, IPartyLookupGateway party, IClock clock, IIdGenerator ids)
        => (_db, _guard, _catalog, _party, _clock, _ids) = (db, guard, catalog, party, clock, ids);

    public async Task<OfferReference> CreateOfferAsync(Guid variantId, Guid sellerId, SalesChannel channel, string? sku, CancellationToken token)
    {
        await _guard.EnsureCanMutateAsync(token);
        if (await _catalog.FindVariantAsync(variantId, token) is null)
            throw new SemanticException(new SemanticError(OfferErrorCodes.CatalogVariantMissing));
        var seller = await _party.FindByIdAsync(sellerId, token)
            ?? throw new SemanticException(new SemanticError(OfferErrorCodes.SellerMissing));
        if (seller.Kind != PartyKind.Organization)
            throw new SemanticException(new SemanticError(OfferErrorCodes.SellerNotOrganization));
        var domainChannel = (Tooba.Offer.Domain.ValueObjects.SalesChannel)(int)channel;
        if (await _db.Offers.AnyAsync(x => x.SellerPartyId == sellerId && x.CatalogVariantId == variantId
            && x.Channel == domainChannel && x.Status != Tooba.Offer.Domain.ValueObjects.OfferStatus.Archived, token))
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateActiveListing));
        if (!string.IsNullOrWhiteSpace(sku)
            && await _db.Offers.AnyAsync(x => x.SellerPartyId == sellerId && x.SellerSku == sku.Trim(), token))
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateSellerSku));
        var offer = SellerOffer.Create(_ids.NewId(), variantId, sellerId, domainChannel, sku, _clock.UtcNow);
        _db.Add(offer);
        await _db.SaveChangesAsync(token);
        return offer.ToReference();
    }

    public async Task ActivateAsync(Guid id, CancellationToken token)
    {
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == id, token);
        offer.Activate(_clock.UtcNow);
        await _db.SaveChangesAsync(token);
    }

    public async Task SuspendAsync(Guid id, CancellationToken token)
    {
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == id, token);
        offer.Suspend(_clock.UtcNow);
        await _db.SaveChangesAsync(token);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken token)
    {
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == id, token);
        offer.Archive(_clock.UtcNow);
        await _db.SaveChangesAsync(token);
    }

    public async Task<OfferReference?> FindOfferAsync(Guid id, CancellationToken token) =>
        (await _db.Offers.AsNoTracking().SingleOrDefaultAsync(x => x.OfferId == id, token))?.ToReference();

    public async Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) =>
        (await _db.Offers.AsNoTracking().Where(x => ids.Contains(x.OfferId)).ToListAsync(token))
            .ToDictionary(x => x.OfferId, x => x.ToReference());

    public async Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken token)
    {
        var result = ids.Distinct().ToDictionary(x => x, _ => 0);
        var rows = await _db.Offers.AsNoTracking().Where(x => ids.Contains(x.CatalogVariantId))
            .GroupBy(x => x.CatalogVariantId).Select(x => new { x.Key, Count = x.Count() }).ToListAsync(token);
        foreach (var row in rows) result[row.Key] = row.Count;
        return result;
    }
}

/// <summary>Minimal sender adapter for legacy integration fixtures.</summary>
internal sealed class OfferTestSender(OfferDirectory offers) : MediatR.ISender
{
    public Task Send<TRequest>(TRequest request, CancellationToken token = default) where TRequest : MediatR.IRequest =>
        throw new NotSupportedException(typeof(TRequest).Name);

    public async Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, CancellationToken token = default) =>
        request switch
        {
            CreateOfferCommand create => (TResponse)(object)await offers.CreateOfferAsync(
                create.CatalogVariantId, create.SellerPartyId, create.Channel, create.SellerSku, token),
            ActivateOfferCommand activate => (TResponse)(object)await ActivateAsync(activate.OfferId, token),
            _ => throw new NotSupportedException(request.GetType().Name),
        };

    public async Task<object?> Send(object request, CancellationToken token = default)
    {
        if (request is CreateOfferCommand create)
            return await offers.CreateOfferAsync(create.CatalogVariantId, create.SellerPartyId, create.Channel, create.SellerSku, token);
        if (request is ActivateOfferCommand activate)
            return await ActivateAsync(activate.OfferId, token);
        throw new NotSupportedException(request.GetType().Name);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, CancellationToken token = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken token = default) =>
        throw new NotSupportedException();

    private async Task<OfferReference> ActivateAsync(Guid id, CancellationToken token)
    {
        await offers.ActivateAsync(id, token);
        return (await offers.FindOfferAsync(id, token))!;
    }
}
