using Tooba.Offer.Application.Validation;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.History;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Commands.SetOfferInventory;

/// <summary>Sets offer inventory and returns its refreshed seller detail model.</summary>
public sealed record SetOfferInventoryCommand(
    Guid OfferId,
    Guid SellerPartyId,
    decimal OnHand,
    string? Reason,
    Guid? StockItemId = null) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class SetOfferInventoryCommandHandler(
    IOfferStore store,
    ISellerOfferInventoryGateway inventory,
    IClock clock,
    IIdGenerator ids,
    IOfferHistoryWriter history,
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

        var before = await inventory.GetAvailabilityAsync([request.OfferId], cancellationToken);
        var write = await inventory.SetInventoryAsync(new SetSellerOfferInventory(
            request.OfferId,
            request.SellerPartyId,
            request.OnHand,
            request.Reason,
            request.StockItemId), cancellationToken);
        if (write.IsFailure)
            return Result<SellerOfferDetailPage>.Failure(write.Errors);

        var after = await inventory.GetAvailabilityAsync([request.OfferId], cancellationToken);
        before.TryGetValue(request.OfferId, out var oldSummary);
        after.TryGetValue(request.OfferId, out var newSummary);
        var newPosition = newSummary?.Locations?.FirstOrDefault(
            position => request.StockItemId is null || position.StockItemId == request.StockItemId);
        var oldValue = oldSummary is null
            ? null
            : OfferHistorySummaries.InventoryLevel(oldSummary.OnHand);
        var newValue = OfferHistorySummaries.Inventory(
            newPosition?.StockItemId ?? request.StockItemId,
            newPosition?.LocationId,
            request.OnHand,
            request.Reason);
        await history.AppendAsync(
            OfferHistorySummaries.CreateEntry(
                ids.NewId(),
                offer.OfferId,
                clock.UtcNow,
                OfferHistorySummaries.InventoryChanged,
                "Offer inventory changed",
                oldValue,
                newValue),
            cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}
