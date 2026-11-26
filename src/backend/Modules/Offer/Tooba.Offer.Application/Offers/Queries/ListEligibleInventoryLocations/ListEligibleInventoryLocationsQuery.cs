using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Seller;

namespace Tooba.Offer.Application.Offers.Queries.ListEligibleInventoryLocations;

/// <summary>Reads the Inventory-owned active storage locations eligible for an offer stock position.</summary>
public sealed record ListEligibleInventoryLocationsQuery(Guid? SellerPartyId = null)
    : IRequest<Result<IReadOnlyList<InventoryEligibleLocation>>>;

internal sealed class ListEligibleInventoryLocationsHandler(IInventoryProvisioningGateway provisioning)
    : IRequestHandler<ListEligibleInventoryLocationsQuery, Result<IReadOnlyList<InventoryEligibleLocation>>>
{
    public async Task<Result<IReadOnlyList<InventoryEligibleLocation>>> Handle(
        ListEligibleInventoryLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var locations = await provisioning.ListEligibleLocationsAsync(request.SellerPartyId, cancellationToken);
        return Result.Success(locations);
    }
}
