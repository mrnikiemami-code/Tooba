using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Commands.SetOfferInventory;

/// <summary>Sets offer physical stock or allocation through the Inventory owner boundary.</summary>
public sealed record SetOfferInventoryCommand(
    Guid OfferId,
    Guid SellerPartyId,
    decimal OnHand,
    string? Reason,
    Guid? StockItemId = null,
    string? ExpectedVersion = null,
    Guid? ActorUserId = null,
    Guid OperationId = default,
    bool AdminPhysicalAdjustment = false) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class SetOfferInventoryCommandHandler(
    IOfferStore store,
    ISellerOfferInventoryGateway inventory,
    IIdGenerator ids,
    OfferReadModelComposer readModels)
    : IRequestHandler<SetOfferInventoryCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(
        SetOfferInventoryCommand request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();

        var write = await inventory.SetInventoryAsync(new SetSellerOfferInventory(
            request.OfferId,
            request.SellerPartyId,
            request.OnHand,
            request.Reason,
            request.StockItemId,
            request.ExpectedVersion,
            request.ActorUserId,
            request.OperationId == Guid.Empty ? ids.NewId() : request.OperationId,
            request.AdminPhysicalAdjustment), cancellationToken);
        if (write.IsFailure)
            return Result<SellerOfferDetailPage>.Failure(write.Errors);

        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}
