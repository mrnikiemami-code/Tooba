using Tooba.BuildingBlocks.Security;

namespace Tooba.Catalog.Endpoints.Admin;

/// <summary>Neutral Catalog admin auth seam for Endpoints (Host keeps <see cref="IAdminPanelAccess"/>).</summary>
public interface ICatalogAdminAuthorizer
{
    /// <summary>Requires an authorized admin panel actor for Catalog Admin HTTP.</summary>
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

/// <summary>
/// Module-owned Catalog admin authorizer over the neutral platform <see cref="IAdminPanelAccess"/> seam.
/// Catalog policy stays here; Host remains the IAdminPanelAccess implementer.
/// </summary>
public sealed class CatalogAdminAuthorizer(IAdminPanelAccess adminAccess) : ICatalogAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
