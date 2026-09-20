using MediatR;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application;

internal sealed class GetOfferHandler(IOfferStore store) : IRequestHandler<GetOfferQuery, OfferReference?>
{
    public async Task<OfferReference?> Handle(GetOfferQuery request, CancellationToken cancellationToken) =>
        (await store.GetByIdAsync(request.OfferId, cancellationToken))?.ToReference();
}

internal sealed class ListSellerOffersHandler(IOfferStore store)
    : IRequestHandler<ListSellerOffersQuery, IReadOnlyList<OfferReference>>
{
    public async Task<IReadOnlyList<OfferReference>> Handle(
        ListSellerOffersQuery request,
        CancellationToken cancellationToken) =>
        (await store.ListBySellerAsync(request.SellerPartyId, cancellationToken))
            .Select(offer => offer.ToReference())
            .ToList();
}
