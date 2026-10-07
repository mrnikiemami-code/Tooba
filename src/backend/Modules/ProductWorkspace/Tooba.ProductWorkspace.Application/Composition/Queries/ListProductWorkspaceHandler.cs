using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.ProductWorkspace.Application.Composition.Queries;

/// <summary>Lists recent products via Catalog list gateway + commercial Contracts enrichment.</summary>
public sealed class ListProductWorkspaceHandler(
    ICatalogAdminProductWorkspaceListGateway catalog,
    IOfferQueryGateway offers,
    IPriceQueryGateway prices,
    IInventoryQueryGateway inventory)
    : IRequestHandler<ListProductWorkspaceQuery, Result<IReadOnlyList<AdminProductListItem>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AdminProductListItem>>> Handle(
        ListProductWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        var productIds = await catalog.ListRecentProductIdsAsync(100, cancellationToken);
        var items = await ProductWorkspaceListComposer.BuildListItemsAsync(
            catalog, offers, prices, inventory, productIds, cancellationToken);
        return Result.Success(items);
    }
}

