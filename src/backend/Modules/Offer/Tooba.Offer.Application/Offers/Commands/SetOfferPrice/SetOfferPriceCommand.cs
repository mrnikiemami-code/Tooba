using Tooba.Offer.Application.Validation;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Pricing.Contracts.Seller;

namespace Tooba.Offer.Application.Offers.Commands.SetOfferPrice;

/// <summary>Sets an offer price through the Pricing owner boundary and returns the refreshed detail.</summary>
public sealed record SetOfferPriceCommand(
    Guid OfferId,
    Guid SellerPartyId,
    decimal Amount,
    string? Currency,
    string? Market,
    string? ExpectedVersion = null,
    Guid? ActorUserId = null,
    Guid OperationId = default,
    bool AdminSurface = false,
    bool Retire = false) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class SetOfferPriceCommandHandler(
    IOfferStore store,
    ISellerOfferPricingGateway pricing,
    IIdGenerator ids,
    OfferReadModelComposer readModels)
    : IRequestHandler<SetOfferPriceCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(
        SetOfferPriceCommand request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();

        var write = await pricing.SetPriceAsync(new SetSellerOfferPrice(
            request.OfferId,
            request.SellerPartyId,
            request.Amount,
            request.Currency,
            request.Market,
            request.ExpectedVersion,
            request.ActorUserId,
            request.OperationId,
            request.Retire), cancellationToken);
        if (write.IsFailure)
            return Result<SellerOfferDetailPage>.Failure(write.Errors);

        return Result.Success(await readModels.DetailAsync(offer, cancellationToken, adminSurface: request.AdminSurface));
    }
}
