using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Offer.Application.Commands.SetReturnPolicy;

/// <summary>Sets the return policy of a seller-owned offer.</summary>
public sealed record SetReturnPolicyCommand(
    Guid OfferId, Guid SellerPartyId, string Choice, int? CustomReturnWindowDays) : IRequest<OfferReference>;

internal sealed class SetReturnPolicyHandler(
    IOfferStore store, IClock clock, IReturnPolicyResolver resolver)
    : IRequestHandler<SetReturnPolicyCommand, OfferReference>
{
    public async Task<OfferReference> Handle(SetReturnPolicyCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId) throw OfferReadModelComposer.NotFound();
        resolver.ValidateOfferChoice(request.Choice, request.CustomReturnWindowDays);
        offer.SetReturnPolicy(request.Choice, request.CustomReturnWindowDays, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return offer.ToReference();
    }
}
