using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Commands.SetOfferAllocation;

/// <summary>Sets platform-fulfilled offered quantity through Inventory's owner boundary.</summary>
public sealed record SetOfferAllocationCommand(
    Guid OfferId,
    Guid SellerPartyId,
    decimal Quantity,
    string? ExpectedRevision = null,
    Guid? ActorUserId = null,
    Guid OperationId = default,
    bool AdminSurface = false) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class SetOfferAllocationCommandHandler(
    IOfferStore store,
    ISellerOfferAllocationGateway allocation,
    IIdGenerator ids,
    OfferReadModelComposer readModels)
    : IRequestHandler<SetOfferAllocationCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(
        SetOfferAllocationCommand request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();

        var write = await allocation.SetAllocationAsync(new SetSellerOfferAllocation(
            request.OfferId,
            request.SellerPartyId,
            request.Quantity,
            request.ExpectedRevision,
            request.ActorUserId,
            request.OperationId), cancellationToken);
        if (write.IsFailure)
            return Result<SellerOfferDetailPage>.Failure(write.Errors);

        return Result.Success(await readModels.DetailAsync(offer, cancellationToken, adminSurface: request.AdminSurface));
    }
}
