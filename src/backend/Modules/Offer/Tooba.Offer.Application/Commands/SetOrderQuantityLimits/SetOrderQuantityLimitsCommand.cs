using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Commands.SetOrderQuantityLimits;

/// <summary>Sets order quantity limits on a seller-owned offer.</summary>
public sealed record SetOrderQuantityLimitsCommand(
    Guid OfferId, Guid SellerPartyId, decimal? Minimum, decimal? Maximum) : IRequest<OfferReference>;

internal sealed class SetOrderQuantityLimitsHandler(IOfferStore store, IClock clock)
    : IRequestHandler<SetOrderQuantityLimitsCommand, OfferReference>
{
    public async Task<OfferReference> Handle(SetOrderQuantityLimitsCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId) throw OfferReadModelComposer.NotFound();
        offer.SetOrderQuantityLimits(request.Minimum, request.Maximum, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return offer.ToReference();
    }
}
