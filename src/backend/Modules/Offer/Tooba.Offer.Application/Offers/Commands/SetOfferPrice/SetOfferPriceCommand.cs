using Tooba.Offer.Application.Validation;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.History;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Pricing.Contracts.Seller;

namespace Tooba.Offer.Application.Offers.Commands.SetOfferPrice;

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
    IPriceLookupGateway priceLookup,
    IClock clock,
    IIdGenerator ids,
    IOfferHistoryWriter history,
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

        var market = string.IsNullOrWhiteSpace(request.Market) ? "IR" : request.Market.Trim();
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "IRR" : request.Currency.Trim();
        var resolution = new PriceResolutionQuery(
            request.OfferId,
            market,
            (SalesChannel)(int)offer.Channel,
            currency,
            clock.UtcNow,
            null,
            null,
            null);
        var previous = await priceLookup.ResolvePriceAsync(resolution, cancellationToken);

        var write = await pricing.SetPriceAsync(new SetSellerOfferPrice(
            request.OfferId,
            request.SellerPartyId,
            request.Amount,
            request.Currency,
            request.Market), cancellationToken);
        if (write.IsFailure)
            return Result<SellerOfferDetailPage>.Failure(write.Errors);

        var current = await priceLookup.ResolvePriceAsync(resolution, cancellationToken);
        var oldValue = previous is null
            ? null
            : OfferHistorySummaries.Price(previous.Amount, previous.Currency);
        var newValue = current is null
            ? OfferHistorySummaries.Price(request.Amount, currency)
            : OfferHistorySummaries.Price(current.Amount, current.Currency);
        await history.AppendAsync(
            OfferHistorySummaries.CreateEntry(
                ids.NewId(),
                offer.OfferId,
                clock.UtcNow,
                OfferHistorySummaries.PriceChanged,
                "Offer price changed",
                oldValue,
                newValue),
            cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}

