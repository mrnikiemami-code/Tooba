using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.ProductWorkspace.Application.Composition.Grid;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.ProductWorkspace.Application.Composition.Queries;

/// <summary>Queries Admin product grid via Catalog list gateway + commercial Contracts enrichment.</summary>
public sealed class QueryProductWorkspaceGridHandler(
    ICatalogAdminProductWorkspaceListGateway catalog,
    IOfferQueryGateway offers,
    IPriceQueryGateway prices,
    IInventoryQueryGateway inventory)
    : IRequestHandler<QueryProductWorkspaceGridQuery, Result<GridPageResponse<AdminProductListItem>>>
{
    /// <inheritdoc />
    public async Task<Result<GridPageResponse<AdminProductListItem>>> Handle(
        QueryProductWorkspaceGridQuery request,
        CancellationToken cancellationToken)
    {
        GridQueryRequest normalized;
        try
        {
            normalized = AdminProductGridQueryPolicy.Normalize(request.Request);
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<GridPageResponse<AdminProductListItem>>(new SemanticError(ex.ErrorCode));
        }

        var (pageIds, totalCount) = await catalog.ResolveGridPageProductIdsAsync(normalized, cancellationToken);
        if (pageIds.Count == 0)
        {
            return Result.Success(new GridPageResponse<AdminProductListItem>(
                [], normalized.Page, normalized.PageSize, totalCount));
        }

        var items = await ProductWorkspaceListComposer.BuildListItemsAsync(
            catalog, offers, prices, inventory, pageIds, cancellationToken);
        return Result.Success(new GridPageResponse<AdminProductListItem>(
            items, normalized.Page, normalized.PageSize, totalCount));
    }
}
