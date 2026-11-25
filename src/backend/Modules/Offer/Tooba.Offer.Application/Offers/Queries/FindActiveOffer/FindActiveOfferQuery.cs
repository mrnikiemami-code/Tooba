using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Mappings;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Queries.FindActiveOffer;

/// <summary>
/// Finds the existing non-archived offer for one seller/variant/channel so the create form can
/// explain a duplicate listing and link to it instead of retrying blindly.
/// </summary>
public sealed record FindActiveOfferQuery(
    Guid SellerPartyId,
    Guid CatalogVariantId,
    SalesChannel Channel) : IRequest<Result<OfferReference?>>;

internal sealed class FindActiveOfferHandler(IOfferStore store)
    : IRequestHandler<FindActiveOfferQuery, Result<OfferReference?>>
{
    public async Task<Result<OfferReference?>> Handle(
        FindActiveOfferQuery request,
        CancellationToken cancellationToken)
    {
        var offerId = await store.FindActiveListingOfferIdAsync(
            request.SellerPartyId,
            request.CatalogVariantId,
            (Tooba.Offer.Domain.ValueObjects.SalesChannel)(int)request.Channel,
            cancellationToken);
        if (offerId is not Guid id)
        {
            return Result.Success<OfferReference?>(null);
        }

        var offer = await store.GetByIdAsync(id, cancellationToken);
        return Result.Success(offer is null ? null : offer.ToReference());
    }
}
