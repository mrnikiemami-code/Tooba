using Tooba.BuildingBlocks.Security;
using Tooba.Catalog.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Host transport adapter for the Catalog seller Endpoints auth seam.
/// The panel gate comes from the neutral Host seller seam; no Catalog, module policy,
/// service locator, or business logic lives here.
/// </summary>
public sealed class HostCatalogSellerAuthorizer(ISellerPanelAccess sellerAccess) : ICatalogSellerAuthorizer
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
