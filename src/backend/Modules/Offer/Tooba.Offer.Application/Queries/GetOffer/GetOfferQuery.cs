using MediatR;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Queries.GetOffer;

/// <summary>Gets the final seller detail model for an owned offer.</summary>
public sealed record GetOfferQuery(Guid OfferId, Guid SellerPartyId) : IRequest<SellerOfferDetailPage>;

internal sealed class GetOfferHandler(IOfferStore store, OfferReadModelComposer readModels)
    : IRequestHandler<GetOfferQuery, SellerOfferDetailPage>
{
    public async Task<SellerOfferDetailPage> Handle(GetOfferQuery request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId) throw OfferReadModelComposer.NotFound();
        return await readModels.DetailAsync(offer, cancellationToken);
    }
}
