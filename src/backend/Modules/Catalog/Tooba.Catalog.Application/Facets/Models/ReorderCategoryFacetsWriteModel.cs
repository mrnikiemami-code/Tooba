namespace Tooba.Catalog.Application.Facets.Models;

/// <summary>Transport body for PUT .../facets/order (OrderedDefinitionIds only).</summary>
public sealed record ReorderCategoryFacetsWriteModel(IReadOnlyList<Guid>? OrderedDefinitionIds);
