using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;

#pragma warning disable CS1591
namespace Tooba.Offer.Application.Offers.Queries.GetOffer;

/// <summary>Admin detail read by offer id, without impersonating a seller.</summary>
public sealed record GetOfferForAdminQuery(Guid OfferId, string? Locale = null) : IRequest<Result<SellerOfferDetailPage>>;

public sealed class GetOfferForAdminHandler(IOfferStore store, OfferReadModelComposer readModels)
    : IRequestHandler<GetOfferForAdminQuery, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(
        GetOfferForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();
        }
        return Result.Success(await readModels.DetailAsync(offer, cancellationToken, request.Locale, adminSurface: true));
    }
}

