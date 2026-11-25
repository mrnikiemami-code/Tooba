using MediatR;
using Tooba.BuildingBlocks;
using Tooba.StoreContext.Contracts.Current;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Mappings;
using Tooba.Offer.Application.Offers.Policies;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;

namespace Tooba.Offer.Application.Offers.Queries.FindActiveOffer;

/// <summary>
/// Finds the existing non-archived offer for one seller/variant/channel so the create form can
/// explain a duplicate listing and link to it instead of retrying blindly. When <paramref name="Channel"/>
/// is absent the edition policy supplies the default allowed channel, never a hardcoded Marketplace.
/// </summary>
public sealed record FindActiveOfferQuery(
    Guid SellerPartyId,
    Guid CatalogVariantId,
    SalesChannel? Channel = null) : IRequest<Result<OfferReference?>>;

internal sealed class FindActiveOfferHandler(IOfferStore store, ICurrentStoreCommerceContext storeCommerce)
    : IRequestHandler<FindActiveOfferQuery, Result<OfferReference?>>
{
    public async Task<Result<OfferReference?>> Handle(
        FindActiveOfferQuery request,
        CancellationToken cancellationToken)
    {
        var channel = request.Channel ?? OfferChannelPolicy.Default(storeCommerce.Current?.SalesChannel);
        if (channel is not SalesChannel resolved)
        {
            return Result.Failure<OfferReference?>(new SemanticError(OfferErrorCodes.ChannelRequired));
        }

        if (!OfferChannelPolicy.IsAllowed(storeCommerce.Current?.SalesChannel, resolved))
        {
            return Result.Failure<OfferReference?>(new SemanticError(OfferErrorCodes.ChannelNotAllowed));
        }

        var offerId = await store.FindActiveListingOfferIdAsync(
            request.SellerPartyId,
            request.CatalogVariantId,
            (Tooba.Offer.Domain.ValueObjects.SalesChannel)(int)resolved,
            cancellationToken);
        if (offerId is not Guid id)
        {
            return Result.Success<OfferReference?>(null);
        }

        var offer = await store.GetByIdAsync(id, cancellationToken);
        return Result.Success(offer is null ? null : offer.ToReference());
    }
}
