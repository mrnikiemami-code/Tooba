using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// نمای محلی پیکربندی facet.
/// </summary>
public sealed record CategoryFacetConfigurationView(
    Guid FacetConfigurationId,
    Guid CategoryId,
    Guid DefinitionId,
    string Code,
    CatalogAttributeValueKind ValueKind,
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts);
