using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Commands.ArchiveOffer;

/// <summary>Archives a seller-owned offer.</summary>
public sealed record ArchiveOfferCommand(Guid OfferId, Guid SellerPartyId) : IRequest<Result<OfferReference>>;

internal sealed class ArchiveOfferHandler(IOfferStore store, IClock clock)
    : IRequestHandler<ArchiveOfferCommand, Result<OfferReference>>
{
    public async Task<Result<OfferReference>> Handle(ArchiveOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<OfferReference>();
        offer.Archive(clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(offer.ToReference());
    }
}
