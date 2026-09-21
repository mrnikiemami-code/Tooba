using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Commands.SuspendOffer;

/// <summary>Suspends a seller-owned offer.</summary>
public sealed record SuspendOfferCommand(Guid OfferId, Guid SellerPartyId) : IRequest<Result<OfferReference>>;

internal sealed class SuspendOfferHandler(IOfferStore store, IClock clock)
    : IRequestHandler<SuspendOfferCommand, Result<OfferReference>>
{
    public async Task<Result<OfferReference>> Handle(SuspendOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<OfferReference>();
        offer.Suspend(clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(offer.ToReference());
    }
}
