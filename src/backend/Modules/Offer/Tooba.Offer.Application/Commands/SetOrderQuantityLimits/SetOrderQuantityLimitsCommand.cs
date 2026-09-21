using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Commands.SetOrderQuantityLimits;

/// <summary>Sets order quantity limits on a seller-owned offer.</summary>
public sealed record SetOrderQuantityLimitsCommand(
    Guid OfferId, Guid SellerPartyId, decimal? Minimum, decimal? Maximum) : IRequest<Result<OfferReference>>;

internal sealed class SetOrderQuantityLimitsHandler(IOfferStore store, IClock clock)
    : IRequestHandler<SetOrderQuantityLimitsCommand, Result<OfferReference>>
{
    public async Task<Result<OfferReference>> Handle(SetOrderQuantityLimitsCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<OfferReference>();
        var qty = offer.SetOrderQuantityLimits(request.Minimum, request.Maximum, clock.UtcNow);
        if (qty.IsFailure)
            return Result.Failure<OfferReference>(qty.Errors);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(offer.ToReference());
    }
}
