using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Queries.GetOfferHistory;

/// <summary>
/// Authorized read of the recorded Offer history. A seller-scoped caller must own the offer; an admin caller
/// passes no seller id and is therefore not filtered by seller.
/// </summary>
public sealed record GetOfferHistoryQuery(Guid OfferId, Guid? SellerPartyId, int Page, int PageSize)
    : IRequest<Result<OfferHistoryPage>>;

internal sealed class GetOfferHistoryHandler(IOfferStore store, IOfferHistoryReader history)
    : IRequestHandler<GetOfferHistoryQuery, Result<OfferHistoryPage>>
{
    public async Task<Result<OfferHistoryPage>> Handle(
        GetOfferHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return OfferReadModelComposer.NotFound<OfferHistoryPage>();
        }

        if (request.SellerPartyId is Guid sellerId && offer.SellerPartyId != sellerId)
        {
            return OfferReadModelComposer.NotFound<OfferHistoryPage>();
        }

        var page = await history.GetPageAsync(
            request.OfferId, request.Page, request.PageSize, cancellationToken);
        return Result.Success(page);
    }
}
