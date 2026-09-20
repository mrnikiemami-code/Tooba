using MediatR;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Queries.ListSellerOffers;

/// <summary>Lists final enriched read models for a seller's offers.</summary>
public sealed record ListSellerOffersQuery(Guid SellerPartyId) : IRequest<IReadOnlyList<SellerOfferListItem>>;

internal sealed class ListSellerOffersHandler(IOfferStore store, OfferReadModelComposer readModels)
    : IRequestHandler<ListSellerOffersQuery, IReadOnlyList<SellerOfferListItem>>
{
    public async Task<IReadOnlyList<SellerOfferListItem>> Handle(
        ListSellerOffersQuery request,
        CancellationToken cancellationToken)
    {
        var offers = await store.ListBySellerAsync(request.SellerPartyId, cancellationToken);
        return await readModels.ListAsync(offers, cancellationToken);
    }
}
