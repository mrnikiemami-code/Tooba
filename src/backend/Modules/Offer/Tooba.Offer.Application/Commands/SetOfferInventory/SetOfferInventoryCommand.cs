using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Commands.SetOfferInventory;

/// <summary>Sets offer inventory and returns its refreshed seller detail model.</summary>
public sealed record SetOfferInventoryCommand(
    Guid OfferId,
    Guid SellerPartyId,
    decimal OnHand,
    string? Reason) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class SetOfferInventoryCommandHandler(
    IOfferStore store,
    ISellerOfferInventoryGateway inventory,
    OfferReadModelComposer readModels)
    : IRequestHandler<SetOfferInventoryCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(
        SetOfferInventoryCommand request,
        CancellationToken cancellationToken)
    {
        var write = await inventory.SetInventoryAsync(new SetSellerOfferInventory(
            request.OfferId,
            request.SellerPartyId,
            request.OnHand,
            request.Reason), cancellationToken);
        if (write.IsFailure)
            return Result<SellerOfferDetailPage>.Failure(write.Errors);

        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();

        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}
