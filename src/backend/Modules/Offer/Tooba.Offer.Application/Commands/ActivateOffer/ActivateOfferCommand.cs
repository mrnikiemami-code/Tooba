using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Mappings;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Commands.ActivateOffer;

/// <summary>Activates a seller-owned offer.</summary>
public sealed record ActivateOfferCommand(Guid OfferId, Guid SellerPartyId) : IRequest<Result<OfferReference>>;

internal sealed class ActivateOfferHandler(IOfferStore store, IClock clock)
    : IRequestHandler<ActivateOfferCommand, Result<OfferReference>>
{
    public async Task<Result<OfferReference>> Handle(ActivateOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<OfferReference>();
        var activated = offer.Activate(clock.UtcNow);
        if (activated.IsFailure)
            return Result.Failure<OfferReference>(activated.Errors);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(offer.ToReference());
    }
}
