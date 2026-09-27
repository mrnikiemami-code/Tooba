using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Facets.Ports;

/// <summary>Catalog-owned port for Facet Admin + Storefront operations.</summary>
public interface IFacetDirectory
{
    /// <summary>Effective facets for a category (inheritance + filterable eligibility).</summary>
    Task<Result<IReadOnlyList<EffectiveCategoryFacet>>> GetEffectiveFacetsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>Local facet configuration rows for a category.</summary>
    Task<Result<IReadOnlyList<CategoryFacetConfigurationView>>> ListLocalConfigurationsAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    /// <summary>Creates or updates a local facet override.</summary>
    Task<Result> UpsertConfigurationAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryFacetConfigurationInput input,
        CancellationToken cancellationToken);

    /// <summary>Removes a local facet override (fallback to parent).</summary>
    Task<Result> RemoveOverrideAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken);

    /// <summary>Rewrites SortOrder for the exact local facet set.</summary>
    Task<Result> ReorderConfigurationsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken);
}
