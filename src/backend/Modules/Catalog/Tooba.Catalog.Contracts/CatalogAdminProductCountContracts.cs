namespace Tooba.Catalog.Contracts;

/// <summary>
/// Catalog-owned Admin product count read boundary.
/// Returns a scalar count only — never EF types, entities, or queryables.
/// </summary>
public interface ICatalogAdminProductCountGateway
{
    /// <summary>Counts Catalog products currently in the Published publication status.</summary>
    Task<int> CountPublishedProductsAsync(CancellationToken cancellationToken);
}
