using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// ورودی پیکربندی facet رده.
/// </summary>
public sealed record CategoryFacetConfigurationInput(
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts);
