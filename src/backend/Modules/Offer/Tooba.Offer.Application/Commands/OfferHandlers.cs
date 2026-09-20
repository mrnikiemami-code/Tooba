using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Party.Contracts;
using DomainChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;

namespace Tooba.Offer.Application;

internal sealed class CreateOfferHandler(
    IOfferStore store, IOfferUseCaseGuard guard, ICatalogVariantLookup catalog, IPartyLookup parties,
    IClock clock, IIdGenerator ids) : IRequestHandler<CreateOfferCommand, OfferReference>
{
    public async Task<OfferReference> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken);
        if (await catalog.FindVariantAsync(request.CatalogVariantId, cancellationToken) is null)
            throw new SemanticException(new SemanticError(OfferErrorCodes.CatalogVariantMissing));
        var seller = await parties.FindByIdAsync(request.SellerPartyId, cancellationToken)
            ?? throw new SemanticException(new SemanticError(OfferErrorCodes.SellerMissing));
        if (!string.Equals(seller.Kind, "Organization", StringComparison.Ordinal))
            throw new SemanticException(new SemanticError(OfferErrorCodes.SellerNotOrganization));
        var channel = (DomainChannel)(int)request.Channel;
        if (await store.ExistsActiveListingAsync(request.SellerPartyId, request.CatalogVariantId, channel, cancellationToken))
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateActiveListing));
        if (!string.IsNullOrWhiteSpace(request.SellerSku)
            && await store.ExistsSellerSkuAsync(request.SellerPartyId, request.SellerSku.Trim(), null, cancellationToken))
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateSellerSku));
        var offer = SellerOffer.Create(ids.NewId(), request.CatalogVariantId, request.SellerPartyId, channel, request.SellerSku, clock.UtcNow);
        await store.AddAsync(offer, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return offer.ToReference();
    }
}

internal sealed class UpdateOfferHandler(IOfferStore store, IOfferUseCaseGuard guard, IClock clock)
    : IRequestHandler<UpdateOfferCommand, OfferReference>
{
    public async Task<OfferReference> Handle(UpdateOfferCommand request, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            throw new SemanticException(new SemanticError("offer.missing"));
        if (request.SellerSku is not null)
        {
            var sku = request.SellerSku.Trim();
            if (sku.Length > 0 && await store.ExistsSellerSkuAsync(request.SellerPartyId, sku, request.OfferId, cancellationToken))
                throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateSellerSku));
            offer.UpdateSellerSku(sku, clock.UtcNow);
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (request.Status.Equals(nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase)) offer.Activate(clock.UtcNow);
            else if (request.Status.Equals(nameof(OfferStatus.Suspended), StringComparison.OrdinalIgnoreCase)) offer.Suspend(clock.UtcNow);
            else throw new SemanticException(new SemanticError("offer.status.unsupported"));
        }
        await store.SaveChangesAsync(cancellationToken);
        return offer.ToReference();
    }
}

internal abstract class OfferMutationHandler<TRequest>(IOfferStore store, IOfferUseCaseGuard guard, IClock clock)
    : IRequestHandler<TRequest, OfferReference> where TRequest : IRequest<OfferReference>
{
    protected IClock Clock => clock;
    public async Task<OfferReference> Handle(TRequest request, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await store.GetByIdAsync(GetOfferId(request), cancellationToken)
            ?? throw new SemanticException(new SemanticError("offer.missing"));
        Mutate(offer, request);
        await store.SaveChangesAsync(cancellationToken);
        return offer.ToReference();
    }
    protected abstract Guid GetOfferId(TRequest request);
    protected abstract void Mutate(SellerOffer offer, TRequest request);
}

internal sealed class ActivateOfferHandler(IOfferStore s, IOfferUseCaseGuard g, IClock c) : OfferMutationHandler<ActivateOfferCommand>(s, g, c)
{ protected override Guid GetOfferId(ActivateOfferCommand r) => r.OfferId; protected override void Mutate(SellerOffer o, ActivateOfferCommand r) => o.Activate(Clock.UtcNow); }
internal sealed class SuspendOfferHandler(IOfferStore s, IOfferUseCaseGuard g, IClock c) : OfferMutationHandler<SuspendOfferCommand>(s, g, c)
{ protected override Guid GetOfferId(SuspendOfferCommand r) => r.OfferId; protected override void Mutate(SellerOffer o, SuspendOfferCommand r) => o.Suspend(Clock.UtcNow); }
internal sealed class ArchiveOfferHandler(IOfferStore s, IOfferUseCaseGuard g, IClock c) : OfferMutationHandler<ArchiveOfferCommand>(s, g, c)
{ protected override Guid GetOfferId(ArchiveOfferCommand r) => r.OfferId; protected override void Mutate(SellerOffer o, ArchiveOfferCommand r) => o.Archive(Clock.UtcNow); }
internal sealed class SetReturnPolicyHandler(IOfferStore s, IOfferUseCaseGuard g, IClock c, IReturnPolicyResolver resolver) : OfferMutationHandler<SetReturnPolicyCommand>(s, g, c)
{ protected override Guid GetOfferId(SetReturnPolicyCommand r) => r.OfferId; protected override void Mutate(SellerOffer o, SetReturnPolicyCommand r) { resolver.ValidateOfferChoice(r.Choice, r.CustomReturnWindowDays); o.SetReturnPolicy(r.Choice, r.CustomReturnWindowDays, Clock.UtcNow); } }
internal sealed class SetOrderQuantityLimitsHandler(IOfferStore s, IOfferUseCaseGuard g, IClock c) : OfferMutationHandler<SetOrderQuantityLimitsCommand>(s, g, c)
{ protected override Guid GetOfferId(SetOrderQuantityLimitsCommand r) => r.OfferId; protected override void Mutate(SellerOffer o, SetOrderQuantityLimitsCommand r) => o.SetOrderQuantityLimits(r.Minimum, r.Maximum, Clock.UtcNow); }

