using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// گزارش تأثیر تغییر رده؛ حذف خاموش انجام نمی‌شود.
/// </summary>
public sealed record CategoryChangeImpact(
    Guid ProductId,
    Guid NewCategoryId,
    IReadOnlyList<OrphanProductAttributeValue> OrphanAttributeValues,
    IReadOnlyList<Guid> InvalidVariantAxisDefinitionIds);
