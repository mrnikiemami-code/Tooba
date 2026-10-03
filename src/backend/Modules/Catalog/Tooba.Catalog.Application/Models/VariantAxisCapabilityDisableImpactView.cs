using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// پیش‌نمایش غیرمخرب اثر غیرفعال‌کردن capability محور تنوع.
/// </summary>
public sealed record VariantAxisCapabilityDisableImpactView(
    int CategoryBindingCount,
    IReadOnlyList<VariantAxisAffectedCategorySummary> AffectedCategories,
    int ProductCount,
    int VariantCombinationCount,
    bool CanDisable);
