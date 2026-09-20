using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Commands.ActivateOffer;

/// <summary>Activates a seller-owned offer.</summary>
public sealed record ActivateOfferCommand(Guid OfferId, Guid SellerPartyId) : IRequest<OfferReference>;

internal sealed class ActivateOfferHandler(IOfferStore store, IClock clock)
    : IRequestHandler<ActivateOfferCommand, OfferReference>
{
    public async Task<OfferReference> Handle(ActivateOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId) throw OfferReadModelComposer.NotFound();
        offer.Activate(clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return offer.ToReference();
    }
}
