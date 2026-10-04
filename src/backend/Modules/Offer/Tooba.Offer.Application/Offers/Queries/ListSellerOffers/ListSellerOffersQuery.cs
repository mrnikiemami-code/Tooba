using Tooba.Offer.Application.Validation;
using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Queries.ListSellerOffers;

/// <summary>Lists final enriched read models for a seller's offers.</summary>
public sealed record ListSellerOffersQuery(Guid SellerPartyId) : IRequest<Result<IReadOnlyList<SellerOfferListItem>>>;

internal sealed class ListSellerOffersHandler(IOfferStore store, OfferReadModelComposer readModels)
    : IRequestHandler<ListSellerOffersQuery, Result<IReadOnlyList<SellerOfferListItem>>>
{
    public async Task<Result<IReadOnlyList<SellerOfferListItem>>> Handle(
        ListSellerOffersQuery request,
        CancellationToken cancellationToken)
    {
        var offers = await store.ListBySellerAsync(request.SellerPartyId, cancellationToken);
        return Result.Success(await readModels.ListAsync(offers, cancellationToken));
    }
}
