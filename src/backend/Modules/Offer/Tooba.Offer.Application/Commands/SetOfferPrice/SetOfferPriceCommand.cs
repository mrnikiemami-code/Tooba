using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts;

namespace Tooba.Offer.Application.Commands.SetOfferPrice;

/// <summary>Sets an offer price and returns its refreshed seller detail model.</summary>
public sealed record SetOfferPriceCommand(
    Guid OfferId,
    Guid SellerPartyId,
    decimal Amount,
    string? Currency,
    string? Market) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class SetOfferPriceCommandHandler(
    IOfferStore store,
    ISellerOfferPricingGateway pricing,
    OfferReadModelComposer readModels)
    : IRequestHandler<SetOfferPriceCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(
        SetOfferPriceCommand request,
        CancellationToken cancellationToken)
    {
        var write = await pricing.SetPriceAsync(new SetSellerOfferPrice(
            request.OfferId,
            request.SellerPartyId,
            request.Amount,
            request.Currency,
            request.Market), cancellationToken);
        if (write.IsFailure)
            return Result<SellerOfferDetailPage>.Failure(write.Errors);

        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();

        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}
