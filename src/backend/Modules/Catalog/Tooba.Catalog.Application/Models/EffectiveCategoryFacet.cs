using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// facet مؤثر رده برای Admin/Storefront.
/// </summary>
public sealed record EffectiveCategoryFacet(
    Guid DefinitionId,
    string Code,
    string LocalizedName,
    CatalogAttributeValueKind ValueKind,
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts,
    Guid SourceCategoryId,
    bool IsInherited);
