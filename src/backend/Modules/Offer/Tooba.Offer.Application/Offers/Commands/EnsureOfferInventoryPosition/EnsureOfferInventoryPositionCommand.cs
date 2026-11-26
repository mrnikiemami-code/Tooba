using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Domain.ValueObjects;

namespace Tooba.Offer.Application.Offers.Commands.EnsureOfferInventoryPosition;

/// <summary>
/// Ensures the persisted offer has an idempotent Inventory-owned stock position at an active location.
/// A seller-scoped caller must own the offer; an admin caller may target any offer.
/// </summary>
public sealed record EnsureOfferInventoryPositionCommand(
    Guid OfferId,
    Guid LocationId,
    Guid? SellerPartyId = null) : IRequest<Result<ProvisionedStockPosition>>;

internal sealed class EnsureOfferInventoryPositionCommandHandler(
    IOfferStore store,
    IInventoryProvisioningGateway provisioning)
    : IRequestHandler<EnsureOfferInventoryPositionCommand, Result<ProvisionedStockPosition>>
{
    public async Task<Result<ProvisionedStockPosition>> Handle(
        EnsureOfferInventoryPositionCommand request,
        CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null
            || (request.SellerPartyId is Guid sellerPartyId && offer.SellerPartyId != sellerPartyId))
        {
            return OfferReadModelComposer.NotFound<ProvisionedStockPosition>();
        }

        if (request.SellerPartyId is not null && offer.FulfillmentResponsibility == FulfillmentResponsibility.Platform)
            return Result.Failure<ProvisionedStockPosition>(new SemanticError(InventoryErrorCodes.LocationNotEligible));

        try
        {
            var position = await provisioning.EnsurePositionAsync(
                offer.OfferId,
                offer.CatalogVariantId,
                request.LocationId,
                offer.FulfillmentResponsibility == FulfillmentResponsibility.Seller
                    ? offer.SellerPartyId : request.SellerPartyId,
                cancellationToken);
            return Result.Success(position);
        }
        catch (ContractOperationException ex) when (InventoryErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure<ProvisionedStockPosition>(new SemanticError(ex.Code));
        }
    }
}
