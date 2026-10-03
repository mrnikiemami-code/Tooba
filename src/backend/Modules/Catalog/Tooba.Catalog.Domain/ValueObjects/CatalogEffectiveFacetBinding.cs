using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.ValueObjects;

/// <summary>
/// ردیف میانی facet مؤثر.
/// </summary>
public sealed record CatalogEffectiveFacetBinding(
    Guid DefinitionId,
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts,
    Guid SourceCategoryId,
    CatalogAttributeDefinition Definition);
