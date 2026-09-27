namespace Tooba.Catalog.Application.Facets.Models;

/// <summary>
/// Facet view/input DTOs remain Application-root (<c>CategoryFacetConfigurationInput</c>,
/// <c>CategoryFacetConfigurationView</c>, <c>EffectiveCategoryFacet</c>) so ICatalogDirectory
/// and existing tests keep one authoritative shape. This capability folder owns CQRS
/// requests/ports/validators; no duplicate command-shaped Models.
/// </summary>
internal static class FacetSharedModelNote
{
}
