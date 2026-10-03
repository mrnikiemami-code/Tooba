using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// خلاصهٔ ردهٔ تحت تأثیر غیرفعال‌سازی capability محور تنوع.
/// </summary>
public sealed record VariantAxisAffectedCategorySummary(
    Guid CategoryId,
    string Name,
    int VariantBindingCount);
