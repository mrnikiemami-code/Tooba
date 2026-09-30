using Tooba.BuildingBlocks.Grid;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Admin;
using Tooba.Party.Contracts;

namespace Tooba.Party.Infrastructure.Adapters;

/// <summary>Contracts port over admin sellers grid engine + Party-owned normalize policy.</summary>
public sealed class AdminSellersGridAdapter(
    IOfferQueryGateway offers,
    IPartyAdminSellerReadGateway parties,
    IAdminSellerOrderCountPort orderCounts) : IAdminSellersGridPort
{
    /// <inheritdoc />
    public Task<GridPageResponse<AdminSellerListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = Grid.PartyAdminSellersGridPolicies.Normalize(request);
        return new Grid.AdminSellersGridQueryEngine(offers, parties, orderCounts)
            .QueryAsync(q, cancellationToken);
    }
}
