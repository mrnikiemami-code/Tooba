using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.ValueObjects;

/// <summary>
/// گزارش تأثیر تغییر رده بدون حذف خاموش مقادیر.
/// </summary>
public sealed record CatalogCategoryChangeImpactReport(
    IReadOnlyList<(Guid DefinitionId, string CanonicalValue)> OrphanAttributeValues,
    IReadOnlyList<Guid> InvalidVariantAxisDefinitionIds);
